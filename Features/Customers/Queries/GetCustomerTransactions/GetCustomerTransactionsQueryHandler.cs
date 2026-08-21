using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Customers.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerTransactions
{
    public class GetCustomerTransactionsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetCustomerTransactionsQueryHandler> _logger
    ) : IRequestHandler<GetCustomerTransactionsQuery, Result>
    {
        public async Task<Result> Handle(GetCustomerTransactionsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.CustomerTransactions.AsNoTracking();

                if (request.CustomerId is int customerId)
                    query = query.Where(t => t.CustomerId == customerId);

                if (request.BranchId is int branchId)
                    query = query.Where(t => t.CustomerSale.BranchId == branchId);

                if (!string.IsNullOrWhiteSpace(request.InvoiceNumber))
                {
                    var term = $"%{request.InvoiceNumber.Trim()}%";
                    query = query.Where(t =>
                        (t.CustomerSale != null &&
                         EF.Functions.ILike(t.CustomerSale.InvoiceNumber, term))
                        ||
                        (t.CustomerPayment != null &&
                         t.CustomerPayment.SaleCustomerPayments.Any(sp =>
                            EF.Functions.ILike(sp.CustomerSale.InvoiceNumber, term))));
                }

                if (!string.IsNullOrWhiteSpace(request.TransactionType))
                {
                    query = query.Where(t => t.TransactionType == request.TransactionType);
                }

                if (request.StartDate.HasValue)
                    query = query.Where(t => t.TransactionDate >= request.StartDate.Value.Date);

                if (request.EndDate.HasValue)
                    query = query.Where(t => t.TransactionDate < request.EndDate.Value.Date.AddDays(1));

                var pagedResult = await query
                    .OrderByDescending(t => t.Id)
                    .Select(t => new CustomerLedgerEntryResponse(
                        t.Id,
                        t.CustomerId,
                        t.Customer.Name,
                        t.TransactionType,
                        t.TransactionDate,
                        t.SaleId != null
                            ? (t.CustomerSale!.InvoiceNumber ?? ("SAL-" + t.SaleId))
                            : t.CustomerPaymentId != null
                                ? ("PAY-" + t.CustomerPaymentId)
                                : ("TXN-" + t.Id),
                        t.Debit,
                        t.Credit,
                        t.BalanceAfter,
                        t.SaleId != null
                            ? new List<string> { t.CustomerSale!.InvoiceNumber ?? ("SAL-" + t.SaleId) }
                            : t.CustomerPaymentId != null
                                ? t.CustomerPayment!.SaleCustomerPayments
                                    .OrderBy(sp => sp.Id)
                                    .Select(sp => sp.CustomerSale.InvoiceNumber ?? ("SAL-" + sp.SaleId))
                                    .ToList()
                                : new List<string>(),
                        t.SaleId != null
                            ? t.CustomerSale!.Remarks
                            : t.CustomerPaymentId != null
                                ? t.CustomerPayment!.Remarks
                                : null))
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Customer transactions retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer transactions");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving customer transactions."
                };
            }
        }
    }
}
