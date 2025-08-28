using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;

namespace TiklabChallenge.Core.Interfaces
{
    public interface IWaitlistRepository : IRepository<Waitlist>
    {
        Task<IEnumerable<Waitlist>> GetByCourseAsync(string courseCode, CancellationToken ct = default);
        Task<Waitlist?> GetByStudentAndCourseAsync(string studentId, string courseCode, CancellationToken ct = default);
    }
}