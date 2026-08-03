using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
    {
        public void Configure(EntityTypeBuilder<StockTransfer> builder)
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Reference).IsRequired().HasMaxLength(50);
            builder.HasIndex(t => t.Reference).IsUnique();
            builder.Property(t => t.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(t => t.Notes).HasMaxLength(1000);

            builder.HasOne(t => t.SourceBranch)
                   .WithMany()
                   .HasForeignKey(t => t.SourceBranchId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Both FKs point at Branch, so EF cannot infer which is which without WithMany() on
            // each side being explicit — a single shared collection would conflate inbound and
            // outbound transfers on the Branch entity.
            builder.HasOne(t => t.DestinationBranch)
                   .WithMany()
                   .HasForeignKey(t => t.DestinationBranchId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class StockTransferDetailsConfiguration : IEntityTypeConfiguration<StockTransferDetails>
    {
        public void Configure(EntityTypeBuilder<StockTransferDetails> builder)
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Quantity).IsRequired();
            builder.Property(d => d.RequestedSerialNumbersJson)
                   .IsRequired()
                   .HasColumnType("jsonb")
                   .HasDefaultValue("[]");

            builder.HasOne(d => d.StockTransfer)
                   .WithMany(t => t.StockTransferDetails)
                   .HasForeignKey(d => d.StockTransferId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.ProductVariant)
                   .WithMany()
                   .HasForeignKey(d => d.ProductVariantId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
