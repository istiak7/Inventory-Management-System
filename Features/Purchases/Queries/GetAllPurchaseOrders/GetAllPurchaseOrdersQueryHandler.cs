using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Customers.Shared;
using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using Inventory_Management_System.Shared.Extensions.QueryableFilterExtensions;
using static Inventory_Management_System.Entities.Common.EntityConstant;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Purchases.Queries.GetAllPurchaseOrders
{
    public class GetAllPurchaseOrdersQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetAllPurchaseOrdersQueryHandler> _logger
    ) : IRequestHandler<GetAllPurchaseOrdersQuery, Result>
    {
        public async Task<Result> Handle(GetAllPurchaseOrdersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.SupplierPurchases
                    .AsNoTracking()
                    .Where(p => p.IsActive != (int)EntityStatus.Deleted);

                if (!string.IsNullOrWhiteSpace(request.Status) &&
                    Enum.TryParse<PurchaseStatus>(request.Status, true, out var statusFilter))
                    query = query.Where(p => p.Status == statusFilter);

                if (request.SupplierId is int supplierId)
                    query = query.Where(p => p.SupplierId == supplierId);

                if (request.BranchId is int branchId)
                    query = query.Where(p => p.BranchId == branchId);

                if (!string.IsNullOrWhiteSpace(request.PurchaseType) &&
                    Enum.TryParse<PurchaseType>(request.PurchaseType, true, out var purchaseTypeFilter))
                    query = query.Where(p => p.PurchaseType == purchaseTypeFilter);

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var term = $"%{request.Search.Trim()}%";
                    var phone = CustomerPhoneNumber.Normalize(request.Search);
                    var phoneTerm = phone.Length > 0 ? $"%{phone}%" : null;

                    query = query.Where(p =>
                        EF.Functions.ILike(p.InvoiceNumber, term) ||
                        EF.Functions.ILike(p.Supplier.Name, term) ||
                        (phoneTerm != null && EF.Functions.ILike(p.Supplier.PhoneNumber, phoneTerm)));
                }

                query = query.WhereDateRange(t => t.PurchaseDate, request.StartDate, request.EndDate);

                var paged = await query
                    .OrderByDescending(p => p.Id)
                    .Select(p => new
                    {
                        p.Id,
                        p.SupplierId,
                        SupplierName = p.Supplier.Name,
                        p.BranchId,
                        BranchName = p.Branch.Name,
                        p.InvoiceNumber,
                        p.Remarks,
                        p.PurchaseDate,
                        p.Status,
                        p.PurchaseType,
                        p.TotalAmount,
                        p.PaidAmount,
                        p.DueAmount,
                        ItemsCount = p.SupplierPurchaseDetails.Count,
                        Lines = p.SupplierPurchaseDetails.Select(d => new
                        {
                            d.Id,
                            d.ProductVariantId,
                            Sku = d.ProductVariant.SKU,
                            ProductName = d.ProductVariant.Product.ProductName,
                            d.ProductVariant.IsSerialized,
                            d.OrderedQuantity,
                            d.ReceivedQuantity,
                            d.UnitPrice,
                            d.TotalAmount,
                            d.WarrantyMonths,
                            d.Status
                        }).ToList()
                    })
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                var items = paged.Items.Select(p => new PurchaseOrderListResponse(
                    p.Id, p.SupplierId, p.SupplierName, p.BranchId, p.BranchName,
                    p.InvoiceNumber, p.Remarks, p.PurchaseDate, p.Status.ToString(), p.PurchaseType?.ToString(),
                    p.TotalAmount, p.PaidAmount, p.DueAmount, p.ItemsCount,
                    p.Lines.Select(l => new PurchaseOrderLineResponse(
                        l.Id, l.ProductVariantId, l.Sku, l.ProductName, l.IsSerialized, l.OrderedQuantity, l.ReceivedQuantity,
                        l.UnitPrice, l.TotalAmount, l.WarrantyMonths, l.Status.ToString())).ToList()))
                    .ToList();

                var pagedResult = new PagedResult<PurchaseOrderListResponse>
                {
                    Items = items,
                    PageNumber = paged.PageNumber,
                    PageSize = paged.PageSize,
                    TotalCount = paged.TotalCount
                };

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Purchase orders retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving purchase orders");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving purchase orders."
                };
            }
        }
    }
}
