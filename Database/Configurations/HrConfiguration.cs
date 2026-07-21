using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.HasKey(d => d.Id); // Id is the primary key
            builder.Property(d => d.Name).IsRequired().HasMaxLength(100);
            builder.Property(d => d.Description).HasMaxLength(500);
        }
    }

    public class DesignationConfiguration : IEntityTypeConfiguration<Designation>
    {
        public void Configure(EntityTypeBuilder<Designation> builder)
        {
            builder.HasKey(d => d.Id); // Id is the primary key
            builder.Property(d => d.Name).IsRequired().HasMaxLength(100);
            builder.Property(d => d.Description).HasMaxLength(500);
        }
    }
}
