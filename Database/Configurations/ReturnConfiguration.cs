using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class SaleEditConfiguration : IEntityTypeConfiguration<SaleEdit>
    {
        public void Configure(EntityTypeBuilder<SaleEdit> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            builder.Property(e => e.ChangesJson).IsRequired().HasColumnType("text");
            foreach (var name in new[]
            {
                nameof(SaleEdit.OldSubTotal), nameof(SaleEdit.OldDiscountAmount), nameof(SaleEdit.OldTaxAmount),
                nameof(SaleEdit.OldTotalAmount), nameof(SaleEdit.OldPaidAmount), nameof(SaleEdit.OldDueAmount),
                nameof(SaleEdit.NewSubTotal), nameof(SaleEdit.NewDiscountAmount), nameof(SaleEdit.NewTaxAmount),
                nameof(SaleEdit.NewTotalAmount), nameof(SaleEdit.NewPaidAmount), nameof(SaleEdit.NewDueAmount),
                nameof(SaleEdit.ReturnedAmount), nameof(SaleEdit.SettlementReversed), nameof(SaleEdit.SettlementRedone),
                nameof(SaleEdit.CashReleased), nameof(SaleEdit.CashRefunded), nameof(SaleEdit.CashReapplied),
                nameof(SaleEdit.AdditionalPayment),
            })
                builder.Property(name).HasPrecision(18, 2);

            builder.HasOne(e => e.CustomerSale).WithMany().HasForeignKey(e => e.SaleId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(e => e.SaleId);
        }
    }

    public class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
    {
        public void Configure(EntityTypeBuilder<SalesReturn> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.ReturnNumber).IsRequired().HasMaxLength(50);
            builder.HasIndex(r => r.ReturnNumber).IsUnique();
            builder.Property(r => r.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(r => r.Reason).IsRequired().HasMaxLength(500);
            builder.Property(r => r.Remarks).HasMaxLength(500);
            builder.Property(r => r.CancelReason).HasMaxLength(500);
            builder.Property(r => r.SubTotal).HasPrecision(18, 2);
            builder.Property(r => r.DiscountAmount).HasPrecision(18, 2);
            builder.Property(r => r.TaxAmount).HasPrecision(18, 2);
            builder.Property(r => r.TotalAmount).HasPrecision(18, 2);
            builder.Property(r => r.DueReduced).HasPrecision(18, 2);
            builder.Property(r => r.SettlementReversed).HasPrecision(18, 2);
            builder.Property(r => r.CashRefunded).HasPrecision(18, 2);

            builder.HasOne(r => r.CustomerSale).WithMany().HasForeignKey(r => r.SaleId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(r => r.Branch).WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(r => r.ReturnDate);
        }
    }

    public class SalesReturnLineConfiguration : IEntityTypeConfiguration<SalesReturnLine>
    {
        public void Configure(EntityTypeBuilder<SalesReturnLine> builder)
        {
            builder.HasKey(l => l.Id);
            builder.Property(l => l.UnitPrice).HasPrecision(18, 2);
            builder.Property(l => l.Amount).HasPrecision(18, 2);
            builder.Property(l => l.UnitCost).HasPrecision(18, 2);

            builder.HasOne(l => l.SalesReturn).WithMany(r => r.Lines).HasForeignKey(l => l.SalesReturnId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(l => l.SaleDetails).WithMany().HasForeignKey(l => l.SaleDetailsId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(l => l.ProductVariant).WithMany().HasForeignKey(l => l.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(l => l.ProductSerial).WithMany().HasForeignKey(l => l.ProductSerialId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturn>
    {
        public void Configure(EntityTypeBuilder<PurchaseReturn> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.ReturnNumber).IsRequired().HasMaxLength(50);
            builder.HasIndex(r => r.ReturnNumber).IsUnique();
            builder.Property(r => r.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(r => r.Reason).IsRequired().HasMaxLength(500);
            builder.Property(r => r.Remarks).HasMaxLength(500);
            builder.Property(r => r.CancelReason).HasMaxLength(500);
            builder.Property(r => r.SubTotal).HasPrecision(18, 2);
            builder.Property(r => r.DiscountAmount).HasPrecision(18, 2);
            builder.Property(r => r.TotalAmount).HasPrecision(18, 2);
            builder.Property(r => r.DueReduced).HasPrecision(18, 2);
            builder.Property(r => r.SettlementReversed).HasPrecision(18, 2);
            builder.Property(r => r.CashRefunded).HasPrecision(18, 2);

            builder.HasOne(r => r.SupplierPurchase).WithMany().HasForeignKey(r => r.PurchaseId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(r => r.Supplier).WithMany().HasForeignKey(r => r.SupplierId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(r => r.Branch).WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(r => r.ReturnDate);
        }
    }

    public class PurchaseReturnLineConfiguration : IEntityTypeConfiguration<PurchaseReturnLine>
    {
        public void Configure(EntityTypeBuilder<PurchaseReturnLine> builder)
        {
            builder.HasKey(l => l.Id);
            builder.Property(l => l.UnitPrice).HasPrecision(18, 2);
            builder.Property(l => l.Amount).HasPrecision(18, 2);

            builder.HasOne(l => l.PurchaseReturn).WithMany(r => r.Lines).HasForeignKey(l => l.PurchaseReturnId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(l => l.SupplierPurchaseDetails).WithMany().HasForeignKey(l => l.SupplierPurchaseDetailsId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(l => l.ProductVariant).WithMany().HasForeignKey(l => l.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class PurchaseReturnSerialConfiguration : IEntityTypeConfiguration<PurchaseReturnSerial>
    {
        public void Configure(EntityTypeBuilder<PurchaseReturnSerial> builder)
        {
            builder.HasKey(s => s.Id);
            builder.HasOne(s => s.PurchaseReturnLine).WithMany(l => l.Serials).HasForeignKey(s => s.PurchaseReturnLineId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(s => s.ProductSerial).WithMany().HasForeignKey(s => s.ProductSerialId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
