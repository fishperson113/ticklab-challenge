using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
namespace TiklabChallenge.Core.Interfaces
{
    public interface IStudentRepository : IRepository<Student>
    {
        Task UpdateProfileAsync(Student student, CancellationToken ct = default);
    }
}
