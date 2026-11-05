using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Transfers.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Transfers.Command.UpdateStockTransfer
{
    public class UpdateStockTransferHandler(
        AppDbContext _dbContext,
        ILogger<UpdateStockTransferHandler> _logger
    ) : IRequestHandler<UpdateStockTransferCommand, Result>
    {
        public async Task<Result> Handle(UpdateStockTransferCommand request, CancellationToken cancellationToken)
        {
            var headerError = TransferLineBuilder.CheckHeader(request.SourceBranchId, request.DestinationBranchId, request.Items);
            if (headerError != null)
                return headerError;

            var sourceBranch = await _dbContext.Branches.FirstOrDefaultAsync(b => b.Id == request.SourceBranchId, cancellationToken);
            if (sourceBranch == null)
                return Error(404, "Source branch not found.");

            var destinationBranch = await _dbContext.Branches.FirstOrDefaultAsync(b => b.Id == request.DestinationBranchId, cancellationToken);
            if (destinationBranch == null)
                return Error(404, "Destination branch not found.");

            // Lock the transfer so an approve or reject at the same moment waits for this edit.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _dbContext.LockRowAsync<StockTransfer>(request.Id, cancellationToken);

                var transfer = await _dbContext.StockTransfers
                    .Include(t => t.StockTransferDetails)
                    .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

                if (transfer == null)
                    return Error(404, "Stock transfer not found.");

                if (transfer.Status is not (TransferStatus.Draft or TransferStatus.Pending))
                    return Error(400, $"Transfer {transfer.Reference} is {transfer.Status} and can no longer be edited.");

                // The lines are rebuilt from the request; nothing else points at them before approval.
                _dbContext.StockTransferDetails.RemoveRange(transfer.StockTransferDetails);
                transfer.StockTransferDetails.Clear();

                transfer.SourceBranchId = sourceBranch.Id;
                transfer.SourceBranch = sourceBranch;
                transfer.DestinationBranchId = destinationBranch.Id;
                transfer.DestinationBranch = destinationBranch;
                transfer.Notes = request.Notes;

                if (request.Dispatch)
                {
                    transfer.Status = TransferStatus.Pending;
                    transfer.SubmittedAt ??= DateTime.UtcNow;
                }
                else
                {
                    transfer.Status = TransferStatus.Draft;
                    transfer.SubmittedAt = null;
                }

                var (built, lineError) = await TransferLineBuilder.BuildAsync(
                    _dbContext, transfer, request.SourceBranchId, request.Items, cancellationToken);
                if (lineError != null)
                    return lineError;

                transfer.UpDatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = TransferLineBuilder.ToResponse(transfer, sourceBranch.Name, destinationBranch.Name, built!);
                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Stock transfer updated successfully", Data = response };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error updating stock transfer {Id}", request.Id);
                return Error(500, "An error occurred while updating the stock transfer.");
            }
        }

        private static Result Error(int statusCode, string message) =>
            new() { IsSuccess = false, StatusCode = statusCode, Status = "Error", Message = message };
    }
}
