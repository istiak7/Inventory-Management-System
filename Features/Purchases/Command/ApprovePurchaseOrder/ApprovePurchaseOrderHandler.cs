using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Purchases.Command.ApprovePurchaseOrder
{

    public class ApprovePurchaseOrderHandler(
            AppDbContext _dbContext,
            ILogger<ApprovePurchaseOrderHandler> _logger
        ) : IRequestHandler<ApprovePurchaseOrderCommand, Result>
    {

        public async Task<Result> Handle(ApprovePurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            var purchase = await _dbContext.SupplierPurchases
                .Include(p => p.Supplier)
                .Include(p => p.Branch)
                .Include(p => p.SupplierPurchaseDetails)
                .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId, cancellationToken);

            if (purchase == null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Purchase order not found." };

            if (purchase.Status != "Pending")
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Only a pending purchase order can be approved." };

            var supplier = purchase.Supplier;
            var branch = purchase.Branch;

            var totalAmount = purchase.TotalAmount;

            // requested amount, validated (not silently clamped)
            var paidAmount = request.Payment?.Amount ?? 0;
            if (paidAmount < 0 || paidAmount > totalAmount)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Payment amount cannot be negative or exceed the purchase total." };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Full due -> nothing paid, no payment row. Full payment -> paid == total.
                // Partial -> 0 < paid < total. PurchaseType drives the three UI options.
                purchase.PurchaseType = paidAmount <= 0 ? "due"
                                      : paidAmount >= totalAmount ? "fillpayment"
                                      : "partial";
                purchase.Status = paidAmount <= 0 ? "Due"
                                : paidAmount >= totalAmount ? "Paid"
                                : "Partial";

                // Approve every line; the money is settled on the header, not per line.
                foreach (var detail in purchase.SupplierPurchaseDetails)
                    detail.IsApproved = "Approved";

                purchase.PaidAmount = paidAmount;
                purchase.DueAmount = totalAmount - paidAmount;   // outstanding on THIS invoice
                var totalDueAmount = purchase.DueAmount;

                // Ledger: start from the supplier's current overall balance (their total due so far),
                // then the purchase debits it (we now owe more). BalanceAfter tracks the supplier total,
                // not this one invoice.
                var runningBalance = await GetCurrentSupplierBalanceAsync(purchase.SupplierId, cancellationToken);
                runningBalance += totalAmount;

                await _dbContext.SupplierTransactions.AddAsync(new SupplierTransaction
                {
                    SupplierId = purchase.SupplierId,
                    TransactionType = "Purchase",
                    TransactionDate = purchase.PurchaseDate,
                    Debit = totalAmount,
                    Credit = 0,
                    BalanceAfter = runningBalance,
                    SupplierPurchase = purchase,
                    Supplier = supplier,
                }, cancellationToken);

                SupplierPayment? payment = null;
                if (paidAmount > 0)
                {
                    payment = new SupplierPayment
                    {
                        SupplierId = purchase.SupplierId,
                        BranchId = purchase.BranchId,
                        Amount = paidAmount,
                        PaymentDate = request.Payment?.PaymentDate ?? DateTime.Now,
                        PaymentMethod = request.Payment?.PaymentMethod ?? "Cash",
                        Supplier = supplier,
                        Branch = branch,
                    };
                    await _dbContext.SupplierPayments.AddAsync(payment, cancellationToken);

                    // Manual pairing only — this payment is allocated to this purchase alone (no FIFO/auto-allocation).
                    await _dbContext.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
                    {
                        Amount = paidAmount,
                        AllocationDate = payment.PaymentDate,
                        SupplierPurchase = purchase,
                        SupplierPayment = payment,
                    }, cancellationToken);

                    // Ledger: the payment credits the supplier account (we now owe less).
                    runningBalance -= paidAmount;
                    await _dbContext.SupplierTransactions.AddAsync(new SupplierTransaction
                    {
                        SupplierId = purchase.SupplierId,
                        TransactionType = "Payment",
                        TransactionDate = payment.PaymentDate,
                        Debit = 0,
                        Credit = paidAmount,
                        BalanceAfter = runningBalance,
                        SupplierPayment = payment,
                        Supplier = supplier,
                    }, cancellationToken);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new PurchaseOrderResponse(
                    purchase.Id, purchase.SupplierId, purchase.BranchId, purchase.PurchaseDate,
                    purchase.InvoiceNumber, purchase.Status, purchase.PurchaseType,
                    totalAmount, totalDueAmount,
                    payment == null ? null : new PaymentResponse(payment.Id, payment.Amount, payment.PaymentDate, payment.PaymentMethod)
                );

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Purchase order approved successfully", Data = response };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error approving purchase order");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while approving the purchase order." };
            }
        }

        /// <summary>
        /// The supplier's current overall account balance = the <c>BalanceAfter</c> of their most recent
        /// transaction (0 if they have no prior ledger entries). This is the supplier's total outstanding
        /// due, and the starting point onto which this approval's Debit/Credit are applied.
        /// </summary>
        private async Task<decimal> GetCurrentSupplierBalanceAsync(int supplierId, CancellationToken cancellationToken)
        {
            return await _dbContext.SupplierTransactions
                .AsNoTracking()
                .Where(t => t.SupplierId == supplierId)
                .OrderByDescending(t => t.Id)
                .Select(t => t.BalanceAfter)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
