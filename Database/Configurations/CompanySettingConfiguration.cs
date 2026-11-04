using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class CompanySettingConfiguration : IEntityTypeConfiguration<CompanySetting>
    {
        public void Configure(EntityTypeBuilder<CompanySetting> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
            builder.Property(c => c.Tagline).HasMaxLength(200);
            builder.Property(c => c.Address).HasMaxLength(500);
            builder.Property(c => c.Phone).HasMaxLength(100);
            builder.Property(c => c.Email).HasMaxLength(150);
            builder.Property(c => c.Website).HasMaxLength(200);
            builder.Property(c => c.LogoUrl).HasMaxLength(500);
            builder.Property(c => c.QrUrl).HasMaxLength(500);
            builder.Property(c => c.InvoiceFooter).HasMaxLength(500);
        }
    }
}
