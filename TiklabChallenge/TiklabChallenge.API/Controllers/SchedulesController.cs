using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        public SchedulesController(CourseSchedulingService schedulingService, ILogger<SchedulesController> logger)
        {
            _schedulingService = schedulingService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSchedules(CancellationToken ct = default)
        {
            var schedules = await _schedulingService.GetAllSchedulesAsync(ct);
            return Ok(schedules);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSchedule(string id, CancellationToken ct = default)
        {
            var schedule = await _schedulingService.GetScheduleByIdAsync(id, ct);

            if (schedule == null)
                return NotFound($"Schedule with ID '{id}' not found.");

            return Ok(schedule);
        }

        [HttpGet("room/{roomId}")]
        public async Task<IActionResult> GetSchedulesByRoom(string roomId, CancellationToken ct = default)
        {
            var schedules = await _schedulingService.GetSchedulesByRoomAsync(roomId, ct);
            return Ok(schedules);
        }

        [HttpGet("course/{courseCode}")]
        public async Task<IActionResult> GetSchedulesByCourse(string courseCode, CancellationToken ct = default)
        {
            var schedules = await _schedulingService.GetSchedulesByCourseAsync(courseCode, ct);
            return Ok(schedules);
        }

        [HttpGet("day/{dayOfWeek}")]
        public async Task<IActionResult> GetSchedulesByDay(DayOfWeekCode dayOfWeek, CancellationToken ct = default)
        {
            var schedules = await _schedulingService.GetSchedulesByDayAsync(dayOfWeek, ct);
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

                // Create the schedule using the service
                var schedule = await _schedulingService.CreateScheduleAsync(request, ct);

                _logger.LogInformation("Created schedule {Id} for course {CourseCode}", schedule.Id, schedule.CourseCode);

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

                var schedule = await _schedulingService.UpdateScheduleAsync(request, ct);

                if (schedule == null)
                    return NotFound($"Schedule with ID '{request.Id}' not found.");

                _logger.LogInformation("Updated schedule {Id}", request.Id);

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
                var schedule = await _schedulingService.DeleteScheduleAsync(id, ct);

                if (schedule == null)
                    return NotFound($"Schedule with ID '{id}' not found.");

                _logger.LogInformation("Deleted schedule {Id}", id);

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