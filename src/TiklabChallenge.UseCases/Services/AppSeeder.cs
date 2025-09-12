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
            await SeedLargeDatasetAsync(ct);
            await SeedLargeUserSetAsync(ct);
            await Task.CompletedTask;
        }

        public async Task ResetAsync(CancellationToken ct)
        {
            await _db.Database.EnsureDeletedAsync(ct);
            await EnsureDatabaseReadyAsync(ct);
        }
        public async Task SeedLargeDatasetAsync(CancellationToken ct)
        {
            // First, create a large set of subjects with prerequisites
            await SeedLargeSubjectsAsync(ct);

            // Then, create courses based on these subjects
            await SeedLargeCoursesAsync(ct);

            // Finally, create schedules for these courses
            await SeedLargeSchedulesAsync(ct);

            await _db.SaveChangesAsync(ct);
        }
        public async Task SeedLargeUserSetAsync(CancellationToken ct, int count = 9000)
        {
            // 0) Config & dữ liệu mẫu
            var random = new Random(12345); // deterministic
            var firstNames = new[] { "James","Mary","John","Patricia","Robert","Jennifer","Michael","Linda","William","Elizabeth",
                             "David","Susan","Richard","Jessica","Joseph","Sarah","Thomas","Karen","Charles","Nancy" };
            var lastNames = new[] { "Smith","Johnson","Williams","Jones","Brown","Davis","Miller","Wilson","Moore","Taylor",
                             "Anderson","Thomas","Jackson","White","Harris","Martin","Thompson","Garcia","Martinez","Robinson" };
            const string domain = "example.com";
            const string password = "TestUser@12345";
            const int batchSize = 200; // 100–200 thường ổn

            // 1) Nạp sẵn email đang có để skip nhanh
            var existingEmails = new HashSet<string>(
                await _db.Users.AsNoTracking().Select(u => u.Email!).ToListAsync(ct),
                StringComparer.OrdinalIgnoreCase
            );

            // 2) Chuẩn bị buffer cho Students
            var studentsBuffer = new List<Student>(batchSize);

            // 3) Tắt AutoDetectChanges khi add nhiều bản ghi Student
            var prevDetect = _db.ChangeTracker.AutoDetectChangesEnabled;
            _db.ChangeTracker.AutoDetectChangesEnabled = false;

            try
            {
                for (int start = 1; start <= count; start += batchSize)
                {
                    studentsBuffer.Clear();
                    int end = Math.Min(start + batchSize - 1, count);

                    for (int i = start; i <= end; i++)
                    {
                        // Email duy nhất có thể đoán -> không cần FindByEmailAsync
                        string email = $"student{i:D6}@{domain}";
                        if (existingEmails.Contains(email))
                            continue; // đã tồn tại từ lần seed trước

                        // Tạo Identity user (đúng chuẩn hash/password)
                        var user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            EmailConfirmed = true,
                            SecurityStamp = Guid.NewGuid().ToString()
                        };

                        var rc = await _um.CreateAsync(user, password);
                        if (!rc.Succeeded)
                        {
                            // Duplicate hoặc lỗi khác -> log & bỏ qua
                            Console.WriteLine($"[WARN] create {email}: {string.Join(", ", rc.Errors.Select(e => e.Description))}");
                            continue;
                        }

                        // Thêm role (mặc định Student cho perf)
                        await _um.AddToRoleAsync(user, AppRoles.Student);

                        // Sinh thông tin Student (KHÔNG set navigation User để giảm tracking)
                        string fullName = $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}";
                        studentsBuffer.Add(new Student
                        {
                            UserId = user.Id,
                            StudentCode = $"S{i:D6}",
                            FullName = fullName,
                            EnrolledAt = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                            User = user
                        });

                        existingEmails.Add(email);
                    }

                    if (studentsBuffer.Count > 0)
                        await _db.Students.AddRangeAsync(studentsBuffer, ct);

                    await _db.SaveChangesAsync(ct);
                    _db.ChangeTracker.Clear(); // tránh "already being tracked"

                    Console.WriteLine($"[Seed] Created up to {end}/{count}");
                }
            }
            finally
            {
                _db.ChangeTracker.AutoDetectChangesEnabled = prevDetect;
            }
        }


        public async Task ExportAllUsersToCsvAsync(string filePath, CancellationToken ct)
        {
            // Query all users with their roles
            var users = await _db.Users.ToListAsync(ct);
            var userRoles = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _um.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "None";
            }

            // Get student information for those who are students
            var students = await _db.Students.ToListAsync(ct);
            var studentInfo = students.ToDictionary(s => s.UserId, s => s);

            // Standard password for all users
            var standardPassword = "TestUser@12345";

            // Create directory if it doesn't exist
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write to CSV file
            using (var writer = new StreamWriter(filePath))
            {
                // Write header with just the fields we need
                await writer.WriteLineAsync("StudentCode,Email,Password");

                // Write data for each user
                foreach (var user in users)
                {
                    // Only export students (users with a student profile)
                    if (studentInfo.TryGetValue(user.Id, out var student))
                    {
                        // Write just the student code, email, and standard password
                        await writer.WriteLineAsync($"{student.StudentCode},{EscapeCsvField(user.Email)},{standardPassword}");
                    }
                }
            }

            Console.WriteLine($"Exported student credentials to {Path.GetFullPath(filePath)}");
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // If the field contains a comma, newline, or double quote, enclose it in quotes
            if (field.Contains(',') || field.Contains('\n') || field.Contains('"'))
            {
                // Replace any double quotes with two double quotes
                field = field.Replace("\"", "\"\"");
                return $"\"{field}\"";
            }

            return field;
        }
        private async Task SeedLargeSubjectsAsync(CancellationToken ct)
        {
            // Create subject categories
            var categories = new[] { "CS", "MATH", "PHY", "ENG", "BIO", "CHEM", "ECON", "ART", "HIST", "PSYCH" };
            var subjectDescriptions = new[] {
                "Introduction to", "Advanced", "Principles of", "Topics in", "Fundamentals of",
                "Applied", "Modern", "Research in", "Theoretical", "Practical"
            };
            var fieldNames = new[] {
                "Programming", "Algorithms", "Data Structures", "Systems", "Networks",
                "Security", "Databases", "Web Development", "AI", "Machine Learning",
                "Statistics", "Calculus", "Algebra", "Physics", "Chemistry",
                "Biology", "Economics", "History", "Psychology", "Art"
            };

            // Track all created subject codes for prerequisite assignment
            var createdSubjectCodes = new List<string>();
            var random = new Random();

            // Create 60 subjects (sufficient for 350 courses with varying prerequisites)
            for (int i = 1; i <= 60; i++)
            {
                var categoryIndex = (i - 1) / 6; // Distribute subjects across categories
                var level = (i % 6) + 1; // Levels 1-6 within each category

                var subjectCode = $"{categories[categoryIndex % categories.Length]}{level.ToString("D3")}";

                if (!_db.Subjects.Any(s => s.SubjectCode == subjectCode))
                {
                    var descriptionIndex = random.Next(subjectDescriptions.Length);
                    var fieldIndex = random.Next(fieldNames.Length);

                    var subject = new Subject
                    {
                        SubjectCode = subjectCode,
                        SubjectName = $"{subjectDescriptions[descriptionIndex]} {fieldNames[fieldIndex]}",
                        Description = $"{subjectDescriptions[descriptionIndex]} {fieldNames[fieldIndex]} (Level {level})",
                        DefaultCredits = random.Next(2, 6),
                        PrerequisiteSubjectCode = level > 1 && createdSubjectCodes.Count > 0
                            ? createdSubjectCodes[random.Next(createdSubjectCodes.Count)]
                            : null
                    };

                    await _db.Subjects.AddAsync(subject, ct);
                    createdSubjectCodes.Add(subjectCode);
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        private async Task SeedLargeCoursesAsync(CancellationToken ct)
        {
            // 1) Load sẵn subject codes (no tracking)
            var subjectCodes = await _db.Subjects
                .AsNoTracking()
                .Select(s => s.SubjectCode)
                .ToListAsync(ct);
            if (subjectCodes.Count == 0) return;

            // 2) Lấy các CourseCode đã tồn tại (no tracking) vào HashSet
            var existing = new HashSet<string>(
                await _db.Courses
                    .AsNoTracking()
                    .Select(c => c.CourseCode)
                    .ToListAsync(ct),
                StringComparer.OrdinalIgnoreCase
            );

            // 3) Sinh mới cho đủ 350 khóa học, tránh trùng cả "đã có" lẫn "đang thêm"
            const int target = 350;
            var planned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rnd = new Random(12345); // deterministic

            int created = 0;
            while (created < target)
            {
                // Chọn ngẫu nhiên 1 subject, 1 semester (1..3), 1 section (01..20)
                var subject = subjectCodes[rnd.Next(subjectCodes.Count)];
                int semester = rnd.Next(1, 4);  // 1..3
                int section = rnd.Next(1, 21); // 1..20

                var code = $"{subject}.S{semester}.{section:D2}";

                if (existing.Contains(code) || planned.Contains(code))
                    continue; // trùng -> chọn lại

                _db.Courses.Add(new Course
                {
                    CourseCode = code,
                    SubjectCode = subject,
                    MaxEnrollment = 30,
                    CreatedAt = DateTime.UtcNow
                });

                planned.Add(code);
                created++;

                // Save theo batch để nhẹ context
                if (created % 200 == 0)
                {
                    await _db.SaveChangesAsync(ct);
                    _db.ChangeTracker.Clear();
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        private async Task SeedLargeSchedulesAsync(CancellationToken ct)
        {
            // 1) Lấy danh sách course chưa có lịch
            var coursesNeedingSchedule = await _db.Courses
                .AsNoTracking()
                .Where(c => !_db.Schedules.Any(s => s.CourseCode == c.CourseCode))
                .Select(c => c.CourseCode)
                .ToListAsync(ct);
            if (coursesNeedingSchedule.Count == 0) return;

            // 2) Build danh sách phòng
            var buildings = new[] { "A", "B", "C", "D", "E" };
            var floors = new[] { 1, 2, 3, 4 };
            var roomNumbers = new[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10" };
            var roomIds = (from b in buildings
                           from f in floors
                           from r in roomNumbers
                           select $"{b}{f}{r}").ToList();

            // 3) Lấy các key schedule đã có trong DB để loại trừ
            //    Key = $"{RoomId}|{(int)DayOfWeek}|{Start}|{End}"
            var existingKeys = new HashSet<string>(
                await _db.Schedules.AsNoTracking()
                    .Select(s => s.RoomId + "|" + ((int)s.DayOfWeek) + "|" +
                                 s.StartTime.ToString() + "|" + s.EndTime.ToString())
                    .ToListAsync(ct),
                StringComparer.OrdinalIgnoreCase
            );

            // 4) Tạo pool slot (đủ to), loại trùng với DB
            //    DayOfWeekCode: dùng 0..4 (Mon-Fri) như bạn đang làm
            var durations = new[] { 1, 2, 3 };            // giờ
            var startHours = Enumerable.Range(8, 12);     // 8..19
            var pool = new List<(string room, DayOfWeekCode day, TimeOnly start, TimeOnly end, string key)>();
            foreach (var room in roomIds)
            {
                for (int d = 0; d < 5; d++)
                {
                    foreach (var h in startHours)
                    {
                        foreach (var dur in durations)
                        {
                            var start = new TimeOnly(h, 0);
                            var end = new TimeOnly(h + dur, 0);
                            var key = $"{room}|{d}|{start}|{end}";
                            if (!existingKeys.Contains(key))
                                pool.Add((room, (DayOfWeekCode)d, start, end, key));
                        }
                    }
                }
            }

            if (pool.Count < coursesNeedingSchedule.Count)
                throw new InvalidOperationException($"Không đủ slot lịch ({pool.Count}) cho {coursesNeedingSchedule.Count} course.");

            // 5) Xáo trộn pool để phân bổ ngẫu nhiên, nhưng đảm bảo duy nhất
            var rnd = new Random(12345);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            // 6) Gán mỗi course một slot duy nhất
            int idx = 0;
            foreach (var courseCode in coursesNeedingSchedule)
            {
                var slot = pool[idx++];
                _db.Schedules.Add(new Schedule
                {
                    Id = Guid.NewGuid().ToString(),
                    RoomId = slot.room,
                    CourseCode = courseCode,
                    DayOfWeek = slot.day,
                    StartTime = slot.start,
                    EndTime = slot.end
                });
            }

            await _db.SaveChangesAsync(ct);
        }
        public async Task ResetEnrollmentsAsync(CancellationToken ct)
        {
            // Remove all waitlist entries first (due to foreign key constraints)
            var waitlists = await _db.Waitlists.ToListAsync(ct);
            _db.Waitlists.RemoveRange(waitlists);

            // Remove all enrollment records
            var enrollments = await _db.Enrollments.ToListAsync(ct);
            _db.Enrollments.RemoveRange(enrollments);

            await _db.SaveChangesAsync(ct);
        }
    }
}