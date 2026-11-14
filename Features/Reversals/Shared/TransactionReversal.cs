using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Parties.Shared;
using Inventory_Management_System.Features.Returns.Shared;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Reversals.Shared
{
    // The building blocks of cancelling a whole invoice (CancelSale / CancelPurchaseOrder), kept
    // out of the two handlers so both reverse money and stock the same way:
    //
    //   * stock never has its balance overwritten: every change is an "Adjustment" inventory row
    //     that carries the new running balance;
    //   * cash payments are never deleted or edited. A payment's share on the invoice is released
    //     with a NEGATIVE allocation (newest payment first); the payment itself stays, so the money
    //     remains the party's advance until it is really refunded;
    //   * a real cash refund is a negative "Refund" payment and only exists when the user says
    //     the money was handed back;
    //   * cross-ledger FIFO settlement is undone by CrossLedgerSettlement.ReverseOn*Async (exact
    //     allocations, reversal rows linked to the originals) - callers only invoke it for Credit
    //     documents.
    //
    // Call inside the caller's transaction, after the locks (document -> stock -> customer -> supplier).
    public class TransactionReversal(AppDbContext db)
    {
        // Payment methods that are not cash: settlements (Adjustment) and refunds (negative rows).
        private const string RefundMethod = "Refund";

        // Moves stock by `delta` (+ in, - out) as one traceable Adjustment row and returns the
        // new balance. The caller checks that an outgoing move is covered by stock on hand.
        public async Task<int> MoveStockAsync(
            Dictionary<int, Stock> stocks, Branch branch, ProductVariant variant, int delta,
            SupplierPurchaseDetails? detail, DateTime date, CancellationToken cancellationToken)
        {
            var stock = await ReturnMath.GetOrCreateStockAsync(db, stocks, branch, variant, cancellationToken);
            stock.CurrentStock += delta;
            await db.InventoryTransactions.AddAsync(new InventoryTransaction
            {
                BranchId = branch.Id,
                ProductVariantId = variant.Id,
                SupplierPurchaseDetails = detail,
                TransactionType = InventoryTxnType.Adjustment,
                QuantityIn = delta > 0 ? delta : 0,
                QuantityOut = delta < 0 ? -delta : 0,
                BalanceAfter = stock.CurrentStock,
                TransactionDate = date,
                Branch = branch,
                ProductVariant = variant,
            }, cancellationToken);
            return stock.CurrentStock;
        }

        // Releases `amount` of the cash paid on a sale from the sale (newest payment first) and
        // returns what was released. The payments stay; the released money is the customer's advance.
        public async Task<decimal> ReleaseSaleCashAsync(
            CustomerSale sale, decimal amount, DateTime date, CancellationToken cancellationToken)
        {
            if (amount <= 0) return 0m;

            var net = (await db.SaleCustomerPayments.IgnoreQueryFilters()
                    .Where(a => a.SaleId == sale.Id
                                && a.CustomerPayment.PaymentMethod != PartySettlement.PaymentMethod
                                && a.CustomerPayment.PaymentMethod != RefundMethod)
                    .GroupBy(a => a.CustomerPaymentId)
                    .Select(g => new { PaymentId = g.Key, Amount = g.Sum(a => a.Amount) })
                    .ToListAsync(cancellationToken))
                .Where(x => x.Amount > 0)
                .OrderByDescending(x => x.PaymentId)
                .ToList();

            var left = amount;
            foreach (var p in net)
            {
                if (left <= 0) break;
                var take = Math.Min(p.Amount, left);
                var payment = await db.CustomerPayments.IgnoreQueryFilters()
                    .FirstAsync(x => x.Id == p.PaymentId, cancellationToken);
                await db.SaleCustomerPayments.AddAsync(new SaleCustomerPayment
                {
                    Amount = -take,
                    AllocationDate = date,
                    CustomerSale = sale,
                    CustomerPayment = payment,
                }, cancellationToken);
                left -= take;
            }
            return amount - left;
        }

        // The same for a purchase bill and the payments we made to the supplier.
        public async Task<decimal> ReleasePurchaseCashAsync(
            SupplierPurchase purchase, decimal amount, DateTime date, CancellationToken cancellationToken)
        {
            if (amount <= 0) return 0m;

            var net = (await db.SupplierPurchasePayments.IgnoreQueryFilters()
                    .Where(a => a.SupplierPurchaseId == purchase.Id
                                && a.SupplierPayment.PaymentMethod != PartySettlement.PaymentMethod
                                && a.SupplierPayment.PaymentMethod != RefundMethod)
                    .GroupBy(a => a.SupplierPaymentId)
                    .Select(g => new { PaymentId = g.Key, Amount = g.Sum(a => a.Amount) })
                    .ToListAsync(cancellationToken))
                .Where(x => x.Amount > 0)
                .OrderByDescending(x => x.PaymentId)
                .ToList();

            var left = amount;
            foreach (var p in net)
            {
                if (left <= 0) break;
                var take = Math.Min(p.Amount, left);
                var payment = await db.SupplierPayments.IgnoreQueryFilters()
                    .FirstAsync(x => x.Id == p.PaymentId, cancellationToken);
                await db.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
                {
                    Amount = -take,
                    AllocationDate = date,
                    SupplierPurchase = purchase,
                    SupplierPayment = payment,
                }, cancellationToken);
                left -= take;
            }
            return amount - left;
        }

        // Serials that a sale, a sales return, a warranty claim or a purchase return has touched:
        // they carry history (rows point at them) and must not disappear from the record.
        public async Task<HashSet<int>> SerialsWithHistoryAsync(IReadOnlyCollection<int> serialIds, CancellationToken cancellationToken)
        {
            var used = new HashSet<int>();
            if (serialIds.Count == 0) return used;
            var ids = serialIds.ToList();

            used.UnionWith(await db.SaleDetails.IgnoreQueryFilters()
                .Where(d => d.ProductSerialId != null && ids.Contains(d.ProductSerialId.Value))
                .Select(d => d.ProductSerialId!.Value).ToListAsync(cancellationToken));
            used.UnionWith(await db.SalesReturnLines.IgnoreQueryFilters()
                .Where(l => l.ProductSerialId != null && ids.Contains(l.ProductSerialId.Value))
                .Select(l => l.ProductSerialId!.Value).ToListAsync(cancellationToken));
            used.UnionWith(await db.WarrantyClaims.IgnoreQueryFilters()
                .Where(c => ids.Contains(c.ProductSerialId))
                .Select(c => c.ProductSerialId).ToListAsync(cancellationToken));
            used.UnionWith(await db.PurchaseReturnSerials.IgnoreQueryFilters()
                .Where(r => ids.Contains(r.ProductSerialId))
                .Select(r => r.ProductSerialId).ToListAsync(cancellationToken));
            return used;
        }

        // What a cancellation wrote, for its audit row: the ids of every new settlement reversal,
        // payment allocation (release / refund), inventory movement and ledger row. Call with the
        // entities that were Added BEFORE the save, and read this AFTER it, so the ids exist.
        public static object DescribeAdded(IReadOnlyCollection<object> added) => new
        {
            settlementReversals = added.OfType<PartySettlement>()
                .Select(x => new { x.Id, reversalOfId = x.ReversalOf?.Id ?? x.ReversalOfId, x.Amount }).ToList(),
            saleAllocations = added.OfType<SaleCustomerPayment>()
                .Select(x => new { x.Id, paymentId = x.CustomerPayment?.Id ?? x.CustomerPaymentId, x.Amount }).ToList(),
            purchaseAllocations = added.OfType<SupplierPurchasePayment>()
                .Select(x => new { x.Id, paymentId = x.SupplierPayment?.Id ?? x.SupplierPaymentId, x.Amount }).ToList(),
            refundPayments = added.OfType<CustomerPayment>().Where(x => x.PaymentMethod == RefundMethod).Select(x => x.Id)
                .Concat(added.OfType<SupplierPayment>().Where(x => x.PaymentMethod == RefundMethod).Select(x => x.Id)).ToList(),
            inventoryMovements = added.OfType<InventoryTransaction>().Select(x => x.Id).ToList(),
            ledgerRows = added.OfType<CustomerTransaction>().Select(x => new { x.Id, x.TransactionType, x.Debit, x.Credit })
                .Concat(added.OfType<SupplierTransaction>().Select(x => new { x.Id, x.TransactionType, x.Debit, x.Credit })).ToList(),
        };

        // Cash handed back to the customer for a cancelled sale (only on the user's say-so).
        // Returns the customer's new running balance.
        public async Task<decimal> RefundCustomerAsync(
            CustomerSale sale, Customer customer, decimal amount, decimal balance, DateTime date,
            CancellationToken cancellationToken)
        {
            var refund = new CustomerPayment
            {
                CustomerId = customer.Id,
                BranchId = sale.BranchId,
                Amount = -amount,
                PaymentDate = date,
                PaymentMethod = RefundMethod,
                Remarks = $"Refund for cancelled invoice {sale.InvoiceNumber}",
                Customer = customer,
                Branch = sale.Branch,
            };
            await db.CustomerPayments.AddAsync(refund, cancellationToken);
            await db.SaleCustomerPayments.AddAsync(new SaleCustomerPayment
            {
                Amount = -amount,
                AllocationDate = date,
                CustomerSale = sale,
                CustomerPayment = refund,
            }, cancellationToken);
            balance += amount;
            await db.CustomerTransactions.AddAsync(
                CustomerTransaction.ForRefund(refund, customer, amount, balance, sale), cancellationToken);
            return balance;
        }

        // Cash the supplier paid back to us for a cancelled bill. Returns the supplier's new balance.
        public async Task<decimal> RefundFromSupplierAsync(
            SupplierPurchase purchase, Supplier supplier, decimal amount, decimal balance, DateTime date,
            CancellationToken cancellationToken)
        {
            var refund = new SupplierPayment
            {
                SupplierId = supplier.Id,
                BranchId = purchase.BranchId,
                Amount = -amount,
                PaymentDate = date,
                PaymentMethod = RefundMethod,
                Remarks = $"Refund from supplier for cancelled bill {purchase.InvoiceNumber}",
                Supplier = supplier,
                Branch = purchase.Branch,
            };
            await db.SupplierPayments.AddAsync(refund, cancellationToken);
            await db.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
            {
                Amount = -amount,
                AllocationDate = date,
                SupplierPurchase = purchase,
                SupplierPayment = refund,
            }, cancellationToken);
            balance += amount;
            await db.SupplierTransactions.AddAsync(
                SupplierTransaction.ForEditRefund(refund, supplier, amount, balance, purchase), cancellationToken);
            return balance;
        }
    }
}
