using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Quotations.Shared;
using Inventory_Management_System.Features.Quotations.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Quotations.Queries.GetQuotationById
{
    public class GetQuotationByIdQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetQuotationByIdQueryHandler> _logger
    ) : IRequestHandler<GetQuotationByIdQuery, Result>
    {
        public async Task<Result> Handle(GetQuotationByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var q = await _dbContext.Quotations
                    .AsNoTracking()
                    .Where(x => x.Id == request.Id)
                    .Select(x => new
                    {
                        x.Id,
                        x.QuotationNumber,
                        x.QuotationDate,
                        x.ValidUntil,
                        x.CustomerId,
                        CustomerName = x.Customer.Name,
                        CustomerPhoneNumber = x.Customer.PhoneNumber,
                        CustomerAddress = x.Customer.Address,
                        CustomerEmail = x.Customer.Email,
                        x.Status,
                        x.SubTotal,
                        x.DiscountAmount,
                        x.TaxAmount,
                        x.TotalAmount,
                        x.Notes,
                        x.TermsAndConditions,
                        x.ConvertedSaleId,
                        ConvertedInvoiceNumber = x.ConvertedSale != null ? x.ConvertedSale.InvoiceNumber : null,
                        x.ConvertedAt,
                        x.CreatedAt,
                        CreatedBy = _dbContext.Users
                            .Where(u => u.Id == x.CreatedById)
                            .Select(u => u.Name)
                            .FirstOrDefault(),
                        Lines = x.QuotationDetails
                            .OrderBy(d => d.Id)
                            .Select(d => new QuotationLineResponse(
                                d.Id, d.ProductVariantId, d.ProductName, d.Sku, d.Uom, d.Warranty,
                                d.ProductVariant.IsSerialized,
                                d.Quantity, d.UnitPrice, d.DiscountAmount, d.TotalAmount))
                            .ToList(),
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (q == null)
                    return QuotationRules.Fail(404, "Quotation not found.");

                var response = new QuotationDetailResponse(
                    q.Id, q.QuotationNumber, q.QuotationDate, q.ValidUntil,
                    q.CustomerId, q.CustomerName, q.CustomerPhoneNumber,
                    q.CustomerAddress ?? string.Empty, q.CustomerEmail ?? string.Empty,
                    QuotationRules.EffectiveStatus(q.Status, q.ValidUntil).ToString(),
                    q.SubTotal, q.DiscountAmount, q.TaxAmount, q.TotalAmount,
                    q.Notes, q.TermsAndConditions,
                    q.Lines.Count, q.Lines,
                    q.ConvertedSaleId, q.ConvertedInvoiceNumber, q.ConvertedAt,
                    q.CreatedBy, q.CreatedAt);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Quotation retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quotation {QuotationId}", request.Id);
                return QuotationRules.Fail(500, "An error occurred while retrieving the quotation.");
            }
        }
    }
}
