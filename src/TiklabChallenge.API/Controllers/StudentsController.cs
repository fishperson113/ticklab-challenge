using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Core.Shared;
using TiklabChallenge.UseCases.DTOs;
using TiklabChallenge.UseCases.Services;

namespace TiklabChallenge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.Student)]
    public class StudentsController : ControllerBase
    {
        private readonly ILogger<StudentsController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StudentEnrollmentService _enrollmentService;
        private readonly IRedisCacheService _cache;
        private readonly IJobEnqueuerService _jobEnqueuer;

        public StudentsController(
            ILogger<StudentsController> logger,
            UserManager<ApplicationUser> userManager,
            StudentEnrollmentService enrollmentService,
            IRedisCacheService cache,
            IJobEnqueuerService jobEnqueuer)
        {
            _logger = logger;
            _userManager = userManager;
            _enrollmentService = enrollmentService;
            _cache = cache;
            _jobEnqueuer = jobEnqueuer;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null) return Unauthorized("Cannot determine current user.");
            var cacheKey = $"student_profile_{userId}";
            var studentProfile = _cache?.Get<Student>(cacheKey);

            if (studentProfile is not null)
            {
                _logger.LogInformation("Retrieved student profile for {UserId} from cache", userId);
                return Ok(new
                {
                    studentProfile.UserId,
                    studentProfile.StudentCode,
                    studentProfile.FullName
                });
            }

            var student = await _enrollmentService.GetStudentProfileAsync(userId);
            if (student is null) return NotFound("Student profile not found.");

            _cache?.Set(cacheKey, student);

            return Ok(new
            {
                student.UserId,
                student.StudentCode,
                student.FullName
            });
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest dto, CancellationToken ct)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null) return Unauthorized("Cannot determine current user.");

            var student = await _enrollmentService.GetStudentProfileAsync(userId, ct);
            if (student is null) return NotFound("Student profile not found.");

            if (dto.StudentCode != null)
            {
                student.StudentCode = dto.StudentCode;
            }

            if (dto.FullName != null)
            {
                student.FullName = dto.FullName;
            }

            await _enrollmentService.UpdateStudentProfileAsync(student, ct);
            _cache?.Remove($"student_profile_{userId}");

            _cache?.Remove($"student_enrollments_{userId}");

            return Ok();
        }

        [HttpGet("enrollments")]
        public async Task<IActionResult> GetMyEnrollments(CancellationToken ct = default)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null) return Unauthorized("Cannot determine current user.");

            var cachingKey = $"student_enrollments_{userId}";
            var enrollmentDetails = await _cache.GetAsync<IEnumerable<EnrollmentDetails?>>(cachingKey, 150, ct);

            if (enrollmentDetails is not null)
            {
                _logger.LogInformation("Retrieved enrollment details for {UserId} from cache", userId);
                return Ok(enrollmentDetails);
            }

            enrollmentDetails = await _enrollmentService.GetEnrollmentDetailsAsync(userId, ct);

            _cache?.Set(cachingKey, enrollmentDetails);

            return Ok(enrollmentDetails);
        }

        [HttpPost("enrollments")]
        public async Task<IActionResult> EnrollInCourse([FromBody] CourseEnrollmentRequest request, CancellationToken ct = default)
        {
            //try
            //{
            //    var userId = _userManager.GetUserId(User);
            //    if (userId is null)
            //        return Unauthorized("Cannot determine current user.");

            //    var enrollment = await _enrollmentService.EnrollStudentInCourseAsync(userId, request, ct);

            //    _cache?.Remove($"student_enrollments_{userId}");
            //    _cache?.Remove($"course_{request.CourseCode}_anonymous");
            //    _cache?.Remove("all_courses_anonymous");

            //    _logger.LogInformation("Student {StudentId} enrolled in course {CourseCode}", userId, request.CourseCode);

            //    return CreatedAtAction(nameof(GetMyEnrollments), null, new
            //    {
            //        enrollment.StudentId,
            //        enrollment.CourseCode,
            //        enrollment.EnrolledAt,
            //        enrollment.Status
            //    });
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogWarning(ex, "Enrollment failed: {Message}", ex.Message);
            //    return BadRequest(ex.Message);
            //}

            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId is null)
                    return Unauthorized("Cannot determine current user.");

                // Create a job for enrollment processing
                var jobData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    userId,
                    courseCode = request.CourseCode
                });

                var job = new Job
                {
                    Name = "student-enrollment",
                    Data = jobData,
                    Priority = 5, // Medium priority
                    MaxAttempts = 2
                };

                // Enqueue the job
                var jobId = await _jobEnqueuer.EnqueueJobAsync(job);

                _logger.LogInformation("Enrollment job queued: {JobId} for student {StudentId} in course {CourseCode}",
                    jobId, userId, request.CourseCode);

                return Accepted(new
                {
                    JobId = jobId,
                    Message = "Enrollment request has been queued for processing",
                    StudentId = userId,
                    CourseCode = request.CourseCode
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Enrollment job creation failed: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("enrollments/{courseCode}")]
        public async Task<IActionResult> WithdrawFromCourse(string courseCode, CancellationToken ct = default)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId is null)
                    return Unauthorized("Cannot determine current user.");

                await _enrollmentService.WithdrawFromCourseAsync(userId, courseCode, ct);

                _cache?.Remove($"student_enrollments_{userId}");
                _cache?.Remove($"course_{courseCode}_anonymous");
                _cache?.Remove("all_courses_anonymous");

                _logger.LogInformation("Student {StudentId} withdrew from course {CourseCode}", userId, courseCode);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Withdrawal failed: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
        }
    }
}