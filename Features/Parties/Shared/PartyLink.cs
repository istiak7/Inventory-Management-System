using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Customers.Shared;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Parties.Shared
{
    // Keeps the Customer and Supplier rows of one party (same phone number) linked.
    //
    //  - Every supplier is a credit party, so it always gets a customer row.
    //  - A customer gets a supplier row only when marked "Credit Customer / Supplier".
    //
    // The link is Customer.SupplierId. These methods only add or change tracked rows;
    // the caller saves them inside its own transaction.
    public class PartyLink(AppDbContext db)
    {
        private const string DefaultSupplierGroup = "General";

        // Finds the customer with the supplier's phone number and links it, or creates one.
        // Returns an error message when the phone number already belongs to another party.
        public async Task<(Customer? Customer, string? Error)> EnsureCustomerForSupplierAsync(
            Supplier supplier, CancellationToken cancellationToken)
        {
            var linked = supplier.Id > 0
                ? await db.Customers.FirstOrDefaultAsync(c => c.SupplierId == supplier.Id, cancellationToken)
                : null;
            if (linked != null)
                return (linked, null);

            var phoneNumber = CustomerPhoneNumber.Normalize(supplier.PhoneNumber);
            if (phoneNumber.Length < CustomerPhoneNumber.MinimumDigits)
                return (null, $"Phone number must contain at least {CustomerPhoneNumber.MinimumDigits} digits.");

            var customer = await db.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber, cancellationToken);
            if (customer != null)
            {
                if (customer.SupplierId != null && customer.SupplierId != supplier.Id)
                    return (null, $"Customer '{customer.Name}' with this phone number is already linked to another supplier.");

                customer.Supplier = supplier;
                customer.UpDatedAt = DateTime.UtcNow;
                return (customer, null);
            }

            customer = new Customer
            {
                Group = supplier.Group,
                Name = supplier.Name,
                Description = supplier.Description,
                PhoneNumber = phoneNumber,
                Email = supplier.Email,
                Address = supplier.Address,
                NID = supplier.NID,
                // The opening balance stays on the supplier side, where it was entered.
                OpeningBalance = 0,
                CreatedAt = DateTime.UtcNow,
                Supplier = supplier,
            };
            await db.Customers.AddAsync(customer, cancellationToken);
            return (customer, null);
        }

        // Finds the supplier with the customer's phone number and links it, or creates one.
        public async Task<(Supplier? Supplier, string? Error)> EnsureSupplierForCustomerAsync(
            Customer customer, CancellationToken cancellationToken)
        {
            if (customer.SupplierId is int supplierId)
            {
                var current = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken);
                if (current != null)
                    return (current, null);
            }
            if (customer.Supplier != null)
                return (customer.Supplier, null);

            var phoneNumber = CustomerPhoneNumber.Normalize(customer.PhoneNumber);
            var supplier = await db.Suppliers
                .Include(s => s.Customer)
                .Where(s => s.PhoneNumber == phoneNumber)
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (supplier != null)
            {
                if (supplier.Customer != null && supplier.Customer != customer)
                    return (null, $"Supplier '{supplier.Name}' with this phone number is already linked to another customer.");

                customer.Supplier = supplier;
                customer.UpDatedAt = DateTime.UtcNow;
                return (supplier, null);
            }

            // Supplier emails are unique when filled in, so leave it empty if another supplier has it.
            var email = customer.Email?.Trim() ?? string.Empty;
            if (email.Length > 0 && await db.Suppliers.AnyAsync(s => s.Email == email, cancellationToken))
                email = string.Empty;

            supplier = new Supplier
            {
                Group = string.IsNullOrWhiteSpace(customer.Group) ? DefaultSupplierGroup : customer.Group,
                Name = customer.Name,
                Description = customer.Description,
                PhoneNumber = phoneNumber,
                Email = email,
                Address = customer.Address,
                NID = customer.NID,
                // The opening balance stays on the customer side, where it was entered.
                OpeningBalance = 0,
                CreatedAt = DateTime.UtcNow,
            };
            await db.Suppliers.AddAsync(supplier, cancellationToken);
            customer.Supplier = supplier;
            customer.UpDatedAt = DateTime.UtcNow;
            return (supplier, null);
        }

        // Copies the shared contact details to the other row after an edit, so both stay the same.
        public static void CopyContact(Customer from, Supplier to)
        {
            to.Name = from.Name;
            to.PhoneNumber = from.PhoneNumber;
            to.Address = from.Address;
            to.NID = from.NID;
            to.UpDatedAt = DateTime.UtcNow;
        }

        public static void CopyContact(Supplier from, Customer to)
        {
            to.Name = from.Name;
            to.PhoneNumber = from.PhoneNumber;
            to.Address = from.Address;
            to.NID = from.NID;
            to.UpDatedAt = DateTime.UtcNow;
        }
    }
}
