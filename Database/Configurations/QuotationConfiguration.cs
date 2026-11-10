using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
    {
        public void Configure(EntityTypeBuilder<Quotation> builder)
        {
            builder.HasKey(q => q.Id);
            builder.Property(q => q.QuotationNumber).IsRequired().HasMaxLength(100);
            builder.HasIndex(q => q.QuotationNumber).IsUnique();
            builder.Property(q => q.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(q => q.Notes).HasMaxLength(1000);
            builder.Property(q => q.TermsAndConditions).HasMaxLength(4000);
            builder.Property(q => q.SubTotal).HasPrecision(18, 2);
            builder.Property(q => q.DiscountAmount).HasPrecision(18, 2);
            builder.Property(q => q.TaxAmount).HasPrecision(18, 2);
            builder.Property(q => q.TotalAmount).HasPrecision(18, 2);

            // One sale per quotation, enforced by the database as the last line of defence.
            builder.HasIndex(q => q.ConvertedSaleId)
                   .IsUnique()
                   .HasFilter("\"ConvertedSaleId\" IS NOT NULL");

            builder.HasOne(q => q.Customer)
                   .WithMany()
                   .HasForeignKey(q => q.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(q => q.ConvertedSale)
                   .WithMany()
                   .HasForeignKey(q => q.ConvertedSaleId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class QuotationDetailsConfiguration : IEntityTypeConfiguration<QuotationDetails>
    {
        public void Configure(EntityTypeBuilder<QuotationDetails> builder)
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.ProductName).IsRequired().HasMaxLength(250);
            builder.Property(d => d.Sku).IsRequired().HasMaxLength(100);
            builder.Property(d => d.Uom).IsRequired().HasMaxLength(20);
            builder.Property(d => d.Warranty).HasMaxLength(100);
            builder.Property(d => d.UnitPrice).HasPrecision(18, 2);
            builder.Property(d => d.DiscountAmount).HasPrecision(18, 2);
            builder.Property(d => d.TotalAmount).HasPrecision(18, 2);

            builder.HasOne(d => d.Quotation)
                   .WithMany(q => q.QuotationDetails)
                   .HasForeignKey(d => d.QuotationId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.ProductVariant)
                   .WithMany()
                   .HasForeignKey(d => d.ProductVariantId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
