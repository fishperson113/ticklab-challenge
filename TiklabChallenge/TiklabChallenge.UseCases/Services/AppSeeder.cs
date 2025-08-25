using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
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
            var email = "admin";
            var pass = "Admin@12345";

            var u = await _um.FindByEmailAsync(email);
            if (u is null)
            {
                u = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                var rc = await _um.CreateAsync(u, pass);
                if (!rc.Succeeded) throw new Exception(string.Join(", ", rc.Errors.Select(e => e.Description)));
            }
            if (!await _um.IsInRoleAsync(u, AppRoles.Admin))
                await _um.AddToRoleAsync(u, AppRoles.Admin);
        }

        public async Task SeedStudentsAsync(CancellationToken ct)
        {
            // Create 5 mock student users
            for (int i = 1; i <= 5; i++)
            {
                var email = $"student{i}@example.com";
                var pass = "Student@12345";
                var studentCode = $"S{i.ToString("D4")}";
                var fullName = $"Student {i}";

                // Create user if not exists
                var user = await _um.FindByEmailAsync(email);
                if (user is null)
                {
                    user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                    var result = await _um.CreateAsync(user, pass);
                    if (!result.Succeeded) throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

                    // Add to student role
                    await _um.AddToRoleAsync(user, AppRoles.Student);
                }

                // Check if student profile exists
                if (!_db.Students.Any(s => s.UserId == user.Id))
                {
                    var student = new Student
                    {
                        UserId = user.Id,
                        User = user,
                        StudentCode = studentCode,
                        FullName = fullName,
                        EnrolledAt = DateTime.UtcNow.AddDays(-i * 30) // Different enrollment dates
                    };

                    await _db.Students.AddAsync(student, ct);
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task SeedSubjectsAsync(CancellationToken ct)
        {
            // Define subjects with prerequisites
            var subjects = new List<Subject>
            {
                new Subject
                {
                    SubjectCode = "CS101",
                    SubjectName = "Introduction to Programming",
                    Description = "Basic programming concepts using C#",
                    DefaultCredits = 3,
                    PrerequisiteSubjectCode = null
                },
                new Subject
                {
                    SubjectCode = "CS102",
                    SubjectName = "Data Structures",
                    Description = "Basic data structures and algorithms",
                    DefaultCredits = 4,
                    PrerequisiteSubjectCode = "CS101"
                },
                new Subject
                {
                    SubjectCode = "CS201",
                    SubjectName = "Database Systems",
                    Description = "Introduction to database concepts",
                    DefaultCredits = 3,
                    PrerequisiteSubjectCode = "CS102"
                },
                new Subject
                {
                    SubjectCode = "CS202",
                    SubjectName = "Web Development",
                    Description = "Building web applications with ASP.NET",
                    DefaultCredits = 4,
                    PrerequisiteSubjectCode = "CS201"
                },
                new Subject
                {
                    SubjectCode = "CS301",
                    SubjectName = "Software Engineering",
                    Description = "Software development lifecycle and practices",
                    DefaultCredits = 4,
                    PrerequisiteSubjectCode = "CS202"
                }
            };

            foreach (var subject in subjects)
            {
                if (!_db.Subjects.Any(s => s.SubjectCode == subject.SubjectCode))
                {
                    await _db.Subjects.AddAsync(subject, ct);
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task SeedCoursesAsync(CancellationToken ct)
        {
            // Get subject codes from database
            var subjectCodes = await _db.Subjects.Select(s => s.SubjectCode).ToListAsync(ct);

            // Create courses for each subject
            foreach (var subjectCode in subjectCodes)
            {
                // Create two course sections for each subject
                for (int section = 1; section <= 2; section++)
                {
                    var courseCode = $"{subjectCode}.{section}";

                    if (!_db.Courses.Any(c => c.CourseCode == courseCode))
                    {
                        var course = new Course
                        {
                            CourseCode = courseCode,
                            SubjectCode = subjectCode,
                            MaxEnrollment = 25,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _db.Courses.AddAsync(course, ct);
                    }
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task SeedSchedulesAsync(CancellationToken ct)
        {
            // Get courses from database
            var courses = await _db.Courses.ToListAsync(ct);

            // Define room IDs
            var roomIds = new[] { "R101", "R102", "R103", "R104" };

            // Create a schedule for each course with different times and rooms
            int roomIndex = 0;
            int dayIndex = 0;
            int timeSlot = 0;

            foreach (var course in courses)
            {
                if (!_db.Schedules.Any(s => s.CourseCode == course.CourseCode))
                {
                    // Rotate through days (Monday - Friday)
                    var dayOfWeek = (DayOfWeekCode)(dayIndex % 5);

                    // Create 2-hour time slots starting from 8 AM
                    var startHour = 8 + (timeSlot % 5) * 2;

                    var schedule = new Schedule
                    {
                        Id = Guid.NewGuid().ToString(),
                        RoomId = roomIds[roomIndex % roomIds.Length],
                        CourseCode = course.CourseCode,
                        DayOfWeek = dayOfWeek,
                        StartTime = new TimeOnly(startHour, 0),
                        EndTime = new TimeOnly(startHour + 2, 0)
                    };

                    await _db.Schedules.AddAsync(schedule, ct);

                    // Increment counters for next schedule
                    roomIndex++;
                    timeSlot++;
                    if (timeSlot % 5 == 0) dayIndex++;
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task SeedAllAsync(CancellationToken ct)
        {
            await SeedRolesAsync(ct);
            await SeedAdminAsync(ct);
            await SeedStudentsAsync(ct);
            await SeedSubjectsAsync(ct);
            await SeedCoursesAsync(ct);
            await SeedSchedulesAsync(ct);
        }

        public async Task ResetAsync(CancellationToken ct)
        {
            await _db.Database.EnsureDeletedAsync(ct);
            await EnsureDatabaseReadyAsync(ct);
            await SeedAllAsync(ct);
        }
    }
}