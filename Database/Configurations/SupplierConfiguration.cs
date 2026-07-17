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
            builder.Property(s => s.OpeningBalance).IsRequired();
        }
    }

    public class SupplierPurchaseConfiguration : IEntityTypeConfiguration<SupplierPurchase>
    {
        public void Configure(EntityTypeBuilder<SupplierPurchase> builder)
        {
            builder.HasKey(s => s.Id); // Id is the primary key
            builder.Property(s => s.SupplierId).IsRequired();

        }
    }
}
