using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Core.Shared;
using TiklabChallenge.Infrastructure.Data;

namespace TiklabChallenge.Infrastructure.Repository
{
    public class ScheduleRepository : GenericRepository<Schedule>, IScheduleRepository
    {
        public ScheduleRepository(ApplicationContext context) : base(context)
        {
        }

        public async Task<Schedule?> GetByIdWithDetailsAsync(string id, CancellationToken ct = default)
        {
            var query = _dbSet.AsQueryable();
            query = query.Include(s => s.Course);

            return await query.FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        public async Task<IEnumerable<Schedule?>> GetByRoomIdAsync(string roomId, CancellationToken ct = default)
        {
            return await FindAsync(s => s.RoomId == roomId, ct);
        }

        public async Task<IEnumerable<Schedule?>> GetByCourseCodeAsync(string courseCode, CancellationToken ct = default)
        {
            return await FindAsync(s => s.CourseCode == courseCode, ct);
        }

        public async Task<IEnumerable< Schedule?>> GetByDayOfWeekAsync(DayOfWeekCode dayOfWeek, CancellationToken ct = default)
        {
            return await FindAsync(s => s.DayOfWeek == dayOfWeek, ct);
        }

        public async Task UpdateScalarsAsync(string scheduleId, string roomId,
            DayOfWeekCode dayOfWeek, TimeOnly startTime, TimeOnly endTime, CancellationToken ct = default)
        {
            var existingSchedule = await FirstOrDefaultAsync(s => s.Id == scheduleId, ct);
            if (existingSchedule == null)
                throw new KeyNotFoundException($"Schedule with ID '{scheduleId}' not found.");

            existingSchedule.RoomId = roomId;
            existingSchedule.DayOfWeek = dayOfWeek;
            existingSchedule.StartTime = startTime;
            existingSchedule.EndTime = endTime;
        }
    }
}