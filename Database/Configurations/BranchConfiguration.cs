using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class BranchConfiguration : IEntityTypeConfiguration<Branch>
    {
        public void Configure(EntityTypeBuilder<Branch> builder)
        {
            builder.HasKey(b => b.Id); // Id is the primary key
            builder.Property(b => b.Name).IsRequired().HasMaxLength(100);
            builder.Property(b => b.Location).HasMaxLength(200);
            builder.Property(b => b.PhoneNumber).HasMaxLength(20);
            builder.Property(b => b.Email).HasMaxLength(100);
        }
    }

    public class StockConfiguration : IEntityTypeConfiguration<Stock>
    {
        public void Configure(EntityTypeBuilder<Stock> builder)
        {
            builder.HasKey(s => s.Id); // Id is the primary key
            builder.Property(s => s.CurrentStock).IsRequired();

            // A branch has exactly one stock row per product
            builder.HasIndex(s => new { s.BranchId, s.SKU }).IsUnique();

            builder.HasOne(s => s.Branch)
                   .WithMany(b => b.Stocks)
                   .HasForeignKey(s => s.BranchId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Product)
                   .WithMany()
                   .HasForeignKey(s => s.SKU)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
    {
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.HasKey(t => t.Id); // Id is the primary key
            builder.Property(t => t.TransactionType).IsRequired().HasMaxLength(20);
            builder.Property(t => t.Quantity).IsRequired();

            builder.HasOne(t => t.Branch)
                   .WithMany(b => b.InventoryTransactions)
                   .HasForeignKey(t => t.BranchId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Product)
                   .WithMany()
                   .HasForeignKey(t => t.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.SupplierPurchase)
                   .WithMany()
                   .HasForeignKey(t => t.SupplierPurchaseId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
