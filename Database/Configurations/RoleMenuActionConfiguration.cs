using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class RoleMenuActionConfiguration : IEntityTypeConfiguration<RoleMenuAction>
    {
        public void Configure(EntityTypeBuilder<RoleMenuAction> builder)
        {
            builder.ToTable("RoleMenuActions");
            builder.HasKey(x => x.Id);

            // The same action should not be given to a role twice.
            builder.HasIndex(x => new { x.RoleId, x.MenuActionId }).IsUnique();

            builder.HasOne(x => x.Role)
                   .WithMany(r => r.RoleMenuActions)
                   .HasForeignKey(x => x.RoleId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Removing a menu action removes it from every role too.
            builder.HasOne(x => x.MenuAction)
                   .WithMany(a => a.RoleMenuActions)
                   .HasForeignKey(x => x.MenuActionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
