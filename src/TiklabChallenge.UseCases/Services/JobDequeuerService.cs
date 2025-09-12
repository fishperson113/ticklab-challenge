using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;

public class JobDequeuerService : BackgroundService
{
    private readonly IDatabase _redis;
    private readonly ILogger<JobDequeuerService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly string _consumer;

    private const string STREAM = "job:stream";
    private const string DLQ = "job:dlq";
    private const string GROUP = "workers";
    private const string JOB_DATA = "job:data:";
    private const int MAX_LENGTH = 10000;

    public JobDequeuerService(
        IConnectionMultiplexer redis,
        ILogger<JobDequeuerService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _redis = redis.GetDatabase();
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _consumer = $"w-{Environment.MachineName}-{Guid.NewGuid().ToString()[..8]}";

        try { _redis.StreamCreateConsumerGroup(STREAM, GROUP, "$", createStream: true); }
        catch { /* Group may already exist */ }
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Worker started: {consumer}", _consumer);

        await ClaimPendingMessagesAsync();

        var lastPendingCheck = DateTime.UtcNow;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if ((DateTime.UtcNow - lastPendingCheck).TotalMinutes >= 2)
                {
                    await ClaimPendingMessagesAsync();
                    lastPendingCheck = DateTime.UtcNow;
                }
                // Use raw Redis command for blocking reads with BLOCK parameter
                var result = await _redis.ExecuteAsync("XREADGROUP",
                    "GROUP", GROUP, _consumer, "BLOCK", "5000",
                    "COUNT", "1", "STREAMS", STREAM, ">");

                if (result.IsNull) continue;

                // Parse the result and process entry
                var streamEntries = ParseStreamResult(result);
                foreach (var entry in streamEntries)
                    await ProcessMessageAsync(entry, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading stream");
                await Task.Delay(1000, ct);
            }
        }
        
    }

    private StreamEntry[] ParseStreamResult(RedisResult result)
    {
        if (result.IsNull) return Array.Empty<StreamEntry>();

        try
        {
            var streamArray = (RedisResult[])result;
            if (streamArray.Length == 0) return Array.Empty<StreamEntry>();

            var firstStream = (RedisResult[])streamArray[0];
            var messagesArray = (RedisResult[])firstStream[1];

            var entries = new List<StreamEntry>();
            foreach (var message in messagesArray)
            {
                var messageArray = (RedisResult[])message;
                var id = (string)messageArray[0];
                var values = (RedisResult[])messageArray[1];

                var nameValues = new List<NameValueEntry>();
                for (int i = 0; i < values.Length; i += 2)
                    nameValues.Add(new NameValueEntry((string)values[i], (string)values[i + 1]));

                entries.Add(new StreamEntry(id, nameValues.ToArray()));
            }
            return entries.ToArray();
        }
        catch
        {
            return Array.Empty<StreamEntry>();
        }
    }

    private async Task ProcessMessageAsync(StreamEntry entry, CancellationToken ct)
    {
        var values = entry.Values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());
        if (!values.TryGetValue("id", out var jobId))
        {
            await _redis.StreamAcknowledgeAsync(STREAM, GROUP, entry.Id);
            return;
        }

        var jobData = await _redis.StringGetAsync($"{JOB_DATA}{jobId}");
        if (jobData.IsNull)
        {
            await _redis.StreamAcknowledgeAsync(STREAM, GROUP, entry.Id);
            return;
        }

        var job = JsonSerializer.Deserialize<Job>(jobData);
        job.Attempts = int.Parse(values.GetValueOrDefault("attempts", "0"));

        // Check if job is already completed or failed (idempotency check)
        if (job.Status == "completed" || job.Status == "failed")
        {
            // Job already processed, just acknowledge the message
            await _redis.StreamAcknowledgeAsync(STREAM, GROUP, entry.Id);
            return;
        }

        try
        {
            job.Status = "processing";
            job.Attempts++;
            await _redis.StringSetAsync($"{JOB_DATA}{jobId}", JsonSerializer.Serialize(job));

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                // Resolve the processor from the scope
                var processor = scope.ServiceProvider.GetRequiredService<IJobProcessor>();
                await processor.ProcessAsync(job, ct);
            }

            // Job completed successfully
            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
            await _redis.StringSetAsync($"{JOB_DATA}{jobId}", JsonSerializer.Serialize(job));

            // Acknowledge the message to remove it from pending list
            await _redis.StreamAcknowledgeAsync(STREAM, GROUP, entry.Id);
        }
        catch (Exception ex)
        {
            if (job.Attempts >= job.MaxAttempts)
            {
                // Move to DLQ
                job.Status = "failed";
                job.Error = ex.Message;
                await _redis.StringSetAsync($"{JOB_DATA}{jobId}", JsonSerializer.Serialize(job));

                await _redis.StreamAddAsync(DLQ, new[] {
                new NameValueEntry("id", jobId),
                new NameValueEntry("error", ex.Message),
                new NameValueEntry("attempts", job.Attempts.ToString())
            }, maxLength: MAX_LENGTH);
            }
            else
            {
                // Re-enqueue with incremented attempts
                job.Status = "retry";
                job.Error = ex.Message;
                await _redis.StringSetAsync($"{JOB_DATA}{jobId}", JsonSerializer.Serialize(job));

                await _redis.StreamAddAsync(STREAM, new[] {
                new NameValueEntry("id", jobId),
                new NameValueEntry("attempts", job.Attempts.ToString())
            }, maxLength: MAX_LENGTH);
            }

            // Always ACK the original message
            await _redis.StreamAcknowledgeAsync(STREAM, GROUP, entry.Id);
        }
    }

    private async Task ClaimPendingMessagesAsync()
    {
        try
        {
            var pendingInfo = await _redis.StreamPendingAsync(STREAM, GROUP);
            if (pendingInfo.PendingMessageCount == 0) return;

            var pendingMessages = await _redis.StreamPendingMessagesAsync(
                STREAM, GROUP, count: 10, consumerName: string.Empty);

            var idleMessages = pendingMessages
                .Where(msg => msg.IdleTimeInMilliseconds > 60000)
                .Select(msg => msg.MessageId)
                .ToArray();

            if (idleMessages.Length > 0)
            {
                var claimed = await _redis.StreamClaimAsync(
                    STREAM, GROUP, _consumer, minIdleTimeInMs: 60000, messageIds: idleMessages);

                foreach (var entry in claimed)
                {
                    // Extract job ID
                    var values = entry.Values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());
                    if (values.TryGetValue("id", out var jobId))
                    {
                        // Check if job is already completed before processing
                        var jobData = await _redis.StringGetAsync($"{JOB_DATA}{jobId}");
                        if (!jobData.IsNull)
                        {
                            var job = JsonSerializer.Deserialize<Job>(jobData);
                            if (job.Status == "completed" || job.Status == "failed")
                            {
                                // Job already processed, just acknowledge it
                                await _redis.StreamAcknowledgeAsync(STREAM, GROUP, entry.Id);
                                continue;
                            }
                        }
                    }

                    await ProcessMessageAsync(entry, CancellationToken.None);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error claiming pending messages");
        }
    }
}