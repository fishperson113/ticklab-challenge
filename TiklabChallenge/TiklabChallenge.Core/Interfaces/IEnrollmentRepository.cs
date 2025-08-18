using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;

namespace TiklabChallenge.Core.Interfaces
{
    public interface IEnrollmentRepository : IRepository<Enrollment>
    {
        Task<IEnumerable<Enrollment?>> FindAsync(string studentId, string courseCode, CancellationToken ct = default);
        Task<bool> ExistsAsync(string studentId, string courseCode, CancellationToken ct = default);
        Task<int> CountEnrolledAsync(string courseCode, CancellationToken ct = default);
        Task<IEnumerable<Enrollment?>> GetByStudentAsync(string studentId, CancellationToken ct = default);
        Task<IEnumerable<Enrollment?>> GetByCourseAsync(string courseCode, CancellationToken ct = default);
        Task<Enrollment?> FirstOrDefaultAsync(string studentId, string courseCode, CancellationToken ct = default);
    }
}
