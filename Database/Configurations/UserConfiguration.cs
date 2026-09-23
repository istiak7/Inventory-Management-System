using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Inventory_Management_System.Entities;

namespace Inventory_Management_System.Database.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Name).IsRequired().HasMaxLength(50);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(100);
            builder.Property(u => u.PasswordHash).IsRequired();

            builder.HasIndex(u => u.Email).IsUnique();

            // A user has exactly one role.
            builder.HasOne(u => u.Role)
                   .WithMany()
                   .HasForeignKey(u => u.RoleId)
                   .OnDelete(DeleteBehavior.Restrict);

            // BranchId is optional: null = all branches (admin), a value = one branch (staff).
            builder.HasOne(u => u.Branch)
                   .WithMany()
                   .HasForeignKey(u => u.BranchId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
    {
        public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
        {
            builder.HasKey(t => t.Id);
            // SHA-256 as hex = 64 characters. Unique: it is how a token is looked up.
            builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
            builder.HasIndex(t => t.TokenHash).IsUnique();

            builder.HasOne(t => t.User)
                   .WithMany()
                   .HasForeignKey(t => t.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
