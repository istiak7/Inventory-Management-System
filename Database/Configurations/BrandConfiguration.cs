using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class BrandConfiguration : IEntityTypeConfiguration<Brand>
    {
        public void Configure(EntityTypeBuilder<Brand> builder)
        {
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
            builder.Property(b => b.Description).HasMaxLength(500);
            builder.Property(b => b.LogoUrl).HasMaxLength(500);

            builder.HasMany(b => b.Products)
                   .WithOne(p => p.Brand)
                   .HasForeignKey(p => p.BrandId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
