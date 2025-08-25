using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Infrastructure.Data;
using TiklabChallenge.UseCases.DTOs;

namespace TiklabChallenge.UseCases.Services
{
    public class CourseSchedulingService
    {
        private readonly IUnitOfWork _uow;

        public CourseSchedulingService(IUnitOfWork uow)
        {
            _uow = uow;
        }
        public async Task<Course?> CreateCourseAsync(CourseCreateRequest req, CancellationToken ct = default)
        {
            var course = new Course
            {
                CourseCode = req.CourseCode,
                SubjectCode = req.SubjectCode,
                MaxEnrollment = req.MaxEnrollment,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Courses.AddAsync(course);

            if (req.Schedule != null)
            {
                var scheduleReq = new ScheduleCreateRequest
                {
                    RoomId = req.Schedule.RoomId,
                    CourseCode = req.CourseCode,
                    DayOfWeek = req.Schedule.DayOfWeek,
                    StartTime = req.Schedule.StartTime,
                    EndTime = req.Schedule.EndTime
                };

                await CreateScheduleAsync(scheduleReq, ct);
            }

            await _uow.CommitAsync();
            return course;
        }
        public async Task<Course?> UpdateCourseAsync(CourseUpdateRequest req, CancellationToken ct = default)
        {
            var course = await _uow.Courses.GetByCourseCodeAsync(req.CourseCode, ct);
            if (course == null)
                throw new KeyNotFoundException($"Course with code '{req.CourseCode}' not found.");

            if (req.SubjectCode != null || req.MaxEnrollment.HasValue)
            {
                // Use existing subject code if not provided in request
                string subjectCode = req.SubjectCode ?? course.SubjectCode;

                await _uow.Courses.UpdateScalarsAsync(
                    req.CourseCode,
                    req.MaxEnrollment,
                    subjectCode,
                    ct);
            }
            // Handle schedule update if provided
            if (req.Schedule != null)
            {
                // Get existing schedule for this course
                var existingSchedule = await _uow.Schedules.GetByCourseCodeAsync(req.CourseCode, ct);

                if (existingSchedule != null)
                {
                    // If the schedule has an ID field, make sure it matches the existing schedule
                    if (!string.IsNullOrEmpty(req.Schedule.Id) && existingSchedule.Id != req.Schedule.Id)
                    {
                        // The provided schedule ID doesn't match the existing one - detach old schedule and attach new one
                        await DetachScheduleAsync(req.CourseCode, ct);
                        await UpdateScheduleAsync(req.Schedule, ct);
                    }
                    else
                    {
                        // Update existing schedule
                        req.Schedule.Id = existingSchedule.Id; // Ensure ID is set correctly
                        await UpdateScheduleAsync(req.Schedule, ct);
                    }
                }
                else
                {
                    // No existing schedule - create a new one
                    var scheduleCreateRequest = new ScheduleCreateRequest
                    {
                        RoomId = req.Schedule.RoomId,
                        CourseCode = req.CourseCode,
                        DayOfWeek = req.Schedule.DayOfWeek ?? DayOfWeekCode.Monday,
                        StartTime = req.Schedule.StartTime ?? new TimeOnly(8, 0),
                        EndTime = req.Schedule.EndTime ?? new TimeOnly(10, 0)
                    };

                    await CreateScheduleAsync(scheduleCreateRequest, ct);
                }
            }

            course = await _uow.Courses.GetByCourseCodeAsync(req.CourseCode, ct);

            await _uow.CommitAsync();
            return course;
        }
        public async Task AttachScheduleAsync(string courseCode, string scheduleId, CancellationToken ct = default)
        {
            var course = await _uow.Courses.GetByCourseCodeAsync(courseCode, ct);
            if (course == null)
                throw new KeyNotFoundException($"Course with code '{courseCode}' not found.");

            var schedule = await _uow.Schedules.GetByIdAsync(scheduleId, ct);
            if (schedule == null)
                throw new KeyNotFoundException($"Schedule with ID '{scheduleId}' not found.");

            schedule.CourseCode = courseCode;

            await _uow.CommitAsync();
        }
        public async Task DetachScheduleAsync(string courseCode, CancellationToken ct = default)
        {
            var schedule = await _uow.Schedules.GetByCourseCodeAsync(courseCode, ct);

            if (schedule != null)
            {
                schedule.CourseCode = null;
            }

            await _uow.CommitAsync();
        }
        public async Task<Schedule?> UpdateScheduleAsync(ScheduleUpdateRequest req, CancellationToken ct = default)
        {
            var schedule = await _uow.Schedules.GetByIdAsync(req.Id, ct);

            if (schedule == null)
                throw new KeyNotFoundException($"Schedule with code '{req.Id}' not found.");
            if (req.RoomId != null)
                schedule.RoomId = req.RoomId;

            if (req.CourseCode != null)
                schedule.CourseCode = req.CourseCode;

            if (req.DayOfWeek.HasValue)
                schedule.DayOfWeek = req.DayOfWeek.Value;

            if (req.StartTime.HasValue)
                schedule.StartTime = req.StartTime.Value;

            if (req.EndTime.HasValue)
                schedule.EndTime = req.EndTime.Value;
            if (!req.HasValidTimeRange())
                throw new ArgumentException("Invalid time range: Start time must be before end time");

            if (req.RoomId != null && req.DayOfWeek.HasValue && req.StartTime.HasValue && req.EndTime.HasValue)
            {
                await _uow.Schedules.UpdateScalarsAsync(
                    req.Id,
                    req.RoomId,
                    req.DayOfWeek.Value,
                    req.StartTime.Value,
                    req.EndTime.Value,
                    ct);
            }
            await _uow.CommitAsync();
            return schedule;
        }

        public async Task<Schedule> CreateScheduleAsync(ScheduleCreateRequest req, CancellationToken ct = default)
        {
            var schedule = new Schedule
            {
                Id = Guid.NewGuid().ToString(),
                RoomId = req.RoomId,
                CourseCode = req.CourseCode,
                DayOfWeek = req.DayOfWeek,
                StartTime = req.StartTime,
                EndTime = req.EndTime
            };

            await _uow.Schedules.AddAsync(schedule);
            await _uow.CommitAsync();

            return schedule;
        }
        public async Task<IEnumerable<Course?>> GetAllCoursesAsync(CancellationToken ct = default)
        {
            return await _uow.Courses.GetAllAsync(ct);
        }

        public async Task<Course?> GetByCourseCodeAsync(string courseCode, CancellationToken ct = default)
        {
            return await _uow.Courses.GetByCourseCodeAsync(courseCode, ct);
        }

        public async Task<IEnumerable<Course?>> GetCoursesBySubjectAsync(string subjectCode, CancellationToken ct = default)
        {
            return await _uow.Courses.GetBySubjectAsync(subjectCode, ct);
        }
        public async Task<IEnumerable<Course?>> GetBySubjectAsync(string subjectCode, CancellationToken ct = default)
        {
            return await _uow.Courses.GetBySubjectAsync(subjectCode, ct);
        }
        public async Task<IEnumerable<Schedule?>> GetAllSchedulesAsync(CancellationToken ct = default)
        {
            return await _uow.Schedules.GetAllAsync(ct);
        }

        public async Task<Schedule?> GetScheduleByIdAsync(string id, CancellationToken ct = default)
        {
            return await _uow.Schedules.GetByIdWithDetailsAsync(id, ct);
        }

        public async Task<IEnumerable<Schedule?>> GetSchedulesByRoomAsync(string roomId, CancellationToken ct = default)
        {
            return await _uow.Schedules.GetByRoomIdAsync(roomId, ct);
        }

        public async Task<Schedule?> GetSchedulesByCourseAsync(string courseCode, CancellationToken ct = default)
        {
            return await _uow.Schedules.GetByCourseCodeAsync(courseCode, ct);
        }

        public async Task<IEnumerable<Schedule?>> GetSchedulesByDayAsync(DayOfWeekCode dayOfWeek, CancellationToken ct = default)
        {
            return await _uow.Schedules.GetByDayOfWeekAsync(dayOfWeek, ct);
        }

        public async Task<bool> HasScheduleConflictsAsync(ScheduleCreateRequest request, CancellationToken ct = default)
        {
            var existingSchedules = await _uow.Schedules.GetByRoomIdAsync(request.RoomId, ct);
            foreach (var existingSchedule in existingSchedules)
            {
                if (existingSchedule?.DayOfWeek == request.DayOfWeek)
                {
                    // Check if time ranges overlap
                    if (request.StartTime < existingSchedule.EndTime && request.EndTime > existingSchedule.StartTime)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public async Task<Schedule?> DeleteScheduleAsync(string id, CancellationToken ct = default)
        {
            var schedule = await _uow.Schedules.GetByIdAsync(id, ct);
            if (schedule == null)
                return null;

            await _uow.Schedules.DeleteAsync(schedule);
            await _uow.CommitAsync();

            return schedule;
        }

    }
}