using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Infrastructure.Data;
using TiklabChallenge.Core.Shared;
namespace TiklabChallenge.UseCases.Services
{
    public class AppSeeder
    {
        private readonly ApplicationContext _db;
        private readonly UserManager<ApplicationUser> _um;
        private readonly RoleManager<IdentityRole> _rm;
        public AppSeeder(ApplicationContext db, UserManager<ApplicationUser> um,
                     RoleManager<IdentityRole> rm)
        {
            _db = db;
            _um = um;
            _rm = rm;
        }
        public async Task EnsureDatabaseReadyAsync(CancellationToken ct)
        {
            if (_db.Database.IsRelational())
                await _db.Database.MigrateAsync(ct);
            else
                await _db.Database.EnsureCreatedAsync(ct);
        }
        public async Task SeedRolesAsync(CancellationToken ct)
        {
            foreach (var r in AppRoles.All)
                if (!await _rm.RoleExistsAsync(r))
                    await _rm.CreateAsync(new IdentityRole(r));
        }
        public async Task SeedAdminAsync(CancellationToken ct)
        {
            var email =  "admin";
            var pass = "Admin@12345";

            var u = await _um.FindByEmailAsync(email);
            if (u is null)
            {
                u = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                var rc = await _um.CreateAsync(u, pass);
                if (!rc.Succeeded) throw new Exception(string.Join(", ", rc.Errors.Select(e => e.Description)));
            }
            if (!await _um.IsInRoleAsync(u, "Admin"))
                await _um.AddToRoleAsync(u, "Admin");
        }
        public async Task SeedAllAsync(CancellationToken ct)
        {
            await SeedRolesAsync(ct);
            await SeedAdminAsync(ct);
        }

        public async Task ResetAsync(CancellationToken ct)
        {
            await _db.Database.EnsureDeletedAsync(ct);
            await EnsureDatabaseReadyAsync(ct);
            await SeedAllAsync(ct);
        }
    }
}
