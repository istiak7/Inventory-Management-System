using Microsoft.EntityFrameworkCore;
using Inventory_Management_System.Entities;

namespace Inventory_Management_System.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        #region DbSets

        public DbSet<Activity> Activities { get; set; }
     //   public DbSet<Department> Departments { get; set; }
        public DbSet<User> Users { get; set; }

        #endregion

    }
}
