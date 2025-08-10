using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiklabChallenge.UseCases.Services
{
    public sealed class SeedService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public SeedService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        public async Task StartAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<AppSeeder>();
            await seeder.SeedAllAsync(ct); 
        }
        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }

}
