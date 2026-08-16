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

        // Both SaveChanges overloads are overridden at their widest signature: the parameterless
        // ones delegate here, so overriding only those would let a direct
        // SaveChangesAsync(acceptAllChangesOnSuccess, ct) call slip past the hooks below.
        public override async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            await SyncSearchTextAsync(cancellationToken);
            StampAuditFields();
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            SyncSearchText();
            StampAuditFields();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        #region Full-text search projection

        // Hybrid FTS: the application owns ProductVariant.SearchText (plain text), the database
        // trigger derives SearchVector from it. That keeps the tsvector impossible to drift,
        // while the text stays debuggable and cheap to rebuild.

        private async Task SyncSearchTextAsync(CancellationToken cancellationToken)
        {
            var renamedProductIds = RenamedProductIds();

            // A rename invalidates every variant of that product, including ones nobody touched.
            if (renamedProductIds.Count > 0)
            {
                var ids = renamedProductIds.ToArray();
                await ProductVariants.Where(v => ids.Contains(v.ProductId)).LoadAsync(cancellationToken);
            }

            var variants = CollectVariantsToReindex(renamedProductIds);
            if (variants.Count == 0) return;

            var names = TrackedProductNames();
            var missing = MissingProductIds(variants, names);

            if (missing.Count > 0)
            {
                var fetched = await Products.AsNoTracking()
                    .Where(p => missing.Contains(p.Id))
                    .Select(p => new { p.Id, p.ProductName })
                    .ToListAsync(cancellationToken);

                foreach (var product in fetched)
                    names[product.Id] = product.ProductName;
            }

            ApplySearchText(variants, names);
        }

        private void SyncSearchText()
        {
            var renamedProductIds = RenamedProductIds();

            if (renamedProductIds.Count > 0)
            {
                var ids = renamedProductIds.ToArray();
                ProductVariants.Where(v => ids.Contains(v.ProductId)).Load();
            }

            var variants = CollectVariantsToReindex(renamedProductIds);
            if (variants.Count == 0) return;

            var names = TrackedProductNames();
            var missing = MissingProductIds(variants, names);

            if (missing.Count > 0)
            {
                var fetched = Products.AsNoTracking()
                    .Where(p => missing.Contains(p.Id))
                    .Select(p => new { p.Id, p.ProductName })
                    .ToList();

                foreach (var product in fetched)
                    names[product.Id] = product.ProductName;
            }

            ApplySearchText(variants, names);
        }

        /// <summary>
        /// Products whose name actually changed. Comparing original vs current matters because
        /// DbSet.Update() flags every property as modified regardless of its value.
        /// </summary>
        private HashSet<int> RenamedProductIds()
        {
            var ids = new HashSet<int>();

            foreach (var entry in ChangeTracker.Entries<Product>())
            {
                if (entry.State != EntityState.Modified || entry.Entity.Id == 0) continue;

                var property = entry.Property(p => p.ProductName);
                if (property.IsModified &&
                    !string.Equals(property.OriginalValue, property.CurrentValue, StringComparison.Ordinal))
                {
                    ids.Add(entry.Entity.Id);
                }
            }

            return ids;
        }

        private List<ProductVariant> CollectVariantsToReindex(HashSet<int> renamedProductIds) =>
            [.. ChangeTracker.Entries<ProductVariant>()
                .Where(e => e.State is EntityState.Added or EntityState.Modified
                         || (e.State == EntityState.Unchanged && renamedProductIds.Contains(e.Entity.ProductId)))
                .Select(e => e.Entity)];

        private Dictionary<int, string> TrackedProductNames() =>
            ChangeTracker.Entries<Product>()
                .Where(e => e.State != EntityState.Deleted && e.Entity.Id != 0)
                .ToDictionary(e => e.Entity.Id, e => e.Entity.ProductName);

        // ProductId == 0 means the parent Product is being inserted in this same unit of work,
        // so the navigation is set and no lookup is needed.
        private static List<int> MissingProductIds(
            List<ProductVariant> variants,
            Dictionary<int, string> names) =>
            [.. variants
                .Where(v => v.ProductId != 0 && v.Product is null && !names.ContainsKey(v.ProductId))
                .Select(v => v.ProductId)
                .Distinct()];

        private static void ApplySearchText(List<ProductVariant> variants, Dictionary<int, string> names)
        {
            foreach (var variant in variants)
            {
                names.TryGetValue(variant.ProductId, out var productName);
                variant.RebuildSearchText(productName);
            }
        }

        #endregion

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
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductSerial> ProductSerials { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<SupplierPurchase> SupplierPurchases { get; set; }
        public DbSet<SupplierPurchaseDetails> SupplierPurchaseDetails { get; set; }
        public DbSet<SupplierPayment> SupplierPayments { get; set; }
        public DbSet<SupplierPurchasePayment> SupplierPurchasePayments { get; set; }
        public DbSet<SupplierTransaction> SupplierTransactions { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerSale> CustomerSales { get; set; }
        public DbSet<SaleDetails> SaleDetails { get; set; }
        public DbSet<CustomerPayment> CustomerPayments { get; set; }
        public DbSet<SaleCustomerPayment> SaleCustomerPayments { get; set; }
        public DbSet<CustomerTransaction> CustomerTransactions { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<WarrantyClaim> WarrantyClaims { get; set; }
        public DbSet<StockTransfer> StockTransfers { get; set; }
        public DbSet<StockTransferDetails> StockTransferDetails { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Designation> Designations { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        #endregion

    }
}
