using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Customers.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LedgerExtensions;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Customers.Command.CreateCustomerPayment
{
    public class CreateCustomerPaymentHandler(
        AppDbContext _dbContext,
        ILogger<CreateCustomerPaymentHandler> _logger
    ) : IRequestHandler<CreateCustomerPaymentCommand, Result>
    {
        public async Task<Result> Handle(
            CreateCustomerPaymentCommand request,
            CancellationToken cancellationToken)
        {
            var customer = await _dbContext.Customers.FirstOrDefaultAsync(
                c => c.Id == request.CustomerId,
                cancellationToken);

            if (customer == null)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Error",
                    Message = "Customer not found."
                };

            var branch = await _dbContext.Branches.FirstOrDefaultAsync(
                b => b.Id == request.BranchId,
                cancellationToken);

            if (branch == null)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Error",
                    Message = "Branch not found."
                };

            if (request.Amount <= 0)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "Payment amount must be greater than 0."
                };

            // Lock the customer: two payments (or a payment and a sale) for the same customer
            // now take turns, so the due check and the running balance below are always current.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await _dbContext.LockRowAsync<Customer>(request.CustomerId, cancellationToken);

            var remainingDue = await _dbContext.CustomerSales
                .Where(s => s.CustomerId == request.CustomerId)
                .SumAsync(s => s.DueAmount, cancellationToken);

            if (remainingDue - request.Amount < 0)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "Payment amount exceeds the remaining due amount."
                };

            foreach (var payment in request.Allocations)
            {
                var remainingDueForSale = await _dbContext.CustomerSales
                     .Where(s => s.Id == payment.SaleId && s.CustomerId == request.CustomerId)
                     .Select(s => s.DueAmount)
                     .FirstOrDefaultAsync(cancellationToken);

                if (remainingDueForSale - payment.Amount < 0)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = $"Allocation amount for invoice {payment.SaleId} exceeds its remaining due amount."
                    };
            }

            var hasAllocations = request.Allocations is { Count: > 0 };
            List<CustomerSale> targetedSales = [];
            if (hasAllocations)
            {
                if (request.Allocations.Any(a => a.Amount <= 0))
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = "Each allocation amount must be greater than 0."
                    };

                if (request.Allocations.GroupBy(a => a.SaleId).Any(g => g.Count() > 1))
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

                var ids = request.Allocations.Select(a => a.SaleId).ToList();
                targetedSales = await _dbContext.CustomerSales
                    .Where(s => ids.Contains(s.Id))
                    .ToListAsync(cancellationToken);

                foreach (var alloc in request.Allocations)
                {
                    var sale = targetedSales.FirstOrDefault(s => s.Id == alloc.SaleId);
                    if (sale == null || sale.CustomerId != request.CustomerId)
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 404,
                            Status = "Error",
                            Message = $"Invoice {alloc.SaleId} not found for this customer."
                        };
                    if (sale.DueAmount <= 0)
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 400,
                            Status = "Error",
                            Message = $"Invoice {alloc.SaleId} has no outstanding due."
                        };
                    if (alloc.Amount > sale.DueAmount)
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 400,
                            Status = "Error",
                            Message = $"Allocation for invoice {alloc.SaleId} exceeds its due amount."
                        };
                }
            }

            try
            {
                var paymentDate = request.PaymentDate ?? DateTime.Now;

                var payment = new CustomerPayment
                {
                    CustomerId = request.CustomerId,
                    BranchId = request.BranchId,
                    Amount = request.Amount,
                    PaymentDate = paymentDate,
                    PaymentMethod = request.PaymentMethod,
                    Remarks = request.Remarks,
                    Customer = customer,
                    Branch = branch,
                };

                await _dbContext.CustomerPayments.AddAsync(
                    payment,
                    cancellationToken);

                decimal allocatedAmount = 0;
                if (hasAllocations)
                {
                    foreach (var alloc in request.Allocations)
                    {
                        var sale = targetedSales.First(s => s.Id == alloc.SaleId);
                        sale.ApplyPayment(alloc.Amount);

                        await _dbContext.SaleCustomerPayments.AddAsync(new SaleCustomerPayment
                        {
                            Amount = alloc.Amount,
                            AllocationDate = paymentDate,
                            CustomerSale = sale,
                            CustomerPayment = payment,
                        }, cancellationToken);
                    }
                    allocatedAmount = request.Allocations.Sum(a => a.Amount);
                }

                // Any part of the payment not given to a chosen invoice pays the oldest open
                // invoices. This keeps the invoice dues equal to the ledger balance, so the same
                // due can never be paid twice.
                var unallocated = request.Amount - allocatedAmount;
                if (unallocated > 0)
                {
                    var openSales = await _dbContext.CustomerSales
                        .Where(s => s.CustomerId == request.CustomerId && s.DueAmount > 0)
                        .OrderBy(s => s.SaleDate).ThenBy(s => s.Id)
                        .ToListAsync(cancellationToken);

                    foreach (var sale in openSales)
                    {
                        if (unallocated <= 0) break;

                        // DueAmount is the in-memory value, which already includes the
                        // chosen allocations above.
                        var amount = Math.Min(unallocated, sale.DueAmount);
                        if (amount <= 0) continue;

                        sale.ApplyPayment(amount);

                        // Top up the allocation made above for this invoice, if there is one.
                        var existing = _dbContext.SaleCustomerPayments.Local
                            .FirstOrDefault(a => a.CustomerSale == sale && a.CustomerPayment == payment);
                        if (existing != null)
                            existing.Amount += amount;
                        else
                            await _dbContext.SaleCustomerPayments.AddAsync(new SaleCustomerPayment
                            {
                                Amount = amount,
                                AllocationDate = paymentDate,
                                CustomerSale = sale,
                                CustomerPayment = payment,
                            }, cancellationToken);

                        unallocated -= amount;
                        allocatedAmount += amount;
                    }
                }

                var runningBalance = await _dbContext.CustomerTransactions
                    .Where(t => t.CustomerId == request.CustomerId)
                    .GetLatestBalanceAsync(cancellationToken);

                runningBalance -= request.Amount;

                var customerTransaction = CustomerTransaction.ForPayment(
                    payment,
                    customer,
                    runningBalance);

                await _dbContext.CustomerTransactions.AddAsync(
                    customerTransaction,
                    cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new CustomerPaymentResponse(
                    payment.Id, payment.CustomerId, payment.Amount, payment.PaymentDate,
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
                _logger.LogError(ex, "Error recording customer payment");
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
