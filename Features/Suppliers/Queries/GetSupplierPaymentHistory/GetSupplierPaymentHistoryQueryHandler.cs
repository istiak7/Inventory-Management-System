using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Suppliers.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierPaymentHistory
{
    public class GetSupplierPaymentHistoryQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetSupplierPaymentHistoryQueryHandler> _logger
    ) : IRequestHandler<GetSupplierPaymentHistoryQuery, Result>
    {
        public async Task<Result> Handle(GetSupplierPaymentHistoryQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.SupplierPurchasePayments
                    .AsNoTracking()
                    .Where(pp => pp.SupplierPayment.SupplierId == request.SupplierId);

                if (request.PaymentId is int paymentId)
                    query = query.Where(pp => pp.SupplierPaymentId == paymentId);

                if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
                    query = query.Where(pp => pp.SupplierPayment.PaymentMethod == request.PaymentMethod);

                if (!string.IsNullOrWhiteSpace(request.InvoiceNumber))
                {
                    var term = $"%{request.InvoiceNumber.Trim()}%";
                    query = query.Where(pp =>
                        pp.SupplierPurchase.InvoiceNumber != null &&
                        EF.Functions.ILike(pp.SupplierPurchase.InvoiceNumber, term));
                }

                var pagedResult = await query
                    .OrderByDescending(pp => pp.Id)
                    .Select(pp => new SupplierPaymentHistoryResponse(
                        pp.Id,
                        pp.SupplierPaymentId,
                        pp.SupplierPurchaseId,
                        pp.SupplierPurchase.InvoiceNumber,
                        pp.Amount,
                        pp.AllocationDate,
                        pp.SupplierPayment.PaymentMethod,
                        pp.SupplierPayment.Remarks,
                        pp.SupplierPayment.PaymentDate,
                        pp.SupplierPayment.SupplierId,
                        pp.SupplierPayment.Supplier.Name,
                        pp.SupplierPayment.Amount,
                        // Account balance after the whole payment; before = after + money paid + discount waived.
                        (_dbContext.SupplierTransactions
                            .Where(t => t.SupplierPaymentId == pp.SupplierPaymentId && t.TransactionType == "Payment")
                            .Select(t => (decimal?)t.BalanceAfter)
                            .FirstOrDefault() ?? 0m)
                            + pp.SupplierPayment.Amount
                            + (pp.SupplierPayment.SupplierPurchasePayments.Sum(x => (decimal?)x.Discount) ?? 0m),
                        _dbContext.SupplierTransactions
                            .Where(t => t.SupplierPaymentId == pp.SupplierPaymentId && t.TransactionType == "Payment")
                            .Select(t => (decimal?)t.BalanceAfter)
                            .FirstOrDefault() ?? 0m,
                        // Invoice total before any payment-time discount, and the dues around this
                        // allocation — counting money paid and discount waived at each step.
                        pp.SupplierPurchase.TotalAmount
                            + (pp.SupplierPurchase.SupplierPurchasePayments.Sum(x => (decimal?)x.Discount) ?? 0m),
                        pp.SupplierPurchase.TotalAmount
                            + (pp.SupplierPurchase.SupplierPurchasePayments.Sum(x => (decimal?)x.Discount) ?? 0m)
                            - (pp.SupplierPurchase.SupplierPurchasePayments
                                .Where(x => x.Id < pp.Id)
                                .Sum(x => (decimal?)(x.Amount + x.Discount)) ?? 0m),
                        pp.SupplierPurchase.TotalAmount
                            + (pp.SupplierPurchase.SupplierPurchasePayments.Sum(x => (decimal?)x.Discount) ?? 0m)
                            - (pp.SupplierPurchase.SupplierPurchasePayments
                                .Where(x => x.Id <= pp.Id)
                                .Sum(x => (decimal?)(x.Amount + x.Discount)) ?? 0m),
                        pp.Discount))
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Supplier payment history retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier payment history");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving supplier payment history."
                };
            }
        }
    }
}
