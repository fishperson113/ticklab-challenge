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
    public class StudentRepository : GenericRepository<Student>, IStudentRepository
    {
        public StudentRepository(ApplicationContext context):base(context)
        {
        }
        public async Task UpdateProfileAsync(Student student, CancellationToken ct = default)
        {
            var existingStudent = await GetByIdAsync(student.UserId);

            existingStudent.FullName = student.FullName;

            _context.Students.Update(existingStudent);
        }
    }
}
