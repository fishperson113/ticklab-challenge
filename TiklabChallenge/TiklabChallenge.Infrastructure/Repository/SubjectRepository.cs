using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Infrastructure.Data;

namespace TiklabChallenge.Infrastructure.Repository
{
    public class SubjectRepository : GenericRepository<Subject>, ISubjectRepository
    {
        public SubjectRepository(ApplicationContext context) : base(context)
        {
        }

        public async Task<Subject?> GetBySubjectCodeAsync(string subjectCode, CancellationToken ct = default)
        {
            var query = _dbSet.AsQueryable();
            query = query.Include(s => s.PrerequisiteSubject)
                         .Include(s => s.RequiredBySubjects);

            return await query.FirstOrDefaultAsync(s => s.SubjectCode == subjectCode, ct);
        }

        public async Task<Subject?> GetWithPrerequisiteAsync(string subjectCode, CancellationToken ct = default)
        {
            var query = _dbSet.AsQueryable();
            query = query.Include(s => s.PrerequisiteSubject);

            return await query.FirstOrDefaultAsync(s => s.SubjectCode == subjectCode, ct);
        }

        public async Task<IEnumerable<Subject?>> GetRequiredBySubjectsAsync(string subjectCode, CancellationToken ct = default)
        {
            return await FindAsync(s => s.PrerequisiteSubjectCode == subjectCode, ct);
        }
        public async Task UpdateScalarsAsync(
            string subjectCode,
            string? subjectName,
            string? description,
            int? defaultCredits,
            string? prerequisiteSubjectCode,
            CancellationToken ct = default)
        {
            var existingSubject = await FirstOrDefaultAsync(s => s.SubjectCode == subjectCode, ct);
            if (existingSubject == null)
                throw new KeyNotFoundException($"Subject '{subjectCode}' not found.");

            if (subjectName != null)
                existingSubject.SubjectName = subjectName;

            if (description != null)
                existingSubject.Description = description;

            if (defaultCredits.HasValue)
                existingSubject.DefaultCredits = defaultCredits.Value;

            if (prerequisiteSubjectCode != null)
                existingSubject.PrerequisiteSubjectCode =
                    string.IsNullOrWhiteSpace(prerequisiteSubjectCode) ? null : prerequisiteSubjectCode.Trim();
        }
        
    }
}
