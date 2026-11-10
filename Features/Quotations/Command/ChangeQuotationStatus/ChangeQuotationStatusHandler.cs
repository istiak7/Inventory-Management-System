using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Quotations.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Quotations.Command.ChangeQuotationStatus
{
    // Moves a quotation along: Draft -> Sent -> Accepted / Rejected / Expired.
    // An expired offer cannot be accepted; edit it with a new validity date first.
    public class ChangeQuotationStatusHandler(
        AppDbContext _dbContext,
        ILogger<ChangeQuotationStatusHandler> _logger
    ) : IRequestHandler<ChangeQuotationStatusCommand, Result>
    {
        public async Task<Result> Handle(ChangeQuotationStatusCommand request, CancellationToken cancellationToken)
        {
            var target = Enum.Parse<QuotationStatus>(request.Status, ignoreCase: true);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _dbContext.LockRowAsync<Quotation>(request.Id, cancellationToken);

                var quotation = await _dbContext.Quotations
                    .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);
                if (quotation == null)
                    return QuotationRules.Fail(404, "Quotation not found.");

                var current = QuotationRules.EffectiveStatus(quotation.Status, quotation.ValidUntil);
                if (current == target)
                    return QuotationRules.Fail(400, $"The quotation is already {current}.");

                if (!QuotationRules.CanChange(current, target))
                {
                    var hint = current == QuotationStatus.Expired && target == QuotationStatus.Accepted
                        ? " Edit it and set a new valid-until date first."
                        : string.Empty;
                    return QuotationRules.Fail(400, $"A quotation that is {current} cannot be marked {target}.{hint}");
                }

                quotation.Status = target;
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var effective = QuotationRules.EffectiveStatus(quotation.Status, quotation.ValidUntil);
                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = $"Quotation {quotation.QuotationNumber} marked {effective}",
                    Data = new { quotation.Id, Status = effective.ToString() }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error changing status of quotation {QuotationId}", request.Id);
                return QuotationRules.Fail(500, "An error occurred while changing the quotation status.");
            }
        }
    }
}
