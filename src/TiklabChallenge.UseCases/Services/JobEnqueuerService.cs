using StackExchange.Redis;
using System.Text.Json;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;

public class JobEnqueuerService : IJobEnqueuerService
{
    private readonly IDatabase _redis;
    private const string STREAM_KEY = "job:stream";
    private const string JOB_PREFIX = "job:data:";

    public JobEnqueuerService(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
        // Create consumer group if it doesn't exist
        try { _redis.StreamCreateConsumerGroup(STREAM_KEY, "workers", "0"); }
        catch (RedisServerException) { /* Group exists */ }
    }

    public async Task<string> EnqueueJobAsync(Job job)
    {
        job.Id ??= Guid.NewGuid().ToString();
        job.Status = "queued";
        job.Attempts = 0;
        job.CreatedAt = DateTime.UtcNow;

        // Store job data separately for retrieval
        var jobJson = JsonSerializer.Serialize(job);
        await _redis.StringSetAsync($"{JOB_PREFIX}{job.Id}", jobJson);

        // Add job to stream (just the ID + minimal metadata)
        var entries = new NameValueEntry[]
        {
            new("id", job.Id),
            new("name", job.Name),
            new("priority", job.Priority.ToString())
        };

        await _redis.StreamAddAsync(STREAM_KEY, entries);
        return job.Id;
    }
}