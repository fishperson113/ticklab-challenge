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
        public DbSet<Subject> Subjects { get; set; }

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
            #region Subject Entity
            modelBuilder.Entity<Subject>(e =>
            {
                e.HasKey(x => x.SubjectCode);
                e.Property(x => x.SubjectCode).HasMaxLength(16);
                e.Property(x => x.SubjectName).HasMaxLength(255);

                e.Property(x => x.PrerequisiteSubjectCode).HasMaxLength(16);
                e.HasOne(x => x.PrerequisiteSubject)
                 .WithMany(s => s.RequiredBySubjects)
                 .HasForeignKey(x => x.PrerequisiteSubjectCode)
                 .OnDelete(DeleteBehavior.Restrict);         
            });
            #endregion
            #region Course Entity
            modelBuilder.Entity<Course>(e =>
            {
                e.HasKey(x => x.CourseCode);
                e.Property(x => x.CourseCode).HasMaxLength(32);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("now()"); 

                e.Property(x => x.SubjectCode).IsRequired().HasMaxLength(16);
                e.HasOne(x => x.Subject)
                 .WithMany(s => s.Courses)
                 .HasForeignKey(x => x.SubjectCode)
                 .OnDelete(DeleteBehavior.Restrict);         
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
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasMaxLength(64);

                e.Property(x => x.CourseCode).IsRequired().HasMaxLength(32);
                e.HasIndex(x => x.CourseCode).IsUnique();    

                e.HasOne(x => x.Course)
                 .WithOne(c => c.Schedule)
                 .HasForeignKey<Schedule>(x => x.CourseCode)
                 .OnDelete(DeleteBehavior.Cascade);

                e.Property(x => x.DayOfWeek).HasConversion<string>().HasMaxLength(16);
                e.Property(x => x.StartTime).HasColumnType("time");
                e.Property(x => x.EndTime).HasColumnType("time");

                e.HasIndex(x => new { x.RoomId, x.DayOfWeek, x.StartTime, x.EndTime }).IsUnique();
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
