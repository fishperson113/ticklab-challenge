using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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
    [Authorize]
    public class SchedulesController : ControllerBase
    {
        private readonly CourseSchedulingService _schedulingService;
        private readonly ILogger<SchedulesController> _logger;
        private readonly IRedisCacheService _cache;
        private readonly UserManager<ApplicationUser> _userManager;

        public SchedulesController(
            CourseSchedulingService schedulingService,
            ILogger<SchedulesController> logger,
            IRedisCacheService cache,
            UserManager<ApplicationUser> userManager)
        {
            _schedulingService = schedulingService;
            _logger = logger;
            _cache = cache;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSchedules(CancellationToken ct = default)
        {
            // Cache-aside: Try to get from cache first
            var user = await _userManager.GetUserAsync(User);
            var cacheKey = user != null ? $"all_schedules_{user.Id}" : "all_schedules_anonymous";

            var schedules = _cache?.Get<IEnumerable<Schedule?>>(cacheKey);
            if (schedules is not null)
            {
                _logger.LogInformation("Retrieved all schedules from cache");
                return Ok(schedules);
            }

            // If not in cache, get from database
            schedules = await _schedulingService.GetAllSchedulesAsync(ct);

            // Store in cache
            _cache?.Set(cacheKey, schedules);

            return Ok(schedules);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSchedule(string id, CancellationToken ct = default)
        {
            // Cache-aside: Try to get from cache first
            var user = await _userManager.GetUserAsync(User);
            var cacheKey = user != null ? $"schedule_{id}_{user.Id}" : $"schedule_{id}_anonymous";

            var schedule = _cache?.Get<Schedule?>(cacheKey);
            if (schedule is not null)
            {
                _logger.LogInformation("Retrieved schedule {Id} from cache", id);
                return Ok(schedule);
            }

            // If not in cache, get from database
            schedule = await _schedulingService.GetScheduleByIdAsync(id, ct);

            if (schedule == null)
                return NotFound($"Schedule with ID '{id}' not found.");

            // Store in cache
            _cache?.Set(cacheKey, schedule);

            return Ok(schedule);
        }

        [HttpGet("room/{roomId}")]
        public async Task<IActionResult> GetSchedulesByRoom(string roomId, CancellationToken ct = default)
        {
            // Cache-aside: Try to get from cache first
            var user = await _userManager.GetUserAsync(User);
            var cacheKey = user != null ? $"room_schedules_{roomId}_{user.Id}" : $"room_schedules_{roomId}_anonymous";

            var schedules = _cache?.Get<IEnumerable<Schedule?>>(cacheKey);
            if (schedules is not null)
            {
                _logger.LogInformation("Retrieved schedules for room {RoomId} from cache", roomId);
                return Ok(schedules);
            }

            // If not in cache, get from database
            schedules = await _schedulingService.GetSchedulesByRoomAsync(roomId, ct);

            // Store in cache
            _cache?.Set(cacheKey, schedules);

            return Ok(schedules);
        }

        [HttpGet("course/{courseCode}")]
        public async Task<IActionResult> GetSchedulesByCourse(string courseCode, CancellationToken ct = default)
        {
            // Cache-aside: Try to get from cache first
            var user = await _userManager.GetUserAsync(User);
            var cacheKey = user != null ? $"course_schedules_{courseCode}_{user.Id}" : $"course_schedules_{courseCode}_anonymous";

            var schedules = _cache?.Get<Schedule?>(cacheKey);
            if (schedules is not null)
            {
                _logger.LogInformation("Retrieved schedules for course {CourseCode} from cache", courseCode);
                return Ok(schedules);
            }

            // If not in cache, get from database
            schedules = await _schedulingService.GetSchedulesByCourseAsync(courseCode, ct);

            // Store in cache
            _cache?.Set(cacheKey, schedules);

            return Ok(schedules);
        }

        [HttpGet("day/{dayOfWeek}")]
        public async Task<IActionResult> GetSchedulesByDay(DayOfWeekCode dayOfWeek, CancellationToken ct = default)
        {
            // Cache-aside: Try to get from cache first
            var user = await _userManager.GetUserAsync(User);
            var cacheKey = user != null ? $"day_schedules_{dayOfWeek}_{user.Id}" : $"day_schedules_{dayOfWeek}_anonymous";

            var schedules = _cache?.Get<IEnumerable<Schedule?>>(cacheKey);
            if (schedules is not null)
            {
                _logger.LogInformation("Retrieved schedules for day {DayOfWeek} from cache", dayOfWeek);
                return Ok(schedules);
            }

            // If not in cache, get from database
            schedules = await _schedulingService.GetSchedulesByDayAsync(dayOfWeek, ct);

            // Store in cache
            _cache?.Set(cacheKey, schedules);

            return Ok(schedules);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> CreateSchedule([FromBody] ScheduleCreateRequest request, CancellationToken ct = default)
        {
            try
            {
                // Validate the request
                if (!request.IsValid())
                    return BadRequest("Invalid schedule data. Start time must be before end time.");

                // Check for schedule conflicts
                bool hasConflicts = await _schedulingService.HasScheduleConflictsAsync(request, ct);
                if (hasConflicts)
                {
                    return BadRequest($"Schedule conflicts with existing schedule in room {request.RoomId}");
                }

                // Write-around: Update database first
                var schedule = await _schedulingService.CreateScheduleAsync(request, ct);

                // Invalidate affected cache entries
                _cache?.Remove("all_schedules_anonymous");

                if (request.RoomId != null)
                {
                    _cache?.Remove($"room_schedules_{request.RoomId}_anonymous");
                }

                if (request.CourseCode != null)
                {
                    _cache?.Remove($"course_schedules_{request.CourseCode}_anonymous");
                    _cache?.Remove($"course_{request.CourseCode}_anonymous");
                }

                _cache?.Remove($"day_schedules_{request.DayOfWeek}_anonymous");

                _logger.LogInformation("Created schedule {Id} for course {CourseCode} and invalidated related caches",
                    schedule.Id, schedule.CourseCode);

                // Return created response
                return CreatedAtAction(nameof(GetSchedule), new { id = schedule.Id }, schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schedule");
                return StatusCode(500, "An error occurred while creating the schedule.");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> UpdateSchedule(
            [FromBody] ScheduleUpdateRequest request,
            CancellationToken ct = default)
        {
            try
            {
                if (!request.HasValidTimeRange())
                    return BadRequest("Invalid time range. Start time must be before end time.");

                // Get original schedule to check if properties changed
                var originalSchedule = await _schedulingService.GetScheduleByIdAsync(request.Id, ct);
                var originalRoomId = originalSchedule?.RoomId;
                var originalCourseCode = originalSchedule?.CourseCode;
                var originalDay = originalSchedule?.DayOfWeek;

                // Write-around: Update database first
                var schedule = await _schedulingService.UpdateScheduleAsync(request, ct);

                if (schedule == null)
                    return NotFound($"Schedule with ID '{request.Id}' not found.");

                // Invalidate affected cache entries
                _cache?.Remove("all_schedules_anonymous");
                _cache?.Remove($"schedule_{request.Id}_anonymous");

                // Invalidate room schedules cache if room changed
                if (originalRoomId != null)
                {
                    _cache?.Remove($"room_schedules_{originalRoomId}_anonymous");
                }

                if (request.RoomId != null && request.RoomId != originalRoomId)
                {
                    _cache?.Remove($"room_schedules_{request.RoomId}_anonymous");
                }

                // Invalidate course schedules cache if course changed
                if (originalCourseCode != null)
                {
                    _cache?.Remove($"course_schedules_{originalCourseCode}_anonymous");
                    _cache?.Remove($"course_{originalCourseCode}_anonymous");
                }

                if (request.CourseCode != null && request.CourseCode != originalCourseCode)
                {
                    _cache?.Remove($"course_schedules_{request.CourseCode}_anonymous");
                    _cache?.Remove($"course_{request.CourseCode}_anonymous");
                }

                // Invalidate day schedules cache if day changed
                if (originalDay != null)
                {
                    _cache?.Remove($"day_schedules_{originalDay}_anonymous");
                }

                if (request.DayOfWeek.HasValue && request.DayOfWeek != originalDay)
                {
                    _cache?.Remove($"day_schedules_{request.DayOfWeek}_anonymous");
                }

                _logger.LogInformation("Updated schedule {Id} and invalidated related caches", request.Id);

                // Get the updated schedule with course details
                var updatedSchedule = await _schedulingService.GetScheduleByIdAsync(request.Id, ct);
                return Ok(updatedSchedule);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid schedule update request");
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Schedule not found");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating schedule {Id}", request.Id);
                return StatusCode(500, "An error occurred while updating the schedule.");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> DeleteSchedule(string id, CancellationToken ct = default)
        {
            try
            {
                // Get the schedule before deleting to know which caches to invalidate
                var scheduleToDelete = await _schedulingService.GetScheduleByIdAsync(id, ct);

                // Write-around: Update database first
                var schedule = await _schedulingService.DeleteScheduleAsync(id, ct);

                if (schedule == null)
                    return NotFound($"Schedule with ID '{id}' not found.");

                // Invalidate affected cache entries
                _cache?.Remove("all_schedules_anonymous");
                _cache?.Remove($"schedule_{id}_anonymous");

                if (scheduleToDelete?.RoomId != null)
                {
                    _cache?.Remove($"room_schedules_{scheduleToDelete.RoomId}_anonymous");
                }

                if (scheduleToDelete?.CourseCode != null)
                {
                    _cache?.Remove($"course_schedules_{scheduleToDelete.CourseCode}_anonymous");
                    _cache?.Remove($"course_{scheduleToDelete.CourseCode}_anonymous");
                }

                if (scheduleToDelete?.DayOfWeek != null)
                {
                    _cache?.Remove($"day_schedules_{scheduleToDelete.DayOfWeek}_anonymous");
                }

                _logger.LogInformation("Deleted schedule {Id} and invalidated related caches", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting schedule");
                return StatusCode(500, "An error occurred while deleting the schedule.");
            }
        }
    }
}