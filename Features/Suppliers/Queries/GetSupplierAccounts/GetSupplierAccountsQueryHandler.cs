using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Suppliers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierAccounts
{
    public class GetSupplierAccountsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetSupplierAccountsQueryHandler> _logger
    ) : IRequestHandler<GetSupplierAccountsQuery, Result>
    {
        public async Task<Result> Handle(GetSupplierAccountsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.Suppliers
                    .AsNoTracking()
                    .Where(s => s.IsActive != (int)EntityStatus.Deleted);

                if (request.SupplierId is int supplierId)
                    query = query.Where(s => s.Id == supplierId);

                var rows = await query
                    .OrderBy(s => s.Name)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Group,
                        // Returns come off purchases (a cancelled return, booked as Debit, adds back).
                        TotalPurchases = (s.SupplierTransactions.Where(t => t.TransactionType == "Purchase").Sum(t => (decimal?)t.Debit) ?? 0m)
                            - (s.SupplierTransactions.Where(t => t.TransactionType == "Return").Sum(t => (decimal?)(t.Credit - t.Debit)) ?? 0m),
                        // Settled = paid: cash payments plus cross-ledger adjustments (net of reversals),
                        // less money the supplier refunded on returns.
                        TotalPayments = (s.SupplierTransactions.Where(t => t.TransactionType == "Payment" || t.TransactionType == "Adjustment").Sum(t => (decimal?)t.Credit) ?? 0m)
                            - (s.SupplierTransactions.Where(t => t.TransactionType == "Adjustment" || t.TransactionType == "Refund").Sum(t => (decimal?)t.Debit) ?? 0m),
                        Payable = (s.SupplierTransactions.Sum(t => (decimal?)t.Debit) ?? 0m)
                            - (s.SupplierTransactions.Sum(t => (decimal?)t.Credit) ?? 0m),
                        Receivable = s.Customer == null
                            ? 0m
                            : (s.Customer.CustomerTransactions.Sum(t => (decimal?)t.Credit) ?? 0m)
                                - (s.Customer.CustomerTransactions.Sum(t => (decimal?)t.Debit) ?? 0m),
                        LastTransactionDate = s.SupplierTransactions
                            .OrderByDescending(t => t.Id)
                            .Select(t => (DateTime?)t.TransactionDate)
                            .FirstOrDefault(),
                        s.PhoneNumber,
                        s.Email,
                        // Older suppliers kept their address in Description.
                        Address = s.Address != "" ? s.Address : s.Description,
                        s.OpeningBalance,
                        CustomerId = s.Customer == null ? (int?)null : s.Customer.Id,
                    })
                    .ToListAsync(cancellationToken);

                var accounts = rows.Select(r => new SupplierAccountResponse(
                    r.Id, r.Name, r.Group, r.TotalPurchases, r.TotalPayments, r.Payable, r.LastTransactionDate,
                    r.PhoneNumber, r.Email, r.Address, r.OpeningBalance,
                    r.Receivable, r.Payable, r.Receivable - r.Payable, r.CustomerId))
                    .ToList();

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Supplier accounts retrieved successfully",
                    Data = accounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier accounts");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving supplier accounts."
                };
            }
        }
    }
}
