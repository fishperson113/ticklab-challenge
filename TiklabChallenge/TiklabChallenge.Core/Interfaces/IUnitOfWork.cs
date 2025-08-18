using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;

namespace TiklabChallenge.Core.Interfaces
{
    public interface IUnitOfWork: IAsyncDisposable
    {
        IRepository<WeatherForecast> WeatherForecasts { get; }
        IStudentRepository Students { get; }
        ICourseRepository Courses { get; }
        IScheduleRepository Schedules { get; }
        IEnrollmentRepository Enrollments { get; }
        ISubjectRepository Subjects { get; }
        Task<int> CommitAsync();
    }
}
