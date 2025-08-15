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
        IRepository<Course> Courses { get; }
        IRepository<Schedule> Schedules { get; }
        Task<int> CommitAsync();
    }
}
