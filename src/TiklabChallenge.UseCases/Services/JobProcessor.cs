using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.UseCases.DTOs;
using TiklabChallenge.UseCases.Services;

public class JobProcessor : IJobProcessor
{
    private readonly ILogger<JobProcessor> _logger;
    private readonly IDatabase _redis;
    private readonly StudentEnrollmentService _enrollmentService;
    private readonly IRedisCacheService _cache;

    public JobProcessor(
        ILogger<JobProcessor> logger,
        IConnectionMultiplexer redis,
        StudentEnrollmentService enrollmentService,
        IRedisCacheService cache)
    {
        _logger = logger;
        _redis = redis.GetDatabase();
        _enrollmentService = enrollmentService;
        _cache = cache;
    }

    public async Task ProcessAsync(Job job, CancellationToken ct)
    {
        // Generate deterministic processing ID for idempotency
        var processingId = $"job:processed:{job.Id}:{job.Attempts}";

        // Check if this attempt was already processed (idempotency)
        if (await _redis.KeyExistsAsync(processingId))
        {
            _logger.LogInformation("Skipping already processed job attempt: {id}:{attempt}",
                job.Id, job.Attempts);
            return;
        }

        try
        {
            // Process based on job type
            _logger.LogInformation("Processing job: {name}", job.Name);

            if (job.Name == "student-enrollment" && !string.IsNullOrEmpty(job.Data))
            {
                await ProcessStudentEnrollmentAsync(job, ct);
            }
            else
            {
                // Default behavior for other job types or missing data
                _logger.LogInformation("Unknown job type or missing data: {name}", job.Name);
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }

            // Record successful processing with expiration (24h)
            await _redis.StringSetAsync(processingId, "processed", TimeSpan.FromHours(24));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}: {Message}", job.Id, ex.Message);

            // Update job with error
            job.Error = ex.Message;
            job.Status = "failed";

            throw;
        }
    }

    private async Task ProcessStudentEnrollmentAsync(Job job, CancellationToken ct)
    {
        try
        {
            // Log the raw data for debugging
            _logger.LogDebug("Raw job data: {Data}", job.Data);

            // Try to deserialize with more options
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            EnrollmentJobData jobData;

            jobData = JsonSerializer.Deserialize<EnrollmentJobData>(job.Data, options);
     
            // Validate the deserialized data
            if (jobData == null || string.IsNullOrEmpty(jobData.UserId) || string.IsNullOrEmpty(jobData.CourseCode))
            {
                _logger.LogError("Invalid enrollment job data for {JobId}: UserId={UserId}, CourseCode={CourseCode}",
                    job.Id, jobData?.UserId ?? "null", jobData?.CourseCode ?? "null");
                job.Error = "Invalid job data: missing required fields";
                job.Status = "failed";
                return;
            }

            _logger.LogInformation("Processing enrollment: UserId={UserId}, CourseCode={CourseCode}",
                jobData.UserId, jobData.CourseCode);

            // Process the enrollment
            var enrollmentRequest = new CourseEnrollmentRequest { CourseCode = jobData.CourseCode };
            var enrollment = await _enrollmentService.EnrollStudentInCourseAsync(
                jobData.UserId,
                enrollmentRequest,
                ct);

            // Clear cache
            _cache?.Remove($"student_enrollments_{jobData.UserId}");
            _cache?.Remove($"course_{jobData.CourseCode}_anonymous");
            //_cache?.Remove("all_courses_anonymous");

            _logger.LogInformation("Job {JobId}: Student {StudentId} enrolled in course {CourseCode} with status {Status}",
                job.Id, jobData.UserId, jobData.CourseCode, enrollment.Status);

            // Update job with result
            job.Result = JsonSerializer.Serialize(new
            {
                enrollment.StudentId,
                enrollment.CourseCode,
                enrollment.EnrolledAt,
                enrollment.Status
            });
            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId}: Enrollment failed: {Message}", job.Id, ex.Message);
            job.Error = ex.Message;
            job.Status = "failed";
            throw; // Allow the job processor to handle retries
        }
    }

    private class EnrollmentJobData
    {
        public string UserId { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
    }
}