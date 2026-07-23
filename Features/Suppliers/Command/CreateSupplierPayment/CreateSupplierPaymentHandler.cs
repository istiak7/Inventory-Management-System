using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Suppliers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Suppliers.Command.CreateSupplierPayment
{
    public class CreateSupplierPaymentHandler(
            AppDbContext _dbContext,
            ILogger<CreateSupplierPaymentHandler> _logger
        ) : IRequestHandler<CreateSupplierPaymentCommand, Result>
    {
        public async Task<Result> Handle(CreateSupplierPaymentCommand request, CancellationToken cancellationToken)
        {
            var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);
            if (supplier == null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Supplier not found." };

            var branch = await _dbContext.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
            if (branch == null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Branch not found." };

            if (request.Amount <= 0)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Payment amount must be greater than 0." };

            // Validate explicit allocations up front (before opening a transaction).
            var hasAllocations = request.Allocations is { Count: > 0 };
            List<SupplierPurchase> targetedPurchases = [];
            if (hasAllocations)
            {
                if (request.Allocations.Any(a => a.Amount <= 0))
                    return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Each allocation amount must be greater than 0." };

                if (request.Allocations.GroupBy(a => a.PurchaseId).Any(g => g.Count() > 1))
                    return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Duplicate invoice in allocations." };

                var allocationTotal = request.Allocations.Sum(a => a.Amount);
                if (allocationTotal > request.Amount)
                    return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Allocations exceed the payment amount." };

                var ids = request.Allocations.Select(a => a.PurchaseId).ToList();
                targetedPurchases = await _dbContext.SupplierPurchases
                    .Where(p => ids.Contains(p.Id))
                    .ToListAsync(cancellationToken);

                foreach (var alloc in request.Allocations)
                {
                    var purchase = targetedPurchases.FirstOrDefault(p => p.Id == alloc.PurchaseId);
                    if (purchase == null || purchase.SupplierId != request.SupplierId)
                        return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = $"Invoice {alloc.PurchaseId} not found for this supplier." };
                    // Only approved-with-due invoices are payable (not Pending/Rejected/Paid).
                    if (purchase.Status != "Due" && purchase.Status != "Partial")
                        return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Invoice {alloc.PurchaseId} is not open for payment." };
                    if (alloc.Amount > purchase.DueAmount)
                        return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Allocation for invoice {alloc.PurchaseId} exceeds its due amount." };
                }
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var paymentDate = request.PaymentDate ?? DateTime.Now;

                var payment = new SupplierPayment
                {
                    SupplierId = request.SupplierId,
                    BranchId = request.BranchId,
                    Amount = request.Amount,
                    PaymentDate = paymentDate,
                    PaymentMethod = request.PaymentMethod,
                    Supplier = supplier,
                    Branch = branch,
                };
                await _dbContext.SupplierPayments.AddAsync(payment, cancellationToken);

                decimal allocatedAmount;

                if (hasAllocations)
                {
                    // Targeted: apply each allocation to the invoice the caller chose.
                    foreach (var alloc in request.Allocations)
                    {
                        var purchase = targetedPurchases.First(p => p.Id == alloc.PurchaseId);
                        purchase.PaidAmount += alloc.Amount;
                        purchase.DueAmount -= alloc.Amount;
                        purchase.Status = purchase.DueAmount <= 0 ? "Paid" : "Partial";
                        purchase.PurchaseType = purchase.DueAmount <= 0 ? "fillpayment" : "partial";

                        await _dbContext.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
                        {
                            Amount = alloc.Amount,
                            AllocationDate = paymentDate,
                            SupplierPurchase = purchase,
                            SupplierPayment = payment,
                        }, cancellationToken);
                    }
                    allocatedAmount = request.Allocations.Sum(a => a.Amount);
                }
                else
                {
                    // FIFO: apply the payment against the supplier's open invoices, oldest first.
                    // Only approved-with-due invoices qualify (not Pending/Rejected/Paid).
                    var outstanding = await _dbContext.SupplierPurchases
                        .Where(p => p.SupplierId == request.SupplierId && p.DueAmount > 0
                                    && (p.Status == "Due" || p.Status == "Partial"))
                        .OrderBy(p => p.PurchaseDate)
                        .ThenBy(p => p.Id)
                        .ToListAsync(cancellationToken);

                    decimal remaining = request.Amount;
                    foreach (var purchase in outstanding)
                    {
                        if (remaining <= 0) break;

                        var applied = Math.Min(remaining, purchase.DueAmount);
                        purchase.PaidAmount += applied;
                        purchase.DueAmount -= applied;
                        purchase.Status = purchase.DueAmount <= 0 ? "Paid" : "Partial";
                        purchase.PurchaseType = purchase.DueAmount <= 0 ? "fillpayment" : "partial";
                        remaining -= applied;

                        await _dbContext.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
                        {
                            Amount = applied,
                            AllocationDate = paymentDate,
                            SupplierPurchase = purchase,
                            SupplierPayment = payment,
                        }, cancellationToken);
                    }
                    allocatedAmount = request.Amount - remaining;
                }

                // Ledger: the payment credits the supplier account (we now owe less).
                var runningBalance = await GetCurrentSupplierBalanceAsync(request.SupplierId, cancellationToken);
                runningBalance -= request.Amount;

                await _dbContext.SupplierTransactions.AddAsync(new SupplierTransaction
                {
                    SupplierId = request.SupplierId,
                    TransactionType = "Payment",
                    TransactionDate = paymentDate,
                    Debit = 0,
                    Credit = request.Amount,
                    BalanceAfter = runningBalance,
                    SupplierPayment = payment,
                    Supplier = supplier,
                }, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new SupplierPaymentResponse(
                    payment.Id, payment.SupplierId, payment.Amount, payment.PaymentDate,
                    payment.PaymentMethod, allocatedAmount, runningBalance);

                return new Result { IsSuccess = true, StatusCode = 201, Status = "Success", Message = "Payment recorded successfully", Data = response };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error recording supplier payment");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while recording the payment." };
            }
        }

        /// <summary>
        /// The supplier's current overall balance = the BalanceAfter of their most recent transaction
        /// (0 if none). This is the starting point onto which this payment's credit is applied.
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
