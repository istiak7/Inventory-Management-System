using Inventory_Management_System.Entities;
using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using static Inventory_Management_System.Entities.Common.EntityConstant;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Purchases.Queries.GetPurchaseOrderById
{
    public class GetPurchaseOrderByIdQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetPurchaseOrderByIdQueryHandler> _logger
    ) : IRequestHandler<GetPurchaseOrderByIdQuery, Result>
    {
        public async Task<Result> Handle(GetPurchaseOrderByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var purchase = await _dbContext.SupplierPurchases
                    .AsNoTracking()
                    .Where(p => p.Id == request.Id && p.IsActive != (int)EntityStatus.Deleted)
                    .Select(p => new PurchaseOrderDetailResponse(
                        p.Id,
                        p.SupplierId,
                        p.Supplier.Name,
                        p.BranchId,
                        p.Branch.Name,
                        p.InvoiceNumber,
                        p.Remarks,
                        p.PurchaseDate,
                        p.Status.ToString(),
                        p.PurchaseType.HasValue ? p.PurchaseType.Value.ToString() : null,
                        p.TotalAmount,
                        p.PaidAmount,
                        p.DueAmount,
                        p.DiscountAmount,
                        p.SupplierPurchaseDetails.Select(d => new PurchaseOrderDetailLineResponse(
                            d.Id,
                            d.ProductVariantId,
                            d.ProductVariant.SKU,
                            d.ProductVariant.Product.ProductName,
                            d.ProductVariant.IsSerialized,
                            d.OrderedQuantity,
                            d.ReceivedQuantity,
                            d.UnitPrice,
                            d.TotalAmount,
                            d.WarrantyDays,
                            d.Status.ToString(),
                            d.ProductSerials.Where(s => s.Status != SerialStatus.Ordered).OrderBy(s => s.Id).Select(s => s.SerialNumber).ToList(),
                            d.ProductSerials.Where(s => s.Status == SerialStatus.Ordered).OrderBy(s => s.Id).Select(s => s.SerialNumber).ToList(),
                            _dbContext.PurchaseReturnLines.IgnoreQueryFilters()
                                .Where(l => l.SupplierPurchaseDetailsId == d.Id && l.PurchaseReturn.Status == ReturnStatus.Completed)
                                .Sum(l => (int?)l.Quantity) ?? 0,
                            d.ProductSerials.Where(s => s.Status != SerialStatus.Ordered).OrderBy(s => s.Id)
                                .Select(s => new PurchaseSerialState(s.SerialNumber, s.Status.ToString())).ToList()
                        )).ToList(),
                        p.ReturnedAmount,
                        0m,
                        0m
                    ))
                    .FirstOrDefaultAsync(cancellationToken);

                if (purchase == null)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Status = "Error",
                        Message = "Purchase order not found."
                    };

                // Money released from this bill by earlier edits and still free (our advance with the
                // supplier). An edit that grows the bill puts it back first.
                var free = await _dbContext.SupplierPayments.IgnoreQueryFilters()
                    .Where(p => p.Amount > 0
                                && p.PaymentMethod != PartySettlement.PaymentMethod
                                && p.PaymentMethod != "Refund"
                                && p.SupplierPurchasePayments.Any(a => a.SupplierPurchaseId == request.Id))
                    .Select(p => p.Amount - p.SupplierPurchasePayments.Sum(a => a.Amount))
                    .ToListAsync(cancellationToken);
                var cashPaid = await _dbContext.SupplierPurchasePayments.IgnoreQueryFilters()
                    .Where(a => a.SupplierPurchaseId == request.Id && a.SupplierPayment.PaymentMethod != PartySettlement.PaymentMethod)
                    .SumAsync(a => (decimal?)a.Amount, cancellationToken) ?? 0m;
                purchase = purchase with { ReleasedPayment = free.Where(x => x > 0).Sum(), CashPaidAmount = cashPaid };

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Purchase order retrieved successfully",
                    Data = purchase
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving purchase order {Id}", request.Id);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the purchase order."
                };
            }
        }
    }
}
