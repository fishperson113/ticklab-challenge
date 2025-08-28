using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;

namespace TiklabChallenge.Core.Interfaces
{
    public interface ISubjectRepository : IRepository<Subject>
    {
        Task<Subject?> GetBySubjectCodeAsync(string subjectCode, CancellationToken ct = default);
        Task<Subject?> GetWithPrerequisiteAsync(string subjectCode, CancellationToken ct = default);
        Task<IEnumerable<Subject?>> GetRequiredBySubjectsAsync(string subjectCode, CancellationToken ct = default);
        Task UpdateScalarsAsync(string subjectCode,string? subjectName,string? descriptionn,int? defaultCredits,string? prerequisiteSubjectCode,CancellationToken ct = default);
    }
}
