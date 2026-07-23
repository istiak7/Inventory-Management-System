using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
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
                var query = _dbContext.SupplierPurchases.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(request.Status))
                    query = query.Where(p => p.Status == request.Status);

                if (request.SupplierId is int supplierId)
                    query = query.Where(p => p.SupplierId == supplierId);

                // Newest first — pending orders awaiting approval surface at the top.
                var pagedResult = await query
                    .OrderByDescending(p => p.Id)
                    .Select(p => new PurchaseOrderListResponse(
                        p.Id,
                        p.SupplierId,
                        p.Supplier.Name,
                        p.BranchId,
                        p.Branch.Name,
                        p.InvoiceNumber,
                        p.PurchaseDate,
                        p.Status,
                        p.PurchaseType,
                        p.TotalAmount,
                        p.PaidAmount,
                        p.DueAmount,
                        p.SupplierPurchaseDetails.Count,
                        p.SupplierPurchaseDetails
                            .Select(d => new PurchaseOrderLineResponse(
                                d.ProductId,
                                d.Product.ProductName,
                                d.Quantity,
                                d.UnitPrice,
                                d.TotalAmount,
                                d.IsApproved))
                            .ToList()))
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

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
