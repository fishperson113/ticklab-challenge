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
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {
        }
        // Define DbSets for your entities here, e.g.:
        public DbSet<WeatherForecast> WeatherForecasts { get; set; }
        public DbSet<Student> Students { get; set; }
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
            });
            #endregion
        }
    }
}
