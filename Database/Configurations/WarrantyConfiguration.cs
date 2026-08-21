using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class WarrantyClaimConfiguration : IEntityTypeConfiguration<WarrantyClaim>
    {
        public void Configure(EntityTypeBuilder<WarrantyClaim> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.ClaimNumber).IsRequired().HasMaxLength(50);
            builder.HasIndex(c => c.ClaimNumber).IsUnique();

            builder.Property(c => c.DefectDescription).IsRequired().HasMaxLength(1000);
            builder.Property(c => c.AccessoriesReceived).HasMaxLength(500);
            builder.Property(c => c.TechnicianName).HasMaxLength(100);
            builder.Property(c => c.ResolutionNotes).HasMaxLength(1000);

            builder.Property(c => c.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(c => c.Resolution).HasConversion<string>().HasMaxLength(20);

            builder.HasIndex(c => c.ProductSerialId);
            builder.HasIndex(c => c.CustomerId);
            builder.HasIndex(c => c.Status);

            builder.HasOne(c => c.ProductSerial)
                   .WithMany()
                   .HasForeignKey(c => c.ProductSerialId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.ReplacementSerial)
                   .WithMany()
                   .HasForeignKey(c => c.ReplacementSerialId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.SaleDetails)
                   .WithMany()
                   .HasForeignKey(c => c.SaleDetailsId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Customer)
                   .WithMany()
                   .HasForeignKey(c => c.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Branch)
                   .WithMany()
                   .HasForeignKey(c => c.BranchId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
