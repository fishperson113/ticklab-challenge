using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Core.Shared;
using TiklabChallenge.Infrastructure.Data;

namespace TiklabChallenge.UseCases.Services
{
    public sealed class StudentRegistrationService
    {
        private readonly UserManager<ApplicationUser> _um;
        private readonly RoleManager<IdentityRole> _rm;
        private readonly IUnitOfWork _uow;
        private readonly ApplicationContext _db;

        public StudentRegistrationService(
            UserManager<ApplicationUser> um,
            RoleManager<IdentityRole> rm,
            IUnitOfWork uow,
            ApplicationContext db)
        {
            _um = um;
            _rm = rm;
            _uow = uow;
            _db = db;
        }

        public async Task CompleteStudentRegistrationAsync(string email, string? fullName, string? studentCode, CancellationToken ct)
        {
            var user = await _um.FindByEmailAsync(email);
            if (user is null) return;

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(studentCode))
                return;

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var existing = await _uow.Students.GetByIdAsync(user.Id);
            if (existing is null)
            {
                await _uow.Students.AddAsync(
                new Student
                {
                    UserId = user.Id,
                    FullName = fullName,
                    StudentCode = studentCode,
                    User= user
                });

                await _uow.CommitAsync();
            }

            if (!await _rm.RoleExistsAsync(AppRoles.Student))
                await _rm.CreateAsync(new IdentityRole(AppRoles.Student));

            if (!await _um.IsInRoleAsync(user, AppRoles.Student))
                await _um.AddToRoleAsync(user, AppRoles.Student);

            await tx.CommitAsync(ct);
        }
    }
}
