using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class AppModuleConfiguration : IEntityTypeConfiguration<AppModule>
    {
        public void Configure(EntityTypeBuilder<AppModule> builder)
        {
            builder.ToTable("Modules");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
            builder.Property(m => m.Icon).HasMaxLength(50);
        }
    }

    public class AppSubmoduleConfiguration : IEntityTypeConfiguration<AppSubmodule>
    {
        public void Configure(EntityTypeBuilder<AppSubmodule> builder)
        {
            builder.ToTable("Submodules");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
            builder.HasOne(s => s.Module)
                   .WithMany(m => m.Submodules)
                   .HasForeignKey(s => s.ModuleId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class AppMenuConfiguration : IEntityTypeConfiguration<AppMenu>
    {
        public void Configure(EntityTypeBuilder<AppMenu> builder)
        {
            builder.ToTable("Menus");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
            builder.Property(m => m.FrontendRoute).HasMaxLength(200);
            builder.Property(m => m.Icon).HasMaxLength(50);
            builder.Property(m => m.Permission).HasMaxLength(100);
            builder.HasIndex(m => new { m.SubmoduleId, m.ParentId, m.MenuOrder });

            builder.HasOne(m => m.Submodule)
                   .WithMany(s => s.Menus)
                   .HasForeignKey(m => m.SubmoduleId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(m => m.Parent)
                   .WithMany(m => m.Children)
                   .HasForeignKey(m => m.ParentId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class AppMenuActionConfiguration : IEntityTypeConfiguration<AppMenuAction>
    {
        public void Configure(EntityTypeBuilder<AppMenuAction> builder)
        {
            builder.ToTable("MenuActions");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
            builder.Property(a => a.ComponentCode).IsRequired().HasMaxLength(50);
            // Postgres text[]: a short list of "METHOD /route" strings.
            builder.Property(a => a.ApiRoutes).HasColumnType("text[]");
            builder.HasOne(a => a.Menu)
                   .WithMany(m => m.Actions)
                   .HasForeignKey(a => a.MenuId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
