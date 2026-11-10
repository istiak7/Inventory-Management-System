using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Quotations.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Inventory_Management_System.Features.Quotations.Command.CreateQuotation
{
    // Saves a new quotation as Draft. Only the quotation tables are written:
    // no stock, inventory transaction, customer ledger or payment rows.
    public class CreateQuotationHandler(
        AppDbContext _dbContext,
        ILogger<CreateQuotationHandler> _logger
    ) : IRequestHandler<CreateQuotationCommand, Result>
    {
        public async Task<Result> Handle(CreateQuotationCommand request, CancellationToken cancellationToken)
        {
            var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);
            if (customer == null)
                return QuotationRules.Fail(404, "Customer not found.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var quotationDate = request.QuotationDate.HasValue
                    ? QuotationRules.ToStoredDate(request.QuotationDate.Value)
                    : QuotationRules.TodayStored;

                var quotation = new Quotation
                {
                    QuotationNumber = await DocumentNumbers.NextAsync(
                        _dbContext,
                        _dbContext.Quotations.Select(q => q.QuotationNumber),
                        $"QT-{quotationDate.Year}-",
                        cancellationToken),
                    CustomerId = customer.Id,
                    Customer = customer,
                    QuotationDate = quotationDate,
                    ValidUntil = QuotationRules.ToStoredDate(request.ValidUntil),
                    Status = QuotationStatus.Draft,
                    Notes = QuotationRules.CleanText(request.Notes),
                    TermsAndConditions = QuotationRules.CleanText(request.TermsAndConditions),
                };

                var error = await QuotationRules.ApplyLinesAsync(
                    _dbContext, quotation, request.Items, request.DiscountAmount, request.TaxAmount, cancellationToken);
                if (error != null)
                    return error;

                await _dbContext.Quotations.AddAsync(quotation, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = $"Quotation {quotation.QuotationNumber} created successfully",
                    Data = new { quotation.Id, quotation.QuotationNumber }
                };
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning(ex, "Quotation number clash");
                return QuotationRules.Fail(409, "That quotation number was taken a moment ago. Please save again.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating quotation for customer {CustomerId}", request.CustomerId);
                return QuotationRules.Fail(500, "An error occurred while creating the quotation.");
            }
        }
    }
}
