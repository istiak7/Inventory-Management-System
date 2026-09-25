using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
    {
        public void Configure(EntityTypeBuilder<Expense> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Amount).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(500);
            builder.Property(e => e.ExpenseDate).IsRequired();
            builder.HasOne(e => e.ExpenseCategory)
                   .WithMany(ec => ec.Expenses)
                   .HasForeignKey(e => e.ExpenseCategoryId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
