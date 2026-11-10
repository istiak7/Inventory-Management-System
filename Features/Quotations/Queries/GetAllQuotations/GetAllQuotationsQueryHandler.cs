using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Customers.Shared;
using Inventory_Management_System.Features.Quotations.Shared;
using Inventory_Management_System.Features.Quotations.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Quotations.Queries.GetAllQuotations
{
    public class GetAllQuotationsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetAllQuotationsQueryHandler> _logger
    ) : IRequestHandler<GetAllQuotationsQuery, Result>
    {
        public async Task<Result> Handle(GetAllQuotationsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.Quotations.AsNoTracking();

                if (request.CustomerId is int customerId)
                    query = query.Where(q => q.CustomerId == customerId);

                if (!string.IsNullOrWhiteSpace(request.Status) &&
                    Enum.TryParse<QuotationStatus>(request.Status, true, out var status))
                    query = query.WhereEffectiveStatus(status);

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var term = $"%{request.Search.Trim()}%";
                    var phone = CustomerPhoneNumber.Normalize(request.Search);
                    var phoneTerm = phone.Length > 0 ? $"%{phone}%" : null;

                    query = query.Where(q =>
                        EF.Functions.ILike(q.QuotationNumber, term) ||
                        EF.Functions.ILike(q.Customer.Name, term) ||
                        (phoneTerm != null && EF.Functions.ILike(q.Customer.PhoneNumber, phoneTerm)));
                }

                // Same shop-calendar day rule as the sales list.
                if (request.StartDate.HasValue)
                {
                    var start = BusinessClock.StartOfDayUtc(request.StartDate.Value);
                    query = query.Where(q => q.QuotationDate >= start);
                }

                if (request.EndDate.HasValue)
                {
                    var endExclusive = BusinessClock.StartOfDayUtc(request.EndDate.Value.Date.AddDays(1));
                    query = query.Where(q => q.QuotationDate < endExclusive);
                }

                var paged = await query
                    .OrderByDescending(q => q.Id)
                    .Select(q => new
                    {
                        q.Id,
                        q.QuotationNumber,
                        q.QuotationDate,
                        q.ValidUntil,
                        q.CustomerId,
                        CustomerName = q.Customer.Name,
                        CustomerPhoneNumber = q.Customer.PhoneNumber,
                        q.Status,
                        q.SubTotal,
                        q.DiscountAmount,
                        q.TaxAmount,
                        q.TotalAmount,
                        ItemsCount = q.QuotationDetails.Count,
                        q.ConvertedSaleId,
                        ConvertedInvoiceNumber = q.ConvertedSale != null ? q.ConvertedSale.InvoiceNumber : null,
                        CreatedBy = _dbContext.Users
                            .Where(u => u.Id == q.CreatedById)
                            .Select(u => u.Name)
                            .FirstOrDefault(),
                    })
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                var items = paged.Items.Select(q => new QuotationListResponse(
                    q.Id, q.QuotationNumber, q.QuotationDate, q.ValidUntil,
                    q.CustomerId, q.CustomerName, q.CustomerPhoneNumber,
                    QuotationRules.EffectiveStatus(q.Status, q.ValidUntil).ToString(),
                    q.SubTotal, q.DiscountAmount, q.TaxAmount, q.TotalAmount, q.ItemsCount,
                    q.ConvertedSaleId, q.ConvertedInvoiceNumber, q.CreatedBy))
                    .ToList();

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Quotations retrieved successfully",
                    Data = new PagedResult<QuotationListResponse>
                    {
                        Items = items,
                        PageNumber = paged.PageNumber,
                        PageSize = paged.PageSize,
                        TotalCount = paged.TotalCount
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quotations");
                return QuotationRules.Fail(500, "An error occurred while retrieving quotations.");
            }
        }
    }
}
