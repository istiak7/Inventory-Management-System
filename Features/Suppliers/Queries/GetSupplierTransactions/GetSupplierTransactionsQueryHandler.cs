using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Suppliers.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Inventory_Management_System.Shared.Extensions.QueryableFilterExtensions;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierTransactions
{
    public class GetSupplierTransactionsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetSupplierTransactionsQueryHandler> _logger
    ) : IRequestHandler<GetSupplierTransactionsQuery, Result>
    {
        public async Task<Result> Handle(GetSupplierTransactionsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.SupplierTransactions.AsNoTracking();

                if (request.SupplierId is int supplierId)
                    query = query.Where(t => t.SupplierId == supplierId);

                // A purchase row belongs to its order's branch, a payment row to its payment's branch.
                if (request.BranchId is int branchId)
                    query = query.Where(t =>
                        (t.SupplierPurchase != null && t.SupplierPurchase.BranchId == branchId) ||
                        (t.SupplierPayment != null && t.SupplierPayment.BranchId == branchId));

                if (!string.IsNullOrWhiteSpace(request.InvoiceNumber))
                {
                    var term = $"%{request.InvoiceNumber.Trim()}%";
                    query = query.Where(t =>
                        (t.SupplierPurchase != null &&
                         t.SupplierPurchase.InvoiceNumber != null &&
                         EF.Functions.ILike(t.SupplierPurchase.InvoiceNumber, term))
                        ||
                        (t.SupplierPayment != null &&
                         t.SupplierPayment.SupplierPurchasePayments.Any(pp =>
                            pp.SupplierPurchase.InvoiceNumber != null &&
                            EF.Functions.ILike(pp.SupplierPurchase.InvoiceNumber, term))));
                }

                if (!string.IsNullOrWhiteSpace(request.TransactionType))
                {
                    query = query.Where(t => t.TransactionType == request.TransactionType);
                }

                // Days are the shop's (Dhaka) calendar days, not UTC ones: a payment at 01:00 local
                // time belongs to that day, which in UTC is still the day before.
                query = query.WhereDateRange(t => t.TransactionDate, request.StartDate, request.EndDate);

                var pagedResult = await query
                    .OrderByDescending(t => t.Id)
                    .Select(t => new SupplierLedgerEntryResponse(
                        t.Id,
                        t.SupplierId,
                        t.Supplier.Name,
                        t.TransactionType,
                        t.TransactionDate,
                        t.SupplierPurchaseId != null
                            ? (t.SupplierPurchase!.InvoiceNumber ?? ("PUR-" + t.SupplierPurchaseId))
                            : t.SupplierPaymentId != null
                                ? ("PAY-" + t.SupplierPaymentId)
                                : ("TXN-" + t.Id),
                        t.Debit,
                        t.Credit,
                        t.BalanceAfter,
                        t.SupplierPurchaseId != null
                            ? new List<string> { t.SupplierPurchase!.InvoiceNumber ?? ("PUR-" + t.SupplierPurchaseId) }
                            : t.SupplierPaymentId != null
                                ? t.SupplierPayment!.SupplierPurchasePayments
                                    .OrderBy(pp => pp.Id)
                                    .Select(pp => pp.SupplierPurchase.InvoiceNumber ?? ("PUR-" + pp.SupplierPurchaseId))
                                    .ToList()
                                : new List<string>(),
                        t.SupplierPurchaseId != null
                            ? t.SupplierPurchase!.Remarks
                            : t.SupplierPaymentId != null
                                ? t.SupplierPayment!.Remarks
                                : null,
                        // Current due on the related invoice(s), shown under the invoice number so
                        // no separate column is needed. A Purchase row carries its own invoice's due;
                        // a Payment/Discount row carries the total current due of the invoice(s) it
                        // touched (null for an on-account payment with no allocations).
                        t.SupplierPurchaseId != null
                            ? (decimal?)t.SupplierPurchase!.DueAmount
                            : t.SupplierPaymentId != null
                                ? t.SupplierPayment!.SupplierPurchasePayments
                                    .Sum(pp => (decimal?)pp.SupplierPurchase.DueAmount)
                                : null))
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Supplier transactions retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier transactions");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving supplier transactions."
                };
            }
        }
    }
}
