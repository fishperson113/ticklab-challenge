using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Diagnostics;
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
        private readonly IRedisCacheService _cache;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly BusinessMetricsService _metricsService;

        public CoursesController(CourseSchedulingService courseService, ILogger<CoursesController> logger,
            IRedisCacheService cache, UserManager<ApplicationUser> userManager
            , BusinessMetricsService metricsService)
        {
            _logger = logger;
            _courseService = courseService;
            _cache = cache;
            _userManager = userManager;
            _metricsService = metricsService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCourses(CancellationToken ct = default)
        {
            var cachingKey = "all_courses";
            var courses = await _cache.GetAsync<IEnumerable<Course?>>(cachingKey, 150, ct);

            if (courses is not null)
            {
                _logger.LogInformation("Retrieved all courses from cache");
                return Ok(courses);
            }

            courses = await _courseService.GetAllCoursesAsync(ct);
            _cache?.Set(cachingKey, courses);

            return Ok(courses);
        }

        [HttpGet("{courseCode}")]
        public async Task<IActionResult> GetCourse(string courseCode, CancellationToken ct = default)
        {
            var userId = _userManager.GetUserId(User);
            var cacheKey = userId != null ? $"course_{courseCode}_{userId}" : $"course_{courseCode}_anonymous";
            var course = _cache?.Get<Course?>(cacheKey);
            if (course is not null)
            {
                _logger.LogInformation("Retrieved course {CourseCode} from cache", courseCode);
                return Ok(course);
            }

            // If not in cache, get from database
            course = await _courseService.GetByCourseCodeAsync(courseCode, ct);

            if (course == null)
                return NotFound($"Course with code '{courseCode}' not found.");

            _cache?.Set(cacheKey, course);

            return Ok(course);
        }

        [HttpGet("subject/{subjectCode}")]
        public async Task<IActionResult> GetCoursesBySubject(string subjectCode, CancellationToken ct = default)
        {
            var userId = _userManager.GetUserId(User);
            var cacheKey = userId != null ? $"subject_courses_{subjectCode}_{userId}" : $"subject_courses_{subjectCode}_anonymous";

            var courses = _cache?.Get<IEnumerable<Course?>>(cacheKey);
            if (courses is not null)
            {
                _logger.LogInformation("Retrieved courses for subject {SubjectCode} from cache", subjectCode);
                return Ok(courses);
            }

            courses = await _courseService.GetBySubjectAsync(subjectCode, ct);
            _cache?.Set(cacheKey, courses);

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

                _cache?.Remove("all_courses_anonymous");

                _cache?.Remove($"subject_courses_{course.SubjectCode}_anonymous");

                _logger.LogInformation("Invalidated caches after creating course {CourseCode}", course.CourseCode);

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
                var originalCourse = await _courseService.GetByCourseCodeAsync(request.CourseCode, ct);
                var originalSubjectCode = originalCourse?.SubjectCode;
                var course = await _courseService.UpdateCourseAsync(request, ct);

                if (course == null)
                    return NotFound($"Course with code '{request.CourseCode}' not found.");
                _cache?.Remove("all_courses_anonymous");

                _cache?.Remove($"course_{course.CourseCode}_anonymous");

                _cache?.Remove($"subject_courses_{course.SubjectCode}_anonymous");

                if (originalSubjectCode != null && originalSubjectCode != course.SubjectCode)
                {
                    _cache?.Remove($"subject_courses_{originalSubjectCode}_anonymous");
                }

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