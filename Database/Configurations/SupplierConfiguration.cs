using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.HasKey(s => s.Id); // Id is the primary key
            builder.Property(s => s.Group).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Description).HasMaxLength(500);
            builder.Property(s => s.PhoneNumber).HasMaxLength(20);
            builder.Property(s => s.Email).HasMaxLength(100).IsRequired();
            builder.HasIndex(s => s.Email).IsUnique(); // Email should be unique
            builder.Property(s => s.NID).HasMaxLength(20);
            builder.Property(s => s.OpeningBalance).IsRequired().HasPrecision(18, 2);
        }
    }

    public class SupplierPurchaseConfiguration : IEntityTypeConfiguration<SupplierPurchase>
    {
        public void Configure(EntityTypeBuilder<SupplierPurchase> builder)
        {
            builder.HasKey(s => s.Id); // Id is the primary key
            builder.Property(s => s.SupplierId).IsRequired();
            builder.Property(s => s.BranchId).IsRequired();
            builder.Property(s => s.InvoiceNumber).HasMaxLength(100);
            builder.Property(s => s.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(s => s.PurchaseType).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(s => s.TotalAmount).HasPrecision(18, 2);
            builder.Property(s => s.PaidAmount).HasPrecision(18, 2);
            builder.Property(s => s.DueAmount).HasPrecision(18, 2);

            builder.HasOne(s => s.Supplier)
                   .WithMany(s => s.SupplierPurchases)
                   .HasForeignKey(s => s.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Branch)
                   .WithMany(b => b.SupplierPurchases)
                   .HasForeignKey(s => s.BranchId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class SupplierPurchaseDetailsConfiguration : IEntityTypeConfiguration<SupplierPurchaseDetails>
    {
        public void Configure(EntityTypeBuilder<SupplierPurchaseDetails> builder)
        {
            builder.HasKey(d => d.Id); // Id is the primary key
            builder.Property(d => d.OrderedQuantity).IsRequired();
            builder.Property(d => d.UnitPrice).HasPrecision(18, 2);
            builder.Property(d => d.TotalAmount).HasPrecision(18, 2);
            builder.Property(d => d.WarrantyMonths).IsRequired();
            builder.Property(d => d.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(d => d.PurchaseId).IsRequired();

            builder.HasOne(d => d.SupplierPurchase)
                   .WithMany(s => s.SupplierPurchaseDetails)
                   .HasForeignKey(d => d.PurchaseId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.ProductVariant)
                   .WithMany(v => v.SupplierPurchaseDetails)
                   .HasForeignKey(d => d.ProductVariantId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
    {
        public void Configure(EntityTypeBuilder<SupplierPayment> builder)
        {
            builder.HasKey(p => p.Id); // Id is the primary key
            builder.Property(p => p.PaymentMethod).IsRequired().HasMaxLength(50);
            builder.Property(p => p.Amount).HasPrecision(18, 2);

            builder.HasOne(p => p.Supplier)
                   .WithMany(s => s.SupplierPayments)
                   .HasForeignKey(p => p.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class SupplierPurchasePaymentConfiguration : IEntityTypeConfiguration<SupplierPurchasePayment>
    {
        public void Configure(EntityTypeBuilder<SupplierPurchasePayment> builder)
        {
            builder.HasKey(pp => pp.Id); // Id is the primary key
            builder.Property(pp => pp.Amount).HasPrecision(18, 2);

            builder.HasOne(pp => pp.SupplierPurchase)
                   .WithMany(s => s.SupplierPurchasePayments)
                   .HasForeignKey(pp => pp.SupplierPurchaseId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pp => pp.SupplierPayment)
                   .WithMany(p => p.SupplierPurchasePayments)
                   .HasForeignKey(pp => pp.SupplierPaymentId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class SupplierTransactionConfiguration : IEntityTypeConfiguration<SupplierTransaction>
    {
        public void Configure(EntityTypeBuilder<SupplierTransaction> builder)
        {
            builder.HasKey(t => t.Id); // Id is the primary key
            builder.Property(t => t.TransactionType).IsRequired().HasMaxLength(20);
            builder.Property(t => t.Debit).HasPrecision(18, 2);
            builder.Property(t => t.Credit).HasPrecision(18, 2);
            builder.Property(t => t.BalanceAfter).HasPrecision(18, 2);

            builder.HasOne(t => t.Supplier)
                   .WithMany(s => s.SupplierTransactions)
                   .HasForeignKey(t => t.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.SupplierPurchase)
                   .WithMany()
                   .HasForeignKey(t => t.SupplierPurchaseId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.SupplierPayment)
                   .WithMany()
                   .HasForeignKey(t => t.SupplierPaymentId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
