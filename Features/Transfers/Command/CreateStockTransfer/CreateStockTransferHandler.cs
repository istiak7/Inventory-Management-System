using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Transfers.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Inventory_Management_System.Features.Transfers.Command.CreateStockTransfer
{
    public class CreateStockTransferHandler(
        AppDbContext _dbContext,
        ILogger<CreateStockTransferHandler> _logger
    ) : IRequestHandler<CreateStockTransferCommand, Result>
    {
        public async Task<Result> Handle(CreateStockTransferCommand request, CancellationToken cancellationToken)
        {
            var headerError = TransferLineBuilder.CheckHeader(request.SourceBranchId, request.DestinationBranchId, request.Items);
            if (headerError != null)
                return headerError;

            var sourceBranch = await _dbContext.Branches.FirstOrDefaultAsync(b => b.Id == request.SourceBranchId, cancellationToken);
            if (sourceBranch == null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Source branch not found." };

            var destinationBranch = await _dbContext.Branches.FirstOrDefaultAsync(b => b.Id == request.DestinationBranchId, cancellationToken);
            if (destinationBranch == null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Destination branch not found." };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;

                var transfer = new StockTransfer
                {
                    Reference = await GenerateReferenceAsync(now, cancellationToken),
                    SourceBranchId = request.SourceBranchId,
                    DestinationBranchId = request.DestinationBranchId,
                    Status = request.Dispatch ? TransferStatus.Pending : TransferStatus.Draft,
                    Notes = request.Notes,
                    SubmittedAt = request.Dispatch ? now : null,
                    SourceBranch = sourceBranch,
                    DestinationBranch = destinationBranch,
                };

                var (built, lineError) = await TransferLineBuilder.BuildAsync(
                    _dbContext, transfer, request.SourceBranchId, request.Items, cancellationToken);
                if (lineError != null)
                    return lineError;

                await _dbContext.StockTransfers.AddAsync(transfer, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = TransferLineBuilder.ToResponse(transfer, sourceBranch.Name, destinationBranch.Name, built!);

                return new Result { IsSuccess = true, StatusCode = 201, Status = "Success", Message = "Stock transfer created successfully", Data = response };
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning(ex, "Stock transfer creation lost a uniqueness race on Reference");
                return new Result { IsSuccess = false, StatusCode = 409, Status = "Error", Message = "This transfer clashed with another one saved at the same moment. Please try again." };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating stock transfer");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while creating the stock transfer." };
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

        private Task<string> GenerateReferenceAsync(DateTime date, CancellationToken cancellationToken) =>
            DocumentNumbers.NextAsync(
                _dbContext,
                _dbContext.StockTransfers.IgnoreQueryFilters().Select(t => t.Reference),
                $"TR-{BusinessClock.ToLocal(date).Year}-",
                cancellationToken);
    }
}
