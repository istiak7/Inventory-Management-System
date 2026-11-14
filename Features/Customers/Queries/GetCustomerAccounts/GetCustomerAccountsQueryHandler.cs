using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Customers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerAccounts
{
    public class GetCustomerAccountsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetCustomerAccountsQueryHandler> _logger
    ) : IRequestHandler<GetCustomerAccountsQuery, Result>
    {
        public async Task<Result> Handle(GetCustomerAccountsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.Customers
                    .AsNoTracking()
                    .Where(c => c.IsActive != (int)EntityStatus.Deleted);

                if (request.CustomerId is int customerId)
                    query = query.Where(c => c.Id == customerId);

                var rows = await query
                    .OrderBy(c => c.Name)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Group,
                        // A voided sale takes itself back off sales, and its refund off payments.
                        // Returns come off sales too (a cancelled return, booked as Credit, adds back).
                        TotalSales = (c.CustomerTransactions.Where(t => t.TransactionType == "Sale").Sum(t => (decimal?)t.Credit) ?? 0m)
                            - (c.CustomerTransactions.Where(t => t.TransactionType == "Void").Sum(t => (decimal?)t.Debit) ?? 0m)
                            - (c.CustomerTransactions.Where(t => t.TransactionType == "Return").Sum(t => (decimal?)(t.Debit - t.Credit)) ?? 0m)
                            + (c.CustomerTransactions.Where(t => t.TransactionType == "SaleEdit").Sum(t => (decimal?)(t.Credit - t.Debit)) ?? 0m),
                        // Settled = paid: cash payments plus cross-ledger adjustments (net of reversals).
                        TotalPayments = (c.CustomerTransactions.Where(t => t.TransactionType == "Payment" || t.TransactionType == "Adjustment").Sum(t => (decimal?)t.Debit) ?? 0m)
                            - (c.CustomerTransactions.Where(t => t.TransactionType == "Refund" || t.TransactionType == "Adjustment").Sum(t => (decimal?)t.Credit) ?? 0m),
                        Receivable = (c.CustomerTransactions.Sum(t => (decimal?)t.Credit) ?? 0m)
                            - (c.CustomerTransactions.Sum(t => (decimal?)t.Debit) ?? 0m),
                        Payable = c.Supplier == null
                            ? 0m
                            : (c.Supplier.SupplierTransactions.Sum(t => (decimal?)t.Debit) ?? 0m)
                                - (c.Supplier.SupplierTransactions.Sum(t => (decimal?)t.Credit) ?? 0m),
                        LastTransactionDate = c.CustomerTransactions
                            .OrderByDescending(t => t.Id)
                            .Select(t => (DateTime?)t.TransactionDate)
                            .FirstOrDefault(),
                        c.PhoneNumber,
                        c.Email,
                        c.Address,
                        c.OpeningBalance,
                        c.SupplierId,
                    })
                    .ToListAsync(cancellationToken);

                var accounts = rows.Select(r => new CustomerAccountResponse(
                    r.Id, r.Name, r.Group, r.TotalSales, r.TotalPayments, r.Receivable, r.LastTransactionDate,
                    r.PhoneNumber, r.Email, r.Address, r.OpeningBalance,
                    r.Receivable, r.Payable, r.Receivable - r.Payable, r.SupplierId, r.SupplierId != null))
                    .ToList();

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Customer accounts retrieved successfully",
                    Data = accounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer accounts");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving customer accounts."
                };
            }
        }
    }
}
