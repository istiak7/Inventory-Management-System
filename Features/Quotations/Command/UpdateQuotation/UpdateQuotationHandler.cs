using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Quotations.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Quotations.Command.UpdateQuotation
{
    // Replaces the customer, dates, lines and totals of an open quotation.
    // Accepted and Converted quotations are fixed: the customer agreed to them.
    public class UpdateQuotationHandler(
        AppDbContext _dbContext,
        ILogger<UpdateQuotationHandler> _logger
    ) : IRequestHandler<UpdateQuotationCommand, Result>
    {
        public async Task<Result> Handle(UpdateQuotationCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Waits for a status change or conversion running at the same moment.
                await _dbContext.LockRowAsync<Quotation>(request.Id, cancellationToken);

                var quotation = await _dbContext.Quotations
                    .Include(q => q.QuotationDetails)
                    .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);
                if (quotation == null)
                    return QuotationRules.Fail(404, "Quotation not found.");

                var current = QuotationRules.EffectiveStatus(quotation.Status, quotation.ValidUntil);
                if (!QuotationRules.CanEdit(current))
                    return QuotationRules.Fail(400, $"A quotation that is {current} cannot be edited.");

                if (quotation.CustomerId != request.CustomerId)
                {
                    var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);
                    if (customer == null)
                        return QuotationRules.Fail(404, "Customer not found.");
                    quotation.Customer = customer;
                    quotation.CustomerId = customer.Id;
                }

                if (request.QuotationDate.HasValue)
                    quotation.QuotationDate = QuotationRules.ToStoredDate(request.QuotationDate.Value);
                quotation.ValidUntil = QuotationRules.ToStoredDate(request.ValidUntil);
                quotation.Notes = QuotationRules.CleanText(request.Notes);
                quotation.TermsAndConditions = QuotationRules.CleanText(request.TermsAndConditions);

                var error = await QuotationRules.ApplyLinesAsync(
                    _dbContext, quotation, request.Items, request.DiscountAmount, request.TaxAmount, cancellationToken);
                if (error != null)
                    return error;

                // An edited rejected or expired offer is a new draft (the validator already
                // made sure its validity is not in the past).
                if (current is QuotationStatus.Rejected or QuotationStatus.Expired)
                    quotation.Status = QuotationStatus.Draft;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = $"Quotation {quotation.QuotationNumber} updated successfully",
                    Data = new { quotation.Id, quotation.QuotationNumber }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error updating quotation {QuotationId}", request.Id);
                return QuotationRules.Fail(500, "An error occurred while updating the quotation.");
            }
        }
    }
}
