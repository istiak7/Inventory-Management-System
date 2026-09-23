using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class ProductCategoriesConfigurations : IEntityTypeConfiguration<ProductCategories>
    {
        public void Configure(EntityTypeBuilder<ProductCategories> builder)
        {
            builder.HasKey(pc => pc.Id); 
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
            builder.HasKey(psc => psc.Id); 
            builder.Property(psc => psc.SubCategoryName).IsRequired().HasMaxLength(100);
            builder.Property(psc => psc.Description).HasMaxLength(500);
            builder.Property(psc => psc.ImageUrl).HasMaxLength(200);
            builder.Property(psc => psc.Code).IsRequired().HasMaxLength(50);
            builder.HasOne(psc => psc.ProductCategories)
                   .WithMany(pc => pc.ProductSubCategories)
                   .HasForeignKey(psc => psc.ProductCategoryId)
                   .OnDelete(DeleteBehavior.Restrict); 
        }
    }

    public class ProductConfigurations : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.ProductName).IsRequired().HasMaxLength(100);
            builder.Property(p => p.ProductDescription).HasMaxLength(500);
            builder.Property(p => p.ProductImageUrl).HasMaxLength(200);

            builder.HasOne(p => p.ProductSubCategories)
                   .WithMany(psc => psc.Products)
                   .HasForeignKey(p => p.ProductSubCategoryId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> builder)
        {
            builder.HasKey(v => v.Id);
            builder.Property(v => v.SKU).IsRequired().HasMaxLength(50);
            builder.HasIndex(v => v.SKU).IsUnique();
            builder.Property(v => v.Barcode).HasMaxLength(50);
            builder.Property(v => v.SellingPrice).HasPrecision(18, 2);
            builder.Property(v => v.AttributesJson).HasColumnType("jsonb");
            builder.HasIndex(v => v.AttributesJson).HasMethod("gin");

            builder.Property(v => v.SearchText)
                .HasColumnType("text")
                .IsRequired(false);

            builder.Property(v => v.SearchVector)
                .HasColumnType("tsvector")
                .ValueGeneratedOnAddOrUpdate();

            builder.HasIndex(v => v.SearchVector)
                .HasMethod("GIN");

            builder.HasOne(v => v.Product)
                   .WithMany(p => p.ProductVariants)
                   .HasForeignKey(v => v.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
