using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Inventory_Management_System.Database
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        // PostgreSQL 'timestamp with time zone' only accepts DateTimes with Kind == Utc.
        // Client/JSON values arrive as Unspecified (or Local), so we coerce to UTC at the
        // PROVIDER boundary via a value converter. Doing this in SaveChanges by reassigning
        // CurrentValue does NOT work: DateTime equality ignores Kind, so EF's value comparer
        // treats the UTC value as unchanged and keeps the original Unspecified one.
        private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
            v => v.Kind == DateTimeKind.Utc ? v
               : v.Kind == DateTimeKind.Local ? v.ToUniversalTime()
               : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcConverter = new(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Utc ? v.Value
                   : v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime()
                   : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
                : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // Apply the UTC converter to every DateTime / DateTime? property in the model.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                        property.SetValueConverter(UtcConverter);
                    else if (property.ClrType == typeof(DateTime?))
                        property.SetValueConverter(NullableUtcConverter);
                }
            }
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            StampAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            StampAuditFields();
            return base.SaveChanges();
        }

        /// <summary>
        /// Keep BaseEntity audit fields populated. Kind coercion is handled by the value
        /// converters above, so here we only need to set sensible timestamps.
        /// </summary>
        private void StampAuditFields()
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.CreatedAt == default)
                        entry.Entity.CreatedAt = now;
                    entry.Entity.UpDatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpDatedAt = now;
                }
            }
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
