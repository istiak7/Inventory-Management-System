using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Stocks.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Inventory.Queries.GetAllSerials
{
    public class GetAllSerialsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetAllSerialsQueryHandler> _logger
    ) : IRequestHandler<GetAllSerialsQuery, Result>
    {
        public async Task<Result> Handle(GetAllSerialsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Reads the existing ProductSerial rows — nothing is copied. The branch query filter
                // in AppDbContext keeps staff to their own branch.
                var query = _dbContext.ProductSerials.AsNoTracking().AsQueryable();

                if (request.BranchId is int branchId)
                    query = query.Where(s => s.BranchId == branchId);

                if (request.ProductVariantId is int variantId)
                    query = query.Where(s => s.ProductVariantId == variantId);

                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    if (!Enum.TryParse<SerialStatus>(request.Status.Trim(), true, out var status))
                        return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Unknown serial status '{request.Status}'." };
                    query = query.Where(s => s.Status == status);
                }

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var term = $"%{request.Search.Trim()}%";
                    query = query.Where(s => EF.Functions.ILike(s.SerialNumber, term));
                }

                var paged = await query
                    .OrderBy(s => s.ProductVariantId).ThenBy(s => s.ReceivedDate).ThenBy(s => s.Id)
                    .Select(s => new
                    {
                        s.Id,
                        s.SerialNumber,
                        s.ProductVariantId,
                        ProductName = s.ProductVariant.Product.ProductName,
                        s.ProductVariant.SKU,
                        s.BranchId,
                        BranchName = s.Branch.Name,
                        s.Status,
                        s.ReceivedDate,
                        s.SoldDate,
                        s.WarrantyDays,
                        PurchaseId = (int?)s.SupplierPurchaseDetails.PurchaseId,
                        PurchaseReference = s.SupplierPurchaseDetails.SupplierPurchase.InvoiceNumber,
                        // A unit holds a sale line only once sold; a voided line has let go of it.
                        Sale = s.Status == SerialStatus.InStock || s.Status == SerialStatus.Ordered
                            ? null
                            : _dbContext.SaleDetails
                                .Where(d => d.ProductSerialId == s.Id && d.Status != SaleLineStatus.Voided)
                                .Select(d => new { d.SaleId, d.CustomerSale.InvoiceNumber })
                                .FirstOrDefault()
                    })
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                var items = paged.Items.Select(x => new SerialResponse(
                    x.Id, x.SerialNumber, x.ProductVariantId, x.ProductName, x.SKU,
                    x.BranchId, x.BranchName, x.Status.ToString(), x.ReceivedDate, x.SoldDate, x.WarrantyDays,
                    x.PurchaseId, string.IsNullOrWhiteSpace(x.PurchaseReference) ? null : x.PurchaseReference,
                    x.Sale?.SaleId, x.Sale?.InvoiceNumber)).ToList();

                return new Result
                {
                    IsSuccess = true, StatusCode = 200, Status = "Success",
                    Message = "Serial numbers retrieved successfully",
                    Data = new PagedResult<SerialResponse>
                    {
                        Items = items, PageNumber = paged.PageNumber, PageSize = paged.PageSize, TotalCount = paged.TotalCount
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving serial numbers");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while retrieving serial numbers." };
            }
        }
    }
}
