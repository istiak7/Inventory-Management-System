using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Database
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        #region DbSets

        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<ProductCategories> ProductCategories { get; set; }
        public DbSet<ProductSubCategories> ProductSubCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<SupplierPurchase> SupplierPurchases { get; set; }
        public DbSet<SupplierPurchaseDetails> SupplierPurchaseDetails { get; set; }
        public DbSet<SupplierPayment> SupplierPayments { get; set; }
        public DbSet<SupplierPurchasePayment> SupplierPurchasePayments { get; set; }
        public DbSet<SupplierTransaction> SupplierTransactions { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Designation> Designations { get; set; }

        #endregion

    }
}
