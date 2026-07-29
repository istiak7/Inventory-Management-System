using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class BranchConfiguration : IEntityTypeConfiguration<Branch>
    {
        public void Configure(EntityTypeBuilder<Branch> builder)
        {
            builder.HasKey(b => b.Id); // Id is the primary key
            builder.Property(b => b.Name).IsRequired().HasMaxLength(100);
            builder.Property(b => b.Location).HasMaxLength(200);
            builder.Property(b => b.PhoneNumber).HasMaxLength(20);
            builder.Property(b => b.Email).HasMaxLength(100);
        }
    }
}
