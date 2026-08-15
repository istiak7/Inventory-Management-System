using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Customers.Shared;
using Inventory_Management_System.Features.Sales.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Sales.Queries.GetAllSales
{
    public class GetAllSalesQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetAllSalesQueryHandler> _logger
    ) : IRequestHandler<GetAllSalesQuery, Result>
    {
        public async Task<Result> Handle(GetAllSalesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.CustomerSales.AsNoTracking();

                if (request.CustomerId is int customerId)
                    query = query.Where(s => s.CustomerId == customerId);

                if (request.BranchId is int branchId)
                    query = query.Where(s => s.BranchId == branchId);

                if (!string.IsNullOrWhiteSpace(request.SaleType) &&
                    Enum.TryParse<SaleType>(request.SaleType, true, out var saleTypeFilter))
                    query = query.Where(s => s.SaleType == saleTypeFilter);

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var term = $"%{request.Search.Trim()}%";
                    // Phone is how a walk-in identifies themselves, and it is stored normalized —
                    // a typed "+880 17…" would never ILIKE-match the stored "017…", so compare the
                    // normalized digits instead of the raw text.
                    var phone = CustomerPhoneNumber.Normalize(request.Search);
                    var phoneTerm = phone.Length > 0 ? $"%{phone}%" : null;

                    query = query.Where(s =>
                        EF.Functions.ILike(s.InvoiceNumber, term) ||
                        EF.Functions.ILike(s.Customer.Name, term) ||
                        (phoneTerm != null && EF.Functions.ILike(s.Customer.PhoneNumber, phoneTerm)));
                }

                // Materialize with enums intact, then map to string DTOs in memory — EF cannot
                // translate Enum.ToString().
                var paged = await query
                    .OrderByDescending(s => s.Id)
                    .Select(s => new
                    {
                        s.Id,
                        s.CustomerId,
                        CustomerName = s.Customer.Name,
                        CustomerPhoneNumber = s.Customer.PhoneNumber,
                        s.BranchId,
                        BranchName = s.Branch.Name,
                        s.InvoiceNumber,
                        s.Remarks,
                        s.SaleDate,
                        s.Status,
                        s.SaleType,
                        s.SubTotal,
                        s.DiscountAmount,
                        s.TaxAmount,
                        s.TotalAmount,
                        s.PaidAmount,
                        s.DueAmount,
                        ItemsCount = s.SaleDetails.Count,
                        Lines = s.SaleDetails.Select(d => new
                        {
                            d.Id,
                            d.ProductVariantId,
                            Sku = d.ProductVariant.SKU,
                            ProductName = d.ProductVariant.Product.ProductName,
                            d.ProductVariant.IsSerialized,
                            d.Quantity,
                            d.UnitPrice,
                            d.DiscountPerItem,
                            d.TotalAmount,
                            d.WarrantyMonths,
                            d.Status,
                            SerialNumber = d.ProductSerial != null ? d.ProductSerial.SerialNumber : null
                        }).ToList()
                    })
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                var items = paged.Items.Select(s => new SaleListResponse(
                    s.Id, s.CustomerId, s.CustomerName, s.CustomerPhoneNumber,
                    s.BranchId, s.BranchName, s.InvoiceNumber, s.Remarks, s.SaleDate,
                    s.Status.ToString(), s.SaleType.ToString(),
                    s.SubTotal, s.DiscountAmount, s.TaxAmount, s.TotalAmount,
                    s.PaidAmount, s.DueAmount, s.ItemsCount,
                    s.Lines.Select(l => new SaleLineResponse(
                        l.Id, l.ProductVariantId, l.Sku, l.ProductName, l.IsSerialized,
                        l.Quantity, l.UnitPrice, l.DiscountPerItem, l.TotalAmount,
                        l.WarrantyMonths, l.Status.ToString(), l.SerialNumber)).ToList()))
                    .ToList();

                var pagedResult = new PagedResult<SaleListResponse>
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
                    Message = "Sales retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sales");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving sales."
                };
            }
        }
    }
}
