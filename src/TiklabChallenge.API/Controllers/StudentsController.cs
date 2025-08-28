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

        public StudentsController(
            ILogger<StudentsController> logger,
            UserManager<ApplicationUser> userManager,
            StudentEnrollmentService enrollmentService,
            IRedisCacheService cache)
        {
            _logger = logger;
            _userManager = userManager;
            _enrollmentService = enrollmentService;
            _cache = cache;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized("Cannot determine current user.");
            var cacheKey = $"student_profile_{user.Id}";
            var studentProfile = _cache?.Get<Student>(cacheKey);

            if (studentProfile is not null)
            {
                _logger.LogInformation("Retrieved student profile for {UserId} from cache", user.Id);
                return Ok(new
                {
                    studentProfile.UserId,
                    studentProfile.StudentCode,
                    studentProfile.FullName
                });
            }

            var student = await _enrollmentService.GetStudentProfileAsync(user.Id);
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
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized("Cannot determine current user.");

            var student = await _enrollmentService.GetStudentProfileAsync(user.Id, ct);
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
            _cache?.Remove($"student_profile_{user.Id}");

            _cache?.Remove($"student_enrollments_{user.Id}");

            return Ok();
        }

        [HttpGet("enrollments")]
        public async Task<IActionResult> GetMyEnrollments(CancellationToken ct = default)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized("Cannot determine current user.");

            var cacheKey = $"student_enrollments_{user.Id}";
            var enrollmentDetails = _cache?.Get<IEnumerable<object>>(cacheKey);

            if (enrollmentDetails is not null)
            {
                _logger.LogInformation("Retrieved enrollment details for {UserId} from cache", user.Id);
                return Ok(enrollmentDetails);
            }

            enrollmentDetails = await _enrollmentService.GetEnrollmentDetailsAsync(user.Id, ct);

            _cache?.Set(cacheKey, enrollmentDetails);

            return Ok(enrollmentDetails);
        }

        [HttpPost("enrollments")]
        public async Task<IActionResult> EnrollInCourse([FromBody] CourseEnrollmentRequest request, CancellationToken ct = default)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user is null)
                    return Unauthorized("Cannot determine current user.");

                var enrollment = await _enrollmentService.EnrollStudentInCourseAsync(user.Id, request, ct);

                _cache?.Remove($"student_enrollments_{user.Id}");
                _cache?.Remove($"course_{request.CourseCode}_anonymous");
                _cache?.Remove("all_courses_anonymous");

                _logger.LogInformation("Student {StudentId} enrolled in course {CourseCode}", user.Id, request.CourseCode);

                return CreatedAtAction(nameof(GetMyEnrollments), null, new
                {
                    enrollment.StudentId,
                    enrollment.CourseCode,
                    enrollment.EnrolledAt,
                    enrollment.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Enrollment failed: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
        }
        [HttpDelete("enrollments/{courseCode}")]
        public async Task<IActionResult> WithdrawFromCourse(string courseCode, CancellationToken ct = default)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user is null)
                    return Unauthorized("Cannot determine current user.");

                await _enrollmentService.WithdrawFromCourseAsync(user.Id, courseCode, ct);

                _cache?.Remove($"student_enrollments_{user.Id}");
                _cache?.Remove($"course_{courseCode}_anonymous");
                _cache?.Remove("all_courses_anonymous");

                _logger.LogInformation("Student {StudentId} withdrew from course {CourseCode}", user.Id, courseCode);

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