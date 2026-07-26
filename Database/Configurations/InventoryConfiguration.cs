using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    // Denormalized on-hand snapshot: exactly one row per (Branch, ProductVariant).
    public class StockConfiguration : IEntityTypeConfiguration<Stock>
    {
        public void Configure(EntityTypeBuilder<Stock> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.CurrentStock).IsRequired();
            builder.HasIndex(s => new { s.BranchId, s.ProductVariantId }).IsUnique();

            builder.HasOne(s => s.Branch)
                   .WithMany(b => b.Stocks)
                   .HasForeignKey(s => s.BranchId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.ProductVariant)
                   .WithMany(v => v.Stocks)
                   .HasForeignKey(s => s.ProductVariantId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    // Append-only stock ledger.
    public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
    {
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.TransactionType).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(t => t.QuantityIn).IsRequired();
            builder.Property(t => t.QuantityOut).IsRequired();
            builder.Property(t => t.BalanceAfter).IsRequired();

            builder.HasOne(t => t.Branch)
                   .WithMany(b => b.InventoryTransactions)
                   .HasForeignKey(t => t.BranchId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.ProductVariant)
                   .WithMany(v => v.InventoryTransactions)
                   .HasForeignKey(t => t.ProductVariantId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.SupplierPurchaseDetails)
                   .WithMany()
                   .HasForeignKey(t => t.SupplierPurchaseDetailsId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    // One row per physical serialized unit; SerialNumber is globally unique.
    public class ProductSerialConfiguration : IEntityTypeConfiguration<ProductSerial>
    {
        public void Configure(EntityTypeBuilder<ProductSerial> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.SerialNumber).IsRequired().HasMaxLength(100);
            builder.HasIndex(s => s.SerialNumber).IsUnique();
            builder.Property(s => s.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(s => s.WarrantyMonths).IsRequired();

            builder.HasOne(s => s.ProductVariant)
                   .WithMany(v => v.ProductSerials)
                   .HasForeignKey(s => s.ProductVariantId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.SupplierPurchaseDetails)
                   .WithMany(d => d.ProductSerials)
                   .HasForeignKey(s => s.SupplierPurchaseDetailsId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Branch)
                   .WithMany()
                   .HasForeignKey(s => s.BranchId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
