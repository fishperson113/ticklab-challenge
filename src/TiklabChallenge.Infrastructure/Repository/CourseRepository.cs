using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Core.Shared;
using TiklabChallenge.Infrastructure.Data;

namespace TiklabChallenge.Infrastructure.Repository
{
    public class CourseRepository : GenericRepository<Course>, ICourseRepository
    {
        public CourseRepository(ApplicationContext context) : base(context)
        {
        }

        public async Task<Course?> GetByCourseCodeAsync(string courseCode, CancellationToken ct = default)
        {
            return await FirstOrDefaultAsync(c => c.CourseCode == courseCode, ct);
        }

        public async Task<IEnumerable<Course?>> GetBySubjectAsync(string subjectCode, CancellationToken ct = default)
        {
            return await FindAsync(c => c.SubjectCode == subjectCode, ct);
        }

        public async Task UpdateScalarsAsync(string courseCode, int? maxEnrollment, string subjectCode, CancellationToken ct = default)
        {
            var existingCourse = await FirstOrDefaultAsync(c => c.CourseCode == courseCode, ct);
            if (existingCourse is null)
                throw new KeyNotFoundException($"Course '{courseCode}' not found.");

            existingCourse.MaxEnrollment = maxEnrollment;
            existingCourse.SubjectCode = subjectCode;
        }
    }
}
