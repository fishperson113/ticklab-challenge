using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Infrastructure.Repository;
using TiklabChallenge.Infrastructure.Data;
namespace TiklabChallenge.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationContext _context;
        public IRepository<WeatherForecast> WeatherForecasts { get; private set; }
        public IStudentRepository Students { get; private set; }
        public ICourseRepository Courses { get; private set; }
        public IScheduleRepository Schedules { get; private set; }
        public ISubjectRepository Subjects { get; private set; }
        public IEnrollmentRepository Enrollments { get; private set; }

        public UnitOfWork(ApplicationContext context)
        {
            _context = context;
            WeatherForecasts = new GenericRepository<WeatherForecast>(_context);
            Students = new StudentRepository(_context);
            Schedules = new ScheduleRepository(_context);
            Enrollments = new EnrollmentRepository(_context);
            Courses = new CourseRepository(_context);
            Subjects = new SubjectRepository(_context);
        }

        public async Task<int> CommitAsync() =>
            await _context.SaveChangesAsync();

        public async ValueTask DisposeAsync() =>
            await _context.DisposeAsync();
    }
}
