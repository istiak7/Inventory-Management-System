using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Customers.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerPaymentHistory
{
    public class GetCustomerPaymentHistoryQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetCustomerPaymentHistoryQueryHandler> _logger
    ) : IRequestHandler<GetCustomerPaymentHistoryQuery, Result>
    {
        public async Task<Result> Handle(GetCustomerPaymentHistoryQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Every allocation of a payment to an invoice for this customer, newest first.
                var query = _dbContext.SaleCustomerPayments
                    .AsNoTracking()
                    .Where(sp => sp.CustomerPayment.CustomerId == request.CustomerId);

                if (request.PaymentId is int paymentId)
                    query = query.Where(sp => sp.CustomerPaymentId == paymentId);

                if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
                    query = query.Where(sp => sp.CustomerPayment.PaymentMethod == request.PaymentMethod);

                if (!string.IsNullOrWhiteSpace(request.InvoiceNumber))
                {
                    var term = $"%{request.InvoiceNumber.Trim()}%";
                    query = query.Where(sp =>
                        EF.Functions.ILike(sp.CustomerSale.InvoiceNumber, term));
                }

                var pagedResult = await query
                    .OrderByDescending(sp => sp.Id)
                    .Select(sp => new CustomerPaymentHistoryResponse(
                        sp.Id,
                        sp.CustomerPaymentId,
                        sp.SaleId,
                        sp.CustomerSale.InvoiceNumber,
                        sp.Amount,
                        sp.AllocationDate,
                        sp.CustomerPayment.PaymentMethod,
                        sp.CustomerPayment.Remarks,
                        sp.CustomerPayment.PaymentDate,
                        sp.CustomerPayment.CustomerId,
                        sp.CustomerPayment.Customer.Name,
                        sp.CustomerPayment.Amount,
                        // The payment wrote exactly one ledger row, whose BalanceAfter is the
                        // customer balance once this payment settled. Before = after + payment.
                        (_dbContext.CustomerTransactions
                            .Where(t => t.CustomerPaymentId == sp.CustomerPaymentId)
                            .Select(t => (decimal?)t.BalanceAfter)
                            .FirstOrDefault() ?? 0m) + sp.CustomerPayment.Amount,
                        _dbContext.CustomerTransactions
                            .Where(t => t.CustomerPaymentId == sp.CustomerPaymentId)
                            .Select(t => (decimal?)t.BalanceAfter)
                            .FirstOrDefault() ?? 0m,
                        sp.CustomerSale.TotalAmount,
                        // Due on this invoice as it stood around this allocation — the live
                        // DueAmount minus every allocation recorded after this one. Allocations are
                        // append-only, so Id order is chronological order.
                        sp.CustomerSale.TotalAmount
                            - (sp.CustomerSale.SaleCustomerPayments
                                .Where(x => x.Id < sp.Id)
                                .Sum(x => (decimal?)x.Amount) ?? 0m),
                        sp.CustomerSale.TotalAmount
                            - (sp.CustomerSale.SaleCustomerPayments
                                .Where(x => x.Id <= sp.Id)
                                .Sum(x => (decimal?)x.Amount) ?? 0m)))
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Customer payment history retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer payment history");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving customer payment history."
                };
            }
        }
    }
}
