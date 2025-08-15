using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace TiklabChallenge.Infrastructure.Data
{
    public class ApplicationContext : IdentityDbContext<ApplicationUser>
    {
        #region Ctors
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {
        }
        #endregion
        // Define DbSets for your entities here, e.g.:
        #region DbSets
        public DbSet<WeatherForecast> WeatherForecasts { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<CourseSchedule> CourseSchedules { get; set; }
        public DbSet<Prerequisite> Prerequisites { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        #endregion

        #region OnModelCreating
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configure entity properties and relationships here
            #region Student Entity 
            modelBuilder.Entity<Student>(s =>
            {
                s.HasKey(u => u.UserId);
                s.HasOne(u => u.User)
                 .WithOne()
                 .HasForeignKey<Student>(u => u.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
                s.HasIndex(u => u.StudentCode)
                 .IsUnique();
                s.Property(u => u.FullName)
                 .HasMaxLength(255);
            });
            #endregion

            #region Course Entity
            modelBuilder.Entity<Course>(e =>
            {
                e.HasKey(x => x.CourseCode);
                e.Property(x => x.CourseCode).HasMaxLength(32);
                e.Property(x => x.CourseName).HasMaxLength(255);
                e.Property(x => x.Description);
                e.Property(x => x.Credits);
                e.Property(x => x.MaxEnrollment);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            });
            #endregion

            #region Enrollment Entity

            modelBuilder.Entity<Enrollment>(e =>
            {
                e.HasKey(x => new { x.StudentId, x.CourseCode });

                e.Property(x => x.Status)
                 .HasConversion<string>()
                 .HasMaxLength(16);

                e.Property(x => x.EnrolledAt)
                 .HasDefaultValueSql("now()");

                e.HasOne(x => x.Student)
                 .WithMany(s => s.Enrollments)
                 .HasForeignKey(x => x.StudentId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Course)
                 .WithMany(c => c.Enrollments)
                 .HasForeignKey(x => x.CourseCode)
                 .OnDelete(DeleteBehavior.Cascade);
            });
            #endregion

            #region Schedule Entity
            modelBuilder.Entity<Schedule>(e =>
            {
                e.HasKey(x => x.ScheduleId);
                e.Property(x => x.ScheduleId).HasMaxLength(32);

                e.Property(x => x.DayOfWeek)
                 .HasConversion<string>()
                 .HasMaxLength(16);

                e.Property(x => x.StartTime).HasColumnType("time");
                e.Property(x => x.EndTime).HasColumnType("time");
            });
            #endregion

            #region CourseSchedule Entity
            modelBuilder.Entity<CourseSchedule>(e =>
            {
                e.HasKey(x => new { x.CourseCode, x.ScheduleId });

                e.HasOne(x => x.Course)
                 .WithMany(c => c.CourseSchedules)
                 .HasForeignKey(x => x.CourseCode)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Schedule)
                 .WithMany(s => s.CourseSchedules)
                 .HasForeignKey(x => x.ScheduleId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
            #endregion

            #region Prerequisite Entity

            modelBuilder.Entity<Prerequisite>(e =>
            {
                e.HasKey(x => new { x.CourseCode, x.PrerequisiteCode });

                e.HasOne(x => x.Course)
                 .WithMany(c => c.Prerequisites)
                 .HasForeignKey(x => x.CourseCode)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.RequiredCourse)
                 .WithMany(c => c.RequiredBy)
                 .HasForeignKey(x => x.PrerequisiteCode)
                 .OnDelete(DeleteBehavior.Restrict); 
            });
            #endregion

            #region WeatherForecast Entity (optional)
            modelBuilder.Entity<WeatherForecast>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Summary).HasMaxLength(128);
                e.Property(x => x.Date).HasColumnType("date");
            });
            #endregion
        }
        #endregion
    }
}
