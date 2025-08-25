using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;

namespace TiklabChallenge.Core.Interfaces
{
    public interface IScheduleRepository : IRepository<Schedule>
    {
        Task<Schedule?> GetByIdWithDetailsAsync(string id, CancellationToken ct = default);
        Task<IEnumerable<Schedule?>> GetByRoomIdAsync(string roomId, CancellationToken ct = default);
        Task<IEnumerable<Schedule?>> GetByCourseCodeAsync(string courseCode, CancellationToken ct = default);
        Task<IEnumerable<Schedule?>> GetByDayOfWeekAsync(DayOfWeekCode dayOfWeek, CancellationToken ct = default);
        Task UpdateScalarsAsync(string scheduleId, string roomId, DayOfWeekCode dayOfWeek,TimeOnly startTime,TimeOnly endTime,
            CancellationToken ct = default);
    }
}
