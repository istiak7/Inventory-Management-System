using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Quotations.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Quotations.Command.DeleteQuotation
{
    // Hard delete, like a draft transfer: a quotation moved no stock and no money, and nothing
    // points at it. Accepted and Converted quotations are kept (the sale refers to the latter).
    public class DeleteQuotationHandler(
        AppDbContext _dbContext,
        ILogger<DeleteQuotationHandler> _logger
    ) : IRequestHandler<DeleteQuotationCommand, Result>
    {
        public async Task<Result> Handle(DeleteQuotationCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _dbContext.LockRowAsync<Quotation>(request.Id, cancellationToken);

                var quotation = await _dbContext.Quotations
                    .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);
                if (quotation == null)
                    return QuotationRules.Fail(404, "Quotation not found.");

                var current = QuotationRules.EffectiveStatus(quotation.Status, quotation.ValidUntil);
                if (!QuotationRules.CanDelete(current))
                    return QuotationRules.Fail(400, $"A quotation that is {current} cannot be deleted.");

                // Lines go with it (cascade).
                _dbContext.Quotations.Remove(quotation);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = $"Quotation {quotation.QuotationNumber} deleted successfully"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error deleting quotation {QuotationId}", request.Id);
                return QuotationRules.Fail(500, "An error occurred while deleting the quotation.");
            }
        }
    }
}
