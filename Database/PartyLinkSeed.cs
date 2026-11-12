using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Customers.Shared;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Database
{
    // One-time step for suppliers that existed before customer <-> supplier linking.
    //
    // Every supplier is a credit party, so each one gets a customer row:
    //   - its phone number is cleaned up the same way customer numbers are;
    //   - a customer with that number is linked to it, or a new customer is created.
    //
    // Old open balances are NOT settled against each other: automatic settlement only runs on
    // new credit sales and credit purchases. Runs once (recorded in SeedHistory).
    public static class PartyLinkSeed
    {
        private const string SeedName = "PartyLinks.v1";

        public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken cancellationToken)
        {
            if (await db.SeedHistory.AnyAsync(s => s.Name == SeedName, cancellationToken)) return;

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var suppliers = await db.Suppliers
                .Include(s => s.Customer)
                .Where(s => s.IsActive != (int)EntityStatus.Deleted)
                .OrderBy(s => s.Id)
                .ToListAsync(cancellationToken);
            var customersByPhone = await db.Customers.ToDictionaryAsync(c => c.PhoneNumber, cancellationToken);

            int linked = 0, created = 0, skipped = 0;
            var phonesTaken = new HashSet<string>();
            foreach (var supplier in suppliers)
            {
                if (supplier.Customer != null)
                {
                    phonesTaken.Add(supplier.Customer.PhoneNumber);
                    continue;
                }

                var phoneNumber = CustomerPhoneNumber.Normalize(supplier.PhoneNumber);
                if (phoneNumber.Length < CustomerPhoneNumber.MinimumDigits)
                {
                    logger.LogWarning("Party link: supplier {Id} '{Name}' has no valid phone number ({Phone}); not linked.",
                        supplier.Id, supplier.Name, supplier.PhoneNumber);
                    skipped++;
                    continue;
                }
                supplier.PhoneNumber = phoneNumber;

                // Two old suppliers with one phone number: only the first gets the customer row.
                if (!phonesTaken.Add(phoneNumber))
                {
                    logger.LogWarning("Party link: supplier {Id} '{Name}' shares phone {Phone} with an earlier supplier; not linked.",
                        supplier.Id, supplier.Name, phoneNumber);
                    skipped++;
                    continue;
                }

                if (customersByPhone.TryGetValue(phoneNumber, out var customer))
                {
                    if (customer.SupplierId != null)
                    {
                        skipped++;
                        continue;
                    }
                    customer.Supplier = supplier;
                    linked++;
                    continue;
                }

                customer = new Customer
                {
                    Group = supplier.Group ?? string.Empty,
                    Name = supplier.Name,
                    Description = supplier.Description ?? string.Empty,
                    PhoneNumber = phoneNumber,
                    Email = supplier.Email ?? string.Empty,
                    Address = supplier.Address ?? string.Empty,
                    NID = supplier.NID ?? string.Empty,
                    OpeningBalance = 0,
                    CreatedAt = DateTime.UtcNow,
                    Supplier = supplier,
                };
                await db.Customers.AddAsync(customer, cancellationToken);
                customersByPhone[phoneNumber] = customer;
                created++;
            }

            await db.SeedHistory.AddAsync(new SeedHistory { Name = SeedName }, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Party link: {Linked} suppliers linked to existing customers, {Created} customers created, {Skipped} skipped.",
                linked, created, skipped);
        }
    }
}
