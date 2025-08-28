using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;

namespace TiklabChallenge.Core.Shared
{
    public static class ScheduleValidator
    {
        public static bool IsTimeOverlap(TimeOnly start1, TimeOnly end1, TimeOnly start2, TimeOnly end2)
        {
            return (start1 < end2 && start2 < end1);
        }
        public static async Task<(bool isValid, string errorMessage)> ValidateScheduleConflictsAsync<TEntity>(
            DbSet<TEntity> dbSet,
            Schedule schedule,
            string identifierFieldName,
            string identifierValue,
            Func<IQueryable<TEntity>, IQueryable<TEntity>> includeFunc,
            Func<TEntity, Schedule> getScheduleFunc,
            CancellationToken ct = default) where TEntity : class
        {
            var query = dbSet.AsQueryable();

            if (includeFunc != null)
            {
                query = includeFunc(query);
            }

            var filteredEntities = await query
                .Where(e => EF.Property<string>(e, identifierFieldName) != identifierValue)
                .ToListAsync(ct);

            var entitiesWithPotentialConflicts = filteredEntities
                .Select(e => getScheduleFunc(e))
                .Where(s => s != null &&
                            s.RoomId == schedule.RoomId &&
                            s.DayOfWeek == schedule.DayOfWeek)
                .ToList();

            foreach (var existingSchedule in entitiesWithPotentialConflicts)
            {
                if (IsTimeOverlap(
                    existingSchedule.StartTime, existingSchedule.EndTime,
                    schedule.StartTime, schedule.EndTime))
                {
                    return (false,
                        $"Schedule conflicts with course '{existingSchedule.CourseCode ?? "unknown"}' " +
                        $"in room {schedule.RoomId} on {schedule.DayOfWeek} " +
                        $"from {existingSchedule.StartTime} to {existingSchedule.EndTime}");
                }
            }

            return (true, string.Empty);
        }
    }
}
