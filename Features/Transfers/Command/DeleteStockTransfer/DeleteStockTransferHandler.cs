using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Transfers.Command.DeleteStockTransfer
{
    // Removes a draft or pending transfer. Nothing has moved yet (stock only moves on approve)
    // and nothing references it, so it is removed outright. An approved or rejected transfer
    // stays as the record of what happened.
    public class DeleteStockTransferHandler(
        AppDbContext _dbContext,
        ILogger<DeleteStockTransferHandler> _logger
    ) : IRequestHandler<DeleteStockTransferCommand, Result>
    {
        public async Task<Result> Handle(DeleteStockTransferCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _dbContext.LockRowAsync<StockTransfer>(request.StockTransferId, cancellationToken);

                var transfer = await _dbContext.StockTransfers
                    .Include(t => t.StockTransferDetails)
                    .FirstOrDefaultAsync(t => t.Id == request.StockTransferId, cancellationToken);

                if (transfer == null)
                    return Error(404, "Stock transfer not found.");

                if (transfer.Status is not (TransferStatus.Draft or TransferStatus.Pending))
                    return Error(400, $"Transfer {transfer.Reference} is {transfer.Status} and can no longer be deleted.");

                _dbContext.StockTransferDetails.RemoveRange(transfer.StockTransferDetails);
                _dbContext.StockTransfers.Remove(transfer);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = $"Transfer {transfer.Reference} deleted successfully" };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error deleting stock transfer {Id}", request.StockTransferId);
                return Error(500, "An error occurred while deleting the stock transfer.");
            }
        }

        private static Result Error(int statusCode, string message) =>
            new() { IsSuccess = false, StatusCode = statusCode, Status = "Error", Message = message };
    }
}
