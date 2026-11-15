using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
    {
        public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
        {
            builder.ToTable("UnitOfMeasures");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Name).IsRequired().HasMaxLength(100);
            builder.Property(u => u.ShortName).IsRequired().HasMaxLength(20);
            builder.Property(u => u.Description).HasMaxLength(500);

            builder.HasIndex(u => u.Name).IsUnique();
            builder.HasIndex(u => u.ShortName).IsUnique();

            // A unit that products use can never be deleted.
            builder.HasMany(u => u.Products)
                   .WithOne(p => p.UnitOfMeasure)
                   .HasForeignKey(p => p.UnitOfMeasureId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
