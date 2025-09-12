using System.Threading;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;

namespace TiklabChallenge.Core.Interfaces
{
    public interface IJobProcessor
    {
        Task ProcessAsync(Job job, CancellationToken cancellationToken);
    }
}