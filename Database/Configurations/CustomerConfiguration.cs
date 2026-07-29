using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory_Management_System.Database.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.HasKey(c => c.Id); // Id is the primary key
            builder.Property(c => c.Group).HasMaxLength(100);
            builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Description).HasMaxLength(500);
            builder.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(20);
            builder.Property(c => c.Email).HasMaxLength(100);
            builder.Property(c => c.Address).HasMaxLength(250);
            builder.Property(c => c.NID).HasMaxLength(20);
            builder.Property(c => c.OpeningBalance).IsRequired().HasPrecision(18, 2);

            // The mobile number IS the customer's identity: the sales form looks a customer up by
            // it and creates one only when nothing comes back. Unique at the database level, not
            // just in the handler, so two tills ringing up the same new walk-in at the same moment
            // cannot both win the "does this phone exist?" check and insert twice.
            // Values are stored normalized (see CustomerPhoneNumber.Normalize) so the comparison
            // is not defeated by dashes, spaces or a +880 prefix.
            builder.HasIndex(c => c.PhoneNumber).IsUnique();
        }
    }

    public class CustomerSaleConfiguration : IEntityTypeConfiguration<CustomerSale>
    {
        public void Configure(EntityTypeBuilder<CustomerSale> builder)
        {
            builder.HasKey(s => s.Id); // Id is the primary key
            builder.Property(s => s.CustomerId).IsRequired();
            builder.Property(s => s.BranchId).IsRequired();
            builder.Property(s => s.InvoiceNumber).IsRequired().HasMaxLength(100);
            builder.HasIndex(s => s.InvoiceNumber).IsUnique(); // one invoice number, one sale
            builder.Property(s => s.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(s => s.SaleType).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(s => s.SubTotal).HasPrecision(18, 2);
            builder.Property(s => s.DiscountAmount).HasPrecision(18, 2);
            builder.Property(s => s.TaxAmount).HasPrecision(18, 2);
            builder.Property(s => s.TotalAmount).HasPrecision(18, 2);
            builder.Property(s => s.PaidAmount).HasPrecision(18, 2);
            builder.Property(s => s.DueAmount).HasPrecision(18, 2);

            builder.HasOne(s => s.Customer)
                   .WithMany(c => c.CustomerSales)
                   .HasForeignKey(s => s.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Branch)
                   .WithMany(b => b.CustomerSales)
                   .HasForeignKey(s => s.BranchId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class SaleDetailsConfiguration : IEntityTypeConfiguration<SaleDetails>
    {
        public void Configure(EntityTypeBuilder<SaleDetails> builder)
        {
            builder.HasKey(d => d.Id); // Id is the primary key
            builder.Property(d => d.SaleId).IsRequired();
            builder.Property(d => d.Quantity).IsRequired();
            builder.Property(d => d.UnitPrice).HasPrecision(18, 2);
            builder.Property(d => d.DiscountPerItem).HasPrecision(18, 2);
            builder.Property(d => d.TotalAmount).HasPrecision(18, 2);
            builder.Property(d => d.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

            builder.HasOne(d => d.CustomerSale)
                   .WithMany(s => s.SaleDetails)
                   .HasForeignKey(d => d.SaleId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.ProductVariant)
                   .WithMany(v => v.SaleDetails)
                   .HasForeignKey(d => d.ProductVariantId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class CustomerPaymentConfiguration : IEntityTypeConfiguration<CustomerPayment>
    {
        public void Configure(EntityTypeBuilder<CustomerPayment> builder)
        {
            builder.HasKey(p => p.Id); // Id is the primary key
            builder.Property(p => p.PaymentMethod).IsRequired().HasMaxLength(50);
            builder.Property(p => p.Amount).HasPrecision(18, 2);

            builder.HasOne(p => p.Customer)
                   .WithMany(c => c.CustomerPayments)
                   .HasForeignKey(p => p.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Branch)
                   .WithMany(b => b.CustomerPayments)
                   .HasForeignKey(p => p.BranchId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class SaleCustomerPaymentConfiguration : IEntityTypeConfiguration<SaleCustomerPayment>
    {
        public void Configure(EntityTypeBuilder<SaleCustomerPayment> builder)
        {
            builder.HasKey(sp => sp.Id); // Id is the primary key
            builder.Property(sp => sp.Amount).HasPrecision(18, 2);

            builder.HasOne(sp => sp.CustomerSale)
                   .WithMany(s => s.SaleCustomerPayments)
                   .HasForeignKey(sp => sp.SaleId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sp => sp.CustomerPayment)
                   .WithMany(p => p.SaleCustomerPayments)
                   .HasForeignKey(sp => sp.CustomerPaymentId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class CustomerTransactionConfiguration : IEntityTypeConfiguration<CustomerTransaction>
    {
        public void Configure(EntityTypeBuilder<CustomerTransaction> builder)
        {
            builder.HasKey(t => t.Id); // Id is the primary key
            builder.Property(t => t.TransactionType).IsRequired().HasMaxLength(20);
            builder.Property(t => t.Debit).HasPrecision(18, 2);
            builder.Property(t => t.Credit).HasPrecision(18, 2);
            builder.Property(t => t.BalanceAfter).HasPrecision(18, 2);

            builder.HasOne(t => t.Customer)
                   .WithMany(c => c.CustomerTransactions)
                   .HasForeignKey(t => t.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.CustomerSale)
                   .WithMany()
                   .HasForeignKey(t => t.SaleId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.CustomerPayment)
                   .WithMany()
                   .HasForeignKey(t => t.CustomerPaymentId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
