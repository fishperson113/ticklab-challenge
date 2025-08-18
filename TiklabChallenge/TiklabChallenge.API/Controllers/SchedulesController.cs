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

namespace TiklabChallenge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SchedulesController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<SchedulesController> _logger;

        public SchedulesController(IUnitOfWork uow, ILogger<SchedulesController> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSchedules(CancellationToken ct = default)
        {
            var schedules = await _uow.Schedules.GetAllAsync(ct);
            return Ok(schedules);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSchedule(string id, CancellationToken ct = default)
        {
            var schedule = await _uow.Schedules.GetByIdWithDetailsAsync(id, ct);

            if (schedule == null)
                return NotFound($"Schedule with ID '{id}' not found.");

            return Ok(schedule);
        }

        [HttpGet("room/{roomId}")]
        public async Task<IActionResult> GetSchedulesByRoom(string roomId, CancellationToken ct = default)
        {
            var schedules = await _uow.Schedules.GetByRoomIdAsync(roomId, ct);
            return Ok(schedules);
        }

        [HttpGet("course/{courseCode}")]
        public async Task<IActionResult> GetSchedulesByCourse(string courseCode, CancellationToken ct = default)
        {
            var schedules = await _uow.Schedules.GetByCourseCodeAsync(courseCode, ct);
            return Ok(schedules);
        }

        [HttpGet("day/{dayOfWeek}")]
        public async Task<IActionResult> GetSchedulesByDay(DayOfWeekCode dayOfWeek, CancellationToken ct = default)
        {
            var schedules = await _uow.Schedules.GetByDayOfWeekAsync(dayOfWeek, ct);
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

                // Create a new schedule
                var schedule = new Schedule
                {
                    Id = Guid.NewGuid().ToString(),
                    RoomId = request.RoomId,
                    CourseCode = request.CourseCode,
                    DayOfWeek = request.DayOfWeek,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime
                };

                // Check for schedule conflicts
                var existingSchedules = await _uow.Schedules.GetByRoomIdAsync(request.RoomId, ct);
                foreach (var existingSchedule in existingSchedules)
                {
                    if (existingSchedule?.DayOfWeek == request.DayOfWeek)
                    {
                        // Check if time ranges overlap
                        if (request.StartTime < existingSchedule.EndTime && request.EndTime > existingSchedule.StartTime)
                        {
                            return BadRequest($"Schedule conflicts with existing schedule in room {request.RoomId}");
                        }
                    }
                }

                // Add the schedule to the repository
                await _uow.Schedules.AddAsync(schedule);
                await _uow.CommitAsync();

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

                // Get the existing schedule
                var schedule = await _uow.Schedules.GetByIdAsync(request.Id, ct);
                if (schedule == null)
                    return NotFound($"Schedule with ID '{request.Id}' not found.");

                // Update schedule properties
                if (request.RoomId != null)
                    schedule.RoomId = request.RoomId;

                if (request.CourseCode != null)
                    schedule.CourseCode = request.CourseCode;

                if (request.DayOfWeek.HasValue)
                    schedule.DayOfWeek = request.DayOfWeek.Value;

                if (request.StartTime.HasValue)
                    schedule.StartTime = request.StartTime.Value;

                if (request.EndTime.HasValue)
                    schedule.EndTime = request.EndTime.Value;

                // If all necessary properties are updated, use UpdateScalarsAsync
                if (request.RoomId != null && request.DayOfWeek.HasValue &&
                    request.StartTime.HasValue && request.EndTime.HasValue)
                {
                    await _uow.Schedules.UpdateScalarsAsync(
                        request.Id,
                        request.RoomId,
                        request.DayOfWeek.Value,
                        request.StartTime.Value,
                        request.EndTime.Value,
                        ct);
                }

                await _uow.CommitAsync();

                _logger.LogInformation("Updated schedule {Id}", request.Id);

                // Get the updated schedule with course details
                var updatedSchedule = await _uow.Schedules.GetByIdWithDetailsAsync(request.Id, ct);
                return Ok(updatedSchedule);
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
                var schedule = await _uow.Schedules.GetByIdAsync(id, ct);
                if (schedule == null)
                    return NotFound($"Schedule with ID '{id}' not found.");

                await _uow.Schedules.DeleteAsync(schedule);
                await _uow.CommitAsync();

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