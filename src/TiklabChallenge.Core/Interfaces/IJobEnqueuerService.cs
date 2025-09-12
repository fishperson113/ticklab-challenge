using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;

namespace TiklabChallenge.Core.Interfaces
{
    public interface IJobEnqueuerService
    {
        Task<string> EnqueueJobAsync(Job job);
    }
}
