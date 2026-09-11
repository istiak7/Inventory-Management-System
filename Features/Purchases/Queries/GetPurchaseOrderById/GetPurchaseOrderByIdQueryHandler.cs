using Inventory_Management_System.Database;
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
                        p.PurchaseType.ToString(),
                        p.TotalAmount,
                        p.PaidAmount,
                        p.DueAmount,
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
                            d.WarrantyMonths,
                            d.Status.ToString(),
                            d.ProductSerials.Select(s => s.SerialNumber).ToList()
                        )).ToList()
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
