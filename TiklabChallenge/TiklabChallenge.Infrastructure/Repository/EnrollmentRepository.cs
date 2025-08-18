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
    public class EnrollmentRepository : GenericRepository<Enrollment>, IEnrollmentRepository
    {
        public EnrollmentRepository(ApplicationContext context) : base(context)
        {
        }

        public async Task<int> CountEnrolledAsync(string courseCode, CancellationToken ct = default)
        {
            return await _context.Enrollments
                .CountAsync<Enrollment>(e => e.CourseCode == courseCode && e.Status == EnrollmentStatus.Enrolled, ct);
        }

        public async Task<bool> ExistsAsync(string studentId, string courseCode, CancellationToken ct = default)
        {
            return await ExistsAsync(studentId, courseCode, ct);
        }

        public async Task<IEnumerable<Enrollment?>> FindAsync(string studentId, string courseCode, CancellationToken ct = default)
        {
            return await FindAsync(e => e.StudentId == studentId && e.CourseCode == courseCode, ct);
        }

        public async Task<Enrollment?> FirstOrDefaultAsync(string studentId, string courseCode, CancellationToken ct = default)
        {
            return await FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseCode == courseCode, ct);
        }

        public async Task<IEnumerable<Enrollment?>> GetByCourseAsync(string courseCode, CancellationToken ct = default)
        {
            return await FindAsync(e => e.CourseCode == courseCode, ct);
        }

        public async Task<IEnumerable<Enrollment?>> GetByStudentAsync(string studentId, CancellationToken ct = default)
        {
            return await FindAsync(e => e.StudentId == studentId, ct);
        }
    }
}
