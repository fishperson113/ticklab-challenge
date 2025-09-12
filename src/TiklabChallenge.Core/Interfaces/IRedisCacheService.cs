using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiklabChallenge.Core.Interfaces
{
    public interface IRedisCacheService
    {
        T? Get<T>(string key);
        Task<T?> GetAsync<T>(string key, int opTimeoutMs = 150, CancellationToken ct = default);
        void Set<T>(string key, T value);
        void Remove(string key);
    }
}
