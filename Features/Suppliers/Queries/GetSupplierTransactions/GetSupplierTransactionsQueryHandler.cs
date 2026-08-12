using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Suppliers.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
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

                if (!string.IsNullOrWhiteSpace(request.InvoiceNumber))
                {
                    var term = $"%{request.InvoiceNumber.Trim()}%";
                    // Match a purchase by its own invoice, or a payment by any invoice it settled.
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

                var pagedResult = await query
                    .OrderByDescending(t => t.Id)
                    .Select(t => new SupplierLedgerEntryResponse(
                        t.Id,
                        t.SupplierId,
                        t.Supplier.Name,
                        t.TransactionType,
                        t.TransactionDate,
                        // Reference: invoice number for purchases, PAY-x for payments, else TXN-x.
                        t.SupplierPurchaseId != null
                            ? (t.SupplierPurchase!.InvoiceNumber ?? ("PUR-" + t.SupplierPurchaseId))
                            : t.SupplierPaymentId != null
                                ? ("PAY-" + t.SupplierPaymentId)
                                : ("TXN-" + t.Id),
                        t.Debit,
                        t.Credit,
                        t.BalanceAfter,
                        // Invoice(s) tied to this row: the purchase's own invoice, or the invoice(s)
                        // a payment was applied against (from the allocation junction).
                        t.SupplierPurchaseId != null
                            ? new List<string> { t.SupplierPurchase!.InvoiceNumber ?? ("PUR-" + t.SupplierPurchaseId) }
                            : t.SupplierPaymentId != null
                                ? t.SupplierPayment!.SupplierPurchasePayments
                                    .OrderBy(pp => pp.Id)
                                    .Select(pp => pp.SupplierPurchase.InvoiceNumber ?? ("PUR-" + pp.SupplierPurchaseId))
                                    .ToList()
                                : new List<string>(),
                        // Remarks: the purchase order's own remarks for a Purchase row, or the
                        // payment's remarks for a Payment row.
                        t.SupplierPurchaseId != null
                            ? t.SupplierPurchase!.Remarks
                            : t.SupplierPaymentId != null
                                ? t.SupplierPayment!.Remarks
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
