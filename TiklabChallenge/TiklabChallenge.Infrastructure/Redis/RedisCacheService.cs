using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TiklabChallenge.Core.Interfaces;

namespace TiklabChallenge.Infrastructure.Redis
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache? _cache;

        public RedisCacheService(IDistributedCache? cache)
        {
            _cache = cache;
        }
        public T? Get<T>(string key)
        {
            var data= _cache?.GetString(key);

            if (data is null) return default(T);

            return JsonSerializer.Deserialize<T>(data);
        }

        public void Set<T>(string key, T value)
        {
            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

            _cache?.SetString(key, JsonSerializer.Serialize(value), options);
        }
    }
}
