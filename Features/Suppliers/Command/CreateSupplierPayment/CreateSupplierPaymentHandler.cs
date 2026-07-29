using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
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


            var remainingDue = await _dbContext.SupplierPurchases
                .Where(p => p.SupplierId == request.SupplierId && p.Status == PurchaseStatus.Approved)
                .SumAsync(p => p.DueAmount, cancellationToken);
            if(remainingDue - request.Amount < 0)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Payment amount exceeds the remaining due amount." };

            // Allocation is ALWAYS explicit — no auto FIFO. Any unallocated remainder stays as
            // on-account credit (an advance), which is allowed.
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
                    // Only an approved (fully received) purchase is payable — pending, partially
                    // received, or rejected orders must not generate payment records.
                    if (purchase.Status != PurchaseStatus.Approved)
                        return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Invoice {alloc.PurchaseId} is not approved yet and cannot be paid." };
                    if (purchase.DueAmount <= 0)
                        return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Invoice {alloc.PurchaseId} has no outstanding due." };
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

                decimal allocatedAmount = 0;
                if (hasAllocations)
                {
                    foreach (var alloc in request.Allocations)
                    {
                        var purchase = targetedPurchases.First(p => p.Id == alloc.PurchaseId);
                        purchase.ApplyPayment(alloc.Amount);   // keeps Paid/Due consistent, validates against due

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

                // Ledger: the payment credits the supplier account (we now owe less), regardless of
                // how much was allocated — the unallocated part is on-account credit.
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
