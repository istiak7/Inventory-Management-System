using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Transfers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory_Management_System.Features.Transfers.Command.RejectStockTransfer
{
    public class RejectStockTransferHandler(
        AppDbContext _dbContext,
        ILogger<RejectStockTransferHandler> _logger
    ) : IRequestHandler<RejectStockTransferCommand, Result>
    {
        public async Task<Result> Handle(RejectStockTransferCommand request, CancellationToken cancellationToken)
        {
            var transfer = await _dbContext.StockTransfers
                .Include(t => t.SourceBranch)
                .Include(t => t.DestinationBranch)
                .Include(t => t.StockTransferDetails).ThenInclude(d => d.ProductVariant).ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(t => t.Id == request.StockTransferId, cancellationToken);

            if (transfer == null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Stock transfer not found." };

            if (transfer.Status is not (TransferStatus.Draft or TransferStatus.Pending))
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Only a draft or pending transfer can be rejected." };

            try
            {
                transfer.Status = TransferStatus.Rejected;
                transfer.RejectedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);

                var lineResponses = transfer.StockTransferDetails.Select(line => new StockTransferLineResponse(
                    line.Id, line.ProductVariantId, line.ProductVariant.SKU, line.ProductVariant.Product.ProductName,
                    line.ProductVariant.IsSerialized, line.Quantity,
                    JsonSerializer.Deserialize<List<string>>(line.RequestedSerialNumbersJson) ?? [])).ToList();

                var response = new StockTransferResponse(
                    transfer.Id, transfer.Reference, transfer.SourceBranchId, transfer.SourceBranch.Name,
                    transfer.DestinationBranchId, transfer.DestinationBranch.Name, transfer.Status.ToString(),
                    transfer.Notes, transfer.CreatedAt, transfer.SubmittedAt, transfer.ApprovedAt, transfer.RejectedAt,
                    lineResponses.Count, lineResponses.Sum(l => l.Quantity), lineResponses);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Stock transfer rejected successfully", Data = response };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting stock transfer {Id}", request.StockTransferId);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while rejecting the stock transfer." };
            }
        }
    }
}
