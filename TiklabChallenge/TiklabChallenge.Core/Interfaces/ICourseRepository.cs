using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;

namespace TiklabChallenge.Core.Interfaces
{
    public interface ICourseRepository : IRepository<Course>
    {
        Task<Course?> GetByCourseCodeAsync(string courseCode, CancellationToken ct = default);
        Task<IEnumerable<Course?>> GetBySubjectAsync(string subjectCode, CancellationToken ct = default);
        Task UpdateScalarsAsync(string courseCode, int? maxEnrollment, string subjectCode, CancellationToken ct = default);

    }
}
