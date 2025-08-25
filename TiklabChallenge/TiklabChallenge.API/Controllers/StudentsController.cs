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

        public StudentsController(
            ILogger<StudentsController> logger,
            UserManager<ApplicationUser> userManager,
            StudentEnrollmentService enrollmentService)
        {
            _logger = logger;
            _userManager = userManager;
            _enrollmentService = enrollmentService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized("Cannot determine current user.");

            var student = await _enrollmentService.GetStudentProfileAsync(user.Id);
            if (student is null) return NotFound("Student profile not found.");

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

            return Ok();
        }

        [HttpGet("enrollments")]
        public async Task<IActionResult> GetMyEnrollments(CancellationToken ct = default)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized("Cannot determine current user.");

            var enrollmentDetails = await _enrollmentService.GetEnrollmentDetailsAsync(user.Id, ct);

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
    }
}