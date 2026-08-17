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

                if (!string.IsNullOrWhiteSpace(request.InvoiceNumber))
                {
                    var term = $"%{request.InvoiceNumber.Trim()}%";
                    // Match a sale by its own invoice, or a payment by any invoice it settled.
                    query = query.Where(t =>
                        (t.CustomerSale != null &&
                         EF.Functions.ILike(t.CustomerSale.InvoiceNumber, term))
                        ||
                        (t.CustomerPayment != null &&
                         t.CustomerPayment.SaleCustomerPayments.Any(sp =>
                            EF.Functions.ILike(sp.CustomerSale.InvoiceNumber, term))));
                }

                var pagedResult = await query
                    .OrderByDescending(t => t.Id)
                    .Select(t => new CustomerLedgerEntryResponse(
                        t.Id,
                        t.CustomerId,
                        t.Customer.Name,
                        t.TransactionType,
                        t.TransactionDate,
                        // Reference: invoice number for sales, PAY-x for payments, else TXN-x.
                        t.SaleId != null
                            ? (t.CustomerSale!.InvoiceNumber ?? ("SAL-" + t.SaleId))
                            : t.CustomerPaymentId != null
                                ? ("PAY-" + t.CustomerPaymentId)
                                : ("TXN-" + t.Id),
                        t.Debit,
                        t.Credit,
                        t.BalanceAfter,
                        // Invoice(s) tied to this row: the sale's own invoice, or the invoice(s)
                        // a payment was applied against (from the allocation junction).
                        t.SaleId != null
                            ? new List<string> { t.CustomerSale!.InvoiceNumber ?? ("SAL-" + t.SaleId) }
                            : t.CustomerPaymentId != null
                                ? t.CustomerPayment!.SaleCustomerPayments
                                    .OrderBy(sp => sp.Id)
                                    .Select(sp => sp.CustomerSale.InvoiceNumber ?? ("SAL-" + sp.SaleId))
                                    .ToList()
                                : new List<string>(),
                        // Remarks: the sale's own remarks for a Sale row, or the payment's remarks
                        // for a Payment row.
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
