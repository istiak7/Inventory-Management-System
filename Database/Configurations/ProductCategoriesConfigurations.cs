using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class ProductCategoriesConfigurations : IEntityTypeConfiguration<ProductCategories>
    {
        public void Configure(EntityTypeBuilder<ProductCategories> builder)
        {
            builder.HasKey(pc => pc.Id); // Id is the primary key
            builder.Property(pc => pc.CategoryName).IsRequired().HasMaxLength(100);
            builder.Property(pc => pc.Description).HasMaxLength(500);
            builder.Property(pc => pc.ImageUrl).HasMaxLength(200);
            builder.Property(pc => pc.Code).IsRequired().HasMaxLength(50);
        }
    }
    public class ProductSubCategoriesConfigurations : IEntityTypeConfiguration<ProductSubCategories>
    {
        public void Configure(EntityTypeBuilder<ProductSubCategories> builder)
        {
            builder.HasKey(psc => psc.Id); // Id is the primary key
            builder.Property(psc => psc.SubCategoryName).IsRequired().HasMaxLength(100);
            builder.Property(psc => psc.Description).HasMaxLength(500);
            builder.Property(psc => psc.ImageUrl).HasMaxLength(200);
            builder.Property(psc => psc.Code).IsRequired().HasMaxLength(50);
            // Configure the relationship with ProductCategories
            builder.HasOne(psc => psc.ProductCategories)
                   .WithMany(pc => pc.ProductSubCategories)
                   .HasForeignKey(psc => psc.ProductCategoryId)
                   .OnDelete(DeleteBehavior.Cascade); // Optional: specify delete behavior
        }
    }

    public class ProductConfigurations : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(p => p.Id); // Id is the primary key
            builder.Property(p => p.ProductName).IsRequired().HasMaxLength(100);
            builder.Property(p => p.ProductDescription).HasMaxLength(500);
            builder.Property(p => p.ProductImageUrl).HasMaxLength(200);
            builder.Property(p => p.SKU).IsRequired().HasMaxLength(50);
            builder.HasIndex(p => p.SKU).IsUnique(); // SKU is the product business key
            builder.Property(p => p.ProductPrice).HasPrecision(18, 2);
            // Configure the relationship with ProductSubCategories
            builder.HasOne(p => p.ProductSubCategories)
                   .WithMany(psc => psc.Products)
                   .HasForeignKey(p => p.ProductSubCategoryId)
                   .OnDelete(DeleteBehavior.Cascade); // Optional: specify delete behavior
        }
    }
}
