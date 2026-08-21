using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Customers.Shared;
using Inventory_Management_System.Features.Warranty.Shared;
using Inventory_Management_System.Features.Warranty.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Warranty.Queries.GetAllWarrantyClaims
{
    public class GetAllWarrantyClaimsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetAllWarrantyClaimsQueryHandler> _logger
    ) : IRequestHandler<GetAllWarrantyClaimsQuery, Result>
    {
        public async Task<Result> Handle(GetAllWarrantyClaimsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.WarrantyClaims.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(request.Status) &&
                    Enum.TryParse<WarrantyClaimStatus>(request.Status, true, out var status))
                    query = query.Where(c => c.Status == status);

                if (request.BranchId is int branchId)
                    query = query.Where(c => c.BranchId == branchId);

                if (request.CustomerId is int customerId)
                    query = query.Where(c => c.CustomerId == customerId);

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var term = $"%{request.Search.Trim()}%";
                    var phone = CustomerPhoneNumber.Normalize(request.Search);
                    var phoneTerm = phone.Length > 0 ? $"%{phone}%" : null;

                    query = query.Where(c =>
                        EF.Functions.ILike(c.ClaimNumber, term) ||
                        EF.Functions.ILike(c.ProductSerial.SerialNumber, term) ||
                        EF.Functions.ILike(c.SaleDetails.CustomerSale.InvoiceNumber, term) ||
                        EF.Functions.ILike(c.Customer.Name, term) ||
                        (phoneTerm != null && EF.Functions.ILike(c.Customer.PhoneNumber, phoneTerm)));
                }

                var paged = await query
                    .OrderByDescending(c => c.Id)
                    .ProjectToRow()
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                var pagedResult = new PagedResult<WarrantyClaimResponse>
                {
                    Items = [.. paged.Items.Select(row => row.ToResponse())],
                    PageNumber = paged.PageNumber,
                    PageSize = paged.PageSize,
                    TotalCount = paged.TotalCount
                };

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Warranty claims retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving warranty claims");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while retrieving warranty claims." };
            }
        }
    }
}
