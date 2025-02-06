using Microsoft.EntityFrameworkCore;
using PlusApi.Models.User;
using PlusApi.ViewModels.Helper;

namespace PlusApi.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Users> Users { get; set; }

        // Constructor to pass options to the base DbContext class
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { }

        // Configure the DbContext
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.ConfigureWarnings(warnings => 
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        // Override OnModelCreating to add seed data
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Seed(); // Make sure this is correctly added to your ModelBuilderExtensions
        }
    }
}
