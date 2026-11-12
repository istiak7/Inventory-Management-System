using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Shared.Extensions.LedgerExtensions;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Parties.Shared
{
    // Automatic FIFO settlement between the customer and supplier ledgers of one party.
    //
    // Example: Party A owes us 5,000 on sales. We buy from A on credit for 3,000.
    // Settled = min(3,000 still due on the new bill, 5,000 A owes us) = 3,000.
    //   - the new bill gets an "Adjustment" payment of 3,000 (we owe A nothing for it)
    //   - A's open sales get an "Adjustment" payment of 3,000, oldest sale first
    // A now owes us 2,000. No cash moved, so cash reports leave "Adjustment" out.
    //
    // A credit sale works the same way the other way round.
    //
    // Call inside the caller's transaction, after the new document and its own ledger rows
    // are added. Lock order: customer before supplier (see RowLockExtensions).
    public class CrossLedgerSettlement(AppDbContext db)
    {
        // A credit sale to a party that is also our supplier: what we owe them pays this sale.
        public async Task<PartySettlement?> SettleSaleAsync(
            CustomerSale sale, Customer customer, CancellationToken cancellationToken)
        {
            if (sale.DueAmount <= 0 || customer.SupplierId is not int supplierId)
                return null;

            // The new rows must be in the database before running balances are read.
            await db.SaveChangesAsync(cancellationToken);

            await db.LockRowAsync<Customer>(customer.Id, cancellationToken);
            await db.LockRowAsync<Supplier>(supplierId, cancellationToken);
            var supplier = await db.Suppliers.FirstOrDefaultAsync(
                s => s.Id == supplierId && s.IsActive != (int)EntityStatus.Deleted, cancellationToken);
            if (supplier == null)
                return null;

            var payable = await db.SupplierTransactions
                .Where(t => t.SupplierId == supplierId)
                .GetLatestBalanceAsync(cancellationToken);
            var amount = Math.Min(sale.DueAmount, payable);
            if (amount <= 0)
                return null;

            var date = sale.SaleDate;
            var remarks = $"Settled with sale {sale.InvoiceNumber}";

            // Customer side: this sale only.
            var customerPayment = NewCustomerPayment(customer, sale.Branch, amount, date, remarks);
            sale.ApplyPayment(amount);
            await db.SaleCustomerPayments.AddAsync(new SaleCustomerPayment
            {
                Amount = amount,
                AllocationDate = date,
                CustomerSale = sale,
                CustomerPayment = customerPayment,
            }, cancellationToken);
            var receivable = await db.CustomerTransactions
                .Where(t => t.CustomerId == customer.Id)
                .GetLatestBalanceAsync(cancellationToken);
            await db.CustomerTransactions.AddAsync(
                CustomerTransaction.ForAdjustment(customerPayment, customer, amount, receivable - amount, sale),
                cancellationToken);

            // Supplier side: our oldest open bills first.
            var supplierPayment = NewSupplierPayment(supplier, sale.Branch, amount, date, remarks);
            await AllocateToOpenPurchasesAsync(supplierPayment, supplier.Id, amount, date, cancellationToken);
            await db.SupplierTransactions.AddAsync(
                SupplierTransaction.ForAdjustment(supplierPayment, supplier, amount, payable - amount),
                cancellationToken);

            return await AddSettlementAsync(customer, supplier, customerPayment, supplierPayment, amount, date,
                sourceSale: sale, sourcePurchase: null, reversalOf: null, cancellationToken);
        }

        // A credit purchase from a party that is also our customer: what they owe us pays this bill.
        // The caller must already hold the customer lock (taken before the supplier lock).
        public async Task<PartySettlement?> SettlePurchaseAsync(
            SupplierPurchase purchase, Supplier supplier, CancellationToken cancellationToken)
        {
            if (purchase.DueAmount <= 0)
                return null;

            var customer = await db.Customers.FirstOrDefaultAsync(
                c => c.SupplierId == supplier.Id && c.IsActive != (int)EntityStatus.Deleted, cancellationToken);
            if (customer == null)
                return null;

            await db.SaveChangesAsync(cancellationToken);

            var receivable = await db.CustomerTransactions
                .Where(t => t.CustomerId == customer.Id)
                .GetLatestBalanceAsync(cancellationToken);
            var amount = Math.Min(purchase.DueAmount, receivable);
            if (amount <= 0)
                return null;

            var date = purchase.PurchaseDate;
            var remarks = $"Settled with purchase {purchase.InvoiceNumber}";

            // Supplier side: this bill only.
            var supplierPayment = NewSupplierPayment(supplier, purchase.Branch, amount, date, remarks);
            purchase.ApplyPayment(amount);
            await db.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
            {
                Amount = amount,
                AllocationDate = date,
                SupplierPurchase = purchase,
                SupplierPayment = supplierPayment,
            }, cancellationToken);
            var payable = await db.SupplierTransactions
                .Where(t => t.SupplierId == supplier.Id)
                .GetLatestBalanceAsync(cancellationToken);
            await db.SupplierTransactions.AddAsync(
                SupplierTransaction.ForAdjustment(supplierPayment, supplier, amount, payable - amount, purchase),
                cancellationToken);

            // Customer side: their oldest open sales first.
            var customerPayment = NewCustomerPayment(customer, purchase.Branch, amount, date, remarks);
            await AllocateToOpenSalesAsync(customerPayment, customer.Id, amount, date, cancellationToken);
            await db.CustomerTransactions.AddAsync(
                CustomerTransaction.ForAdjustment(customerPayment, customer, amount, receivable - amount),
                cancellationToken);

            return await AddSettlementAsync(customer, supplier, customerPayment, supplierPayment, amount, date,
                sourceSale: null, sourcePurchase: purchase, reversalOf: null, cancellationToken);
        }

        // A sale is being voided. Every settlement still sitting on it is undone: the customer
        // side goes back up (the void then takes the whole sale off), and the supplier bills it
        // paid become due again, newest allocation first.
        // customerBalance is the customer's running balance so far (the void's rows are not saved yet).
        // Returns how much was reversed, so the void refunds only the cash part, and the new balance.
        public async Task<(decimal Reversed, decimal CustomerBalance)> ReverseForVoidedSaleAsync(
            CustomerSale sale, decimal customerBalance, DateTime date, CancellationToken cancellationToken)
        {
            var onSale = await db.SaleCustomerPayments
                .IgnoreQueryFilters()
                .Where(a => a.SaleId == sale.Id && a.CustomerPayment.PaymentMethod == PartySettlement.PaymentMethod)
                .GroupBy(a => a.CustomerPaymentId)
                .Select(g => new { CustomerPaymentId = g.Key, Amount = g.Sum(a => a.Amount) })
                .ToListAsync(cancellationToken);
            onSale = onSale.Where(x => x.Amount > 0).ToList();
            if (onSale.Count == 0)
                return (0m, customerBalance);

            var paymentIds = onSale.Select(x => x.CustomerPaymentId).ToList();
            var settlements = await db.PartySettlements
                .IgnoreQueryFilters()
                .Include(s => s.Customer)
                .Include(s => s.Supplier)
                .Where(s => paymentIds.Contains(s.CustomerPaymentId))
                .OrderBy(s => s.Id)
                .ToListAsync(cancellationToken);

            decimal reversedTotal = 0;
            foreach (var settlement in settlements)
            {
                var amount = onSale.First(x => x.CustomerPaymentId == settlement.CustomerPaymentId).Amount;
                var remarks = $"Settlement reversed: sale {sale.InvoiceNumber} voided";

                // Customer side: put the settled part back on the sale, so the void's own rows
                // (which take the whole sale off) leave the right balance.
                var customerPayment = NewCustomerPayment(settlement.Customer, sale.Branch, -amount, date, remarks);
                await db.SaleCustomerPayments.AddAsync(new SaleCustomerPayment
                {
                    Amount = -amount,
                    AllocationDate = date,
                    CustomerSale = sale,
                    CustomerPayment = customerPayment,
                }, cancellationToken);
                customerBalance += amount;
                await db.CustomerTransactions.AddAsync(
                    CustomerTransaction.ForAdjustment(customerPayment, settlement.Customer, -amount, customerBalance, sale),
                    cancellationToken);

                // Supplier side: the bills this settlement paid are due again.
                await db.LockRowAsync<Supplier>(settlement.SupplierId, cancellationToken);
                var supplierPayment = NewSupplierPayment(settlement.Supplier, sale.Branch, -amount, date, remarks);
                await UnallocatePurchasesAsync(settlement, supplierPayment, amount, date, cancellationToken);
                var payable = await db.SupplierTransactions
                    .Where(t => t.SupplierId == settlement.SupplierId)
                    .GetLatestBalanceAsync(cancellationToken);
                await db.SupplierTransactions.AddAsync(
                    SupplierTransaction.ForAdjustment(supplierPayment, settlement.Supplier, -amount, payable + amount),
                    cancellationToken);

                await AddSettlementAsync(settlement.Customer, settlement.Supplier, customerPayment, supplierPayment, -amount, date,
                    sourceSale: sale, sourcePurchase: null, reversalOf: settlement, cancellationToken);

                reversedTotal += amount;
            }

            return (reversedTotal, customerBalance);
        }

        // Oldest open sales first, across all branches (the customer's debt is one debt).
        // Anything left after the sales settles the opening balance, like a normal payment.
        private async Task AllocateToOpenSalesAsync(
            CustomerPayment payment, int customerId, decimal amount, DateTime date, CancellationToken cancellationToken)
        {
            var openSales = await db.CustomerSales
                .IgnoreQueryFilters()
                .Where(s => s.CustomerId == customerId && s.DueAmount > 0 && s.Status != SaleStatus.Voided)
                .OrderBy(s => s.SaleDate).ThenBy(s => s.Id)
                .ToListAsync(cancellationToken);

            foreach (var sale in openSales)
            {
                if (amount <= 0) break;
                var take = Math.Min(amount, sale.DueAmount);
                sale.ApplyPayment(take);
                await db.SaleCustomerPayments.AddAsync(new SaleCustomerPayment
                {
                    Amount = take,
                    AllocationDate = date,
                    CustomerSale = sale,
                    CustomerPayment = payment,
                }, cancellationToken);
                amount -= take;
            }
        }

        // Oldest open approved bills first, across all branches. Anything left settles the opening balance.
        private async Task AllocateToOpenPurchasesAsync(
            SupplierPayment payment, int supplierId, decimal amount, DateTime date, CancellationToken cancellationToken)
        {
            var openPurchases = await db.SupplierPurchases
                .IgnoreQueryFilters()
                .Where(p => p.SupplierId == supplierId
                            && p.IsActive != (int)EntityStatus.Deleted
                            && p.Status == PurchaseStatus.Approved
                            && p.DueAmount > 0)
                .OrderBy(p => p.PurchaseDate).ThenBy(p => p.Id)
                .ToListAsync(cancellationToken);

            foreach (var purchase in openPurchases)
            {
                if (amount <= 0) break;
                var take = Math.Min(amount, purchase.DueAmount);
                purchase.ApplyPayment(take);
                await db.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
                {
                    Amount = take,
                    AllocationDate = date,
                    SupplierPurchase = purchase,
                    SupplierPayment = payment,
                }, cancellationToken);
                amount -= take;
            }
        }

        // Takes back up to `amount` of what this settlement (minus earlier reversals of it) paid
        // on supplier bills, newest allocation first. What is left was the opening balance.
        private async Task UnallocatePurchasesAsync(
            PartySettlement settlement, SupplierPayment reversal, decimal amount, DateTime date, CancellationToken cancellationToken)
        {
            var paymentIds = await db.PartySettlements
                .IgnoreQueryFilters()
                .Where(s => s.Id == settlement.Id || s.ReversalOfId == settlement.Id)
                .Select(s => s.SupplierPaymentId)
                .ToListAsync(cancellationToken);

            var stillPaid = await db.SupplierPurchasePayments
                .IgnoreQueryFilters()
                .Where(a => paymentIds.Contains(a.SupplierPaymentId))
                .GroupBy(a => a.SupplierPurchaseId)
                .Select(g => new { PurchaseId = g.Key, Amount = g.Sum(a => a.Amount), LastId = g.Max(a => a.Id) })
                .ToListAsync(cancellationToken);

            foreach (var row in stillPaid.Where(r => r.Amount > 0).OrderByDescending(r => r.LastId))
            {
                if (amount <= 0) break;
                var purchase = await db.SupplierPurchases
                    .IgnoreQueryFilters()
                    .FirstAsync(p => p.Id == row.PurchaseId, cancellationToken);
                var take = Math.Min(amount, Math.Min(row.Amount, purchase.PaidAmount));
                if (take <= 0) continue;

                purchase.ReversePayment(take);
                await db.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
                {
                    Amount = -take,
                    AllocationDate = date,
                    SupplierPurchase = purchase,
                    SupplierPayment = reversal,
                }, cancellationToken);
                amount -= take;
            }
        }

        private CustomerPayment NewCustomerPayment(Customer customer, Branch branch, decimal amount, DateTime date, string remarks)
        {
            var payment = new CustomerPayment
            {
                CustomerId = customer.Id,
                BranchId = branch.Id,
                Amount = amount,
                PaymentDate = date,
                PaymentMethod = PartySettlement.PaymentMethod,
                Remarks = remarks,
                Customer = customer,
                Branch = branch,
            };
            db.CustomerPayments.Add(payment);
            return payment;
        }

        private SupplierPayment NewSupplierPayment(Supplier supplier, Branch branch, decimal amount, DateTime date, string remarks)
        {
            var payment = new SupplierPayment
            {
                SupplierId = supplier.Id,
                BranchId = branch.Id,
                Amount = amount,
                PaymentDate = date,
                PaymentMethod = PartySettlement.PaymentMethod,
                Remarks = remarks,
                Supplier = supplier,
                Branch = branch,
            };
            db.SupplierPayments.Add(payment);
            return payment;
        }

        private async Task<PartySettlement> AddSettlementAsync(
            Customer customer, Supplier supplier, CustomerPayment customerPayment, SupplierPayment supplierPayment,
            decimal amount, DateTime date, CustomerSale? sourceSale, SupplierPurchase? sourcePurchase,
            PartySettlement? reversalOf, CancellationToken cancellationToken)
        {
            var settlement = new PartySettlement
            {
                CustomerId = customer.Id,
                SupplierId = supplier.Id,
                Amount = amount,
                SettlementDate = date,
                Customer = customer,
                Supplier = supplier,
                CustomerPayment = customerPayment,
                SupplierPayment = supplierPayment,
                SourceSale = sourceSale,
                SourcePurchase = sourcePurchase,
                ReversalOf = reversalOf,
            };
            await db.PartySettlements.AddAsync(settlement, cancellationToken);
            return settlement;
        }
    }
}
