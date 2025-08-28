using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Infrastructure.Data;

namespace TiklabChallenge.Infrastructure.Repository
{
    public class WaitlistRepository : GenericRepository<Waitlist>, IWaitlistRepository
    {
        public WaitlistRepository(ApplicationContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Waitlist>> GetByCourseAsync(string courseCode, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(w => w.CourseCode == courseCode)
                .OrderBy(w => w.CreatedAt)
                .ToListAsync(ct);
        }
        public async Task<Waitlist?> GetByStudentAndCourseAsync(string studentId, string courseCode, CancellationToken ct = default)
        {
            return await FirstOrDefaultAsync(w => w.StudentId == studentId && w.CourseCode == courseCode, ct);
        }
    }
}