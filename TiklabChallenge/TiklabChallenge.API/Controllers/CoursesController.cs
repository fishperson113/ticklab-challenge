using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Core.Shared;
using TiklabChallenge.UseCases.DTOs;
using TiklabChallenge.UseCases.Services;

namespace TiklabChallenge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CoursesController : ControllerBase
    {
        private readonly ILogger<CoursesController> _logger;
        private readonly CourseSchedulingService _courseService;

        public CoursesController(CourseSchedulingService courseService, ILogger<CoursesController> logger)
        {
            _logger = logger;
            _courseService = courseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCourses(CancellationToken ct = default)
        {
            var courses = await _courseService.GetAllCoursesAsync(ct);
            return Ok(courses);
        }

        [HttpGet("{courseCode}")]
        public async Task<IActionResult> GetCourse(string courseCode, CancellationToken ct = default)
        {
            var course = await _courseService.GetByCourseCodeAsync(courseCode, ct);

            if (course == null)
                return NotFound($"Course with code '{courseCode}' not found.");

            return Ok(course);
        }

        [HttpGet("subject/{subjectCode}")]
        public async Task<IActionResult> GetCoursesBySubject(string subjectCode, CancellationToken ct = default)
        {
            var courses = await _courseService.GetBySubjectAsync(subjectCode, ct);
            return Ok(courses);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> CreateCourse([FromBody] CourseCreateRequest request, CancellationToken ct = default)
        {
            try
            {
                var course = await _courseService.CreateCourseAsync(request, ct);

                if (course == null)
                    return BadRequest("Failed to create course");

                _logger.LogInformation("Created course {CourseCode}", course.CourseCode);
                return CreatedAtAction(nameof(GetCourse), new { courseCode = course.CourseCode }, course);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid course creation request");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course {CourseCode}", request.CourseCode);
                return StatusCode(500, "An error occurred while creating the course.");
            }
        }

        [HttpPut("{courseCode}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> UpdateCourse(
            [FromBody] CourseUpdateRequest request,
            CancellationToken ct = default)
        {
            try
            {
                var course = await _courseService.UpdateCourseAsync(request, ct);

                if (course == null)
                    return NotFound($"Course with code '{request.CourseCode}' not found.");

                _logger.LogInformation("Updated course {CourseCode}", request.CourseCode);
                return Ok(course);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Course not found for update");
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid course update request");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course {CourseCode}", request.CourseCode);
                return StatusCode(500, "An error occurred while updating the course.");
            }
        }
    }
}
