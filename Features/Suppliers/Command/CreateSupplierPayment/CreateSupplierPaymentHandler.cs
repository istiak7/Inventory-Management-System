using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Suppliers.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LedgerExtensions;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

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
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Error",
                    Message = "Supplier not found."
                };

            var branch = await _dbContext.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
            if (branch == null)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Error",
                    Message = "Branch not found."
                };

            if (request.Amount < 0)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "Payment amount cannot be negative."
                };

            // Total discount waived across the chosen invoices. A payment may carry money, discount,
            // or both, but something must move.
            var totalDiscount = request.Allocations.Sum(a => a.Discount);
            if (request.Amount <= 0 && totalDiscount <= 0)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "Enter a payment amount or a discount."
                };

            // Lock the supplier: two payments (or a payment and a goods receipt) for the same
            // supplier now take turns, so the due check and the running balance are always current.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await _dbContext.LockRowAsync<Supplier>(request.SupplierId, cancellationToken);

            // What we owe the supplier in total: the ledger balance (opening balance + purchases - payments).
            var balance = await _dbContext.SupplierTransactions
                .Where(t => t.SupplierId == request.SupplierId)
                .GetLatestBalanceAsync(cancellationToken);
            // Payment and discount together settle part of the balance, so together they cannot exceed it.
            if (request.Amount + totalDiscount > balance)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"Payment and discount exceed what is owed to this supplier ({balance:0.00})."
                };

            foreach (var payment in request.Allocations)
            {
                var remainingDueForPurchase = await _dbContext.SupplierPurchases
                     .Where(p => p.Id == payment.PurchaseId && p.SupplierId == request.SupplierId && p.Status == PurchaseStatus.Approved)
                     .Select(p => p.DueAmount)
                     .FirstOrDefaultAsync(cancellationToken);

                if (remainingDueForPurchase - (payment.Amount + payment.Discount) < 0)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = $"Payment and discount for invoice {payment.PurchaseId} exceed its remaining due amount."
                    };
            }

            var hasAllocations = request.Allocations is { Count: > 0 };
            List<SupplierPurchase> targetedPurchases = [];
            if (hasAllocations)
            {
                if (request.Allocations.Any(a => a.Amount < 0 || a.Discount < 0))
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = "Allocation amounts and discounts cannot be negative."
                    };

                if (request.Allocations.Any(a => a.Amount + a.Discount <= 0))
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = "Each chosen invoice must have a payment or a discount greater than 0."
                    };

                if (request.Allocations.GroupBy(a => a.PurchaseId).Any(g => g.Count() > 1))
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = "Duplicate invoice in allocations."
                    };

                var allocationTotal = request.Allocations.Sum(a => a.Amount);
                if (allocationTotal > request.Amount)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = "Allocations exceed the payment amount."
                    };

                var ids = request.Allocations.Select(a => a.PurchaseId).ToList();
                targetedPurchases = await _dbContext.SupplierPurchases
                    .Where(p => ids.Contains(p.Id))
                    .ToListAsync(cancellationToken);

                foreach (var alloc in request.Allocations)
                {
                    var purchase = targetedPurchases.FirstOrDefault(p => p.Id == alloc.PurchaseId);
                    if (purchase == null || purchase.SupplierId != request.SupplierId)
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 404,
                            Status = "Error",
                            Message = $"Invoice {alloc.PurchaseId} not found for this supplier."
                        };
                    if (purchase.Status != PurchaseStatus.Approved)
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 400,
                            Status = "Error",
                            Message = $"Invoice {alloc.PurchaseId} is not approved yet and cannot be paid."
                        };
                    if (purchase.DueAmount <= 0)
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 400,
                            Status = "Error",
                            Message = $"Invoice {alloc.PurchaseId} has no outstanding due."
                        };
                    if (alloc.Amount + alloc.Discount > purchase.DueAmount)
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 400,
                            Status = "Error",
                            Message = $"Payment and discount for invoice {alloc.PurchaseId} exceed its due amount."
                        };
                }
            }

            try
            {
                var paymentDate = request.PaymentDate ?? DateTime.UtcNow;

                var payment = new SupplierPayment
                {
                    SupplierId = request.SupplierId,
                    BranchId = request.BranchId,
                    Amount = request.Amount,
                    PaymentDate = paymentDate,
                    PaymentMethod = request.PaymentMethod,
                    Remarks = request.Remarks,
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
                        // Discount first: it lowers the due, then the payment settles the rest.
                        if (alloc.Discount > 0) purchase.ApplyDiscount(alloc.Discount);
                        if (alloc.Amount > 0) purchase.ApplyPayment(alloc.Amount);

                        await _dbContext.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
                        {
                            Amount = alloc.Amount,
                            Discount = alloc.Discount,
                            AllocationDate = paymentDate,
                            SupplierPurchase = purchase,
                            SupplierPayment = payment,
                        }, cancellationToken);
                    }
                    allocatedAmount = request.Allocations.Sum(a => a.Amount);
                }

                // Any part of the payment not given to a chosen invoice pays the oldest open
                // approved invoices. This keeps the invoice dues equal to the ledger balance,
                // so the same due can never be paid twice.
                var unallocated = request.Amount - allocatedAmount;
                if (unallocated > 0)
                {
                    // All branches: the supplier's account is one account.
                    // What is left after all orders are paid settles the opening balance.
                    var openPurchases = await _dbContext.SupplierPurchases
                        .IgnoreQueryFilters()
                        .Where(p => p.SupplierId == request.SupplierId
                                    && p.IsActive != (int)EntityStatus.Deleted
                                    && p.Status == PurchaseStatus.Approved
                                    && p.DueAmount > 0)
                        .OrderBy(p => p.PurchaseDate).ThenBy(p => p.Id)
                        .ToListAsync(cancellationToken);

                    foreach (var purchase in openPurchases)
                    {
                        if (unallocated <= 0) break;

                        // DueAmount is the in-memory value, which already includes the
                        // chosen allocations above.
                        var amount = Math.Min(unallocated, purchase.DueAmount);
                        if (amount <= 0) continue;

                        purchase.ApplyPayment(amount);

                        // Top up the allocation made above for this invoice, if there is one.
                        var existing = _dbContext.SupplierPurchasePayments.Local
                            .FirstOrDefault(a => a.SupplierPurchase == purchase && a.SupplierPayment == payment);
                        if (existing != null)
                            existing.Amount += amount;
                        else
                            await _dbContext.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
                            {
                                Amount = amount,
                                AllocationDate = paymentDate,
                                SupplierPurchase = purchase,
                                SupplierPayment = payment,
                            }, cancellationToken);

                        unallocated -= amount;
                        allocatedAmount += amount;
                    }
                }

                var startBalance = await _dbContext.SupplierTransactions
                    .Where(t => t.SupplierId == request.SupplierId)
                    .GetLatestBalanceAsync(cancellationToken);

                // A discount lowers the payable just like a payment, so it gets its own ledger row
                // first; the payment row follows and carries the final running balance.
                var balanceAfterDiscount = startBalance - totalDiscount;
                if (totalDiscount > 0)
                    await _dbContext.SupplierTransactions.AddAsync(
                        SupplierTransaction.ForDiscount(payment, supplier, totalDiscount, balanceAfterDiscount),
                        cancellationToken);

                var runningBalance = balanceAfterDiscount - request.Amount;
                var supplierTransaction = SupplierTransaction.ForPayment(payment, supplier, runningBalance);
                await _dbContext.SupplierTransactions.AddAsync(supplierTransaction, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new SupplierPaymentResponse(
                    payment.Id, payment.SupplierId, payment.Amount, payment.PaymentDate,
                    payment.PaymentMethod, payment.Remarks, allocatedAmount, runningBalance);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "Payment recorded successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error recording supplier payment");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while recording the payment."
                };
            }
        }
    }
}
