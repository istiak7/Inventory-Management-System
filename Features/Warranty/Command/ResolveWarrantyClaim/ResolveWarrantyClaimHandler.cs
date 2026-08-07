using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Warranty.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Warranty.Command.ResolveWarrantyClaim
{
    // Where a claim ends well. Repaired touches nothing but the claim — the unit never left the
    // customer's ownership.
    //
    // Replaced is the only path in the system that hands a customer a unit without a sale, and it
    // is deliberately NOT modelled as a zero-price sale: a warranty swap is not revenue and must
    // never reach the sales books or the customer ledger. Instead, in one transaction:
    //   - the replacement leaves stock (one ReturnOut ledger row, CurrentStock - 1)
    //   - it inherits the ORIGINAL unit's SoldDate, so the customer's warranty clock keeps running
    //     from the day they first bought rather than restarting for free
    //   - the defective unit becomes RmaReturned: no stock row, because it never re-enters the
    //     sellable pool, and its serial status alone keeps it out
    //   - the original sale line is left untouched; this claim row IS the record of the swap
    public class ResolveWarrantyClaimHandler(
            AppDbContext _dbContext,
            ILogger<ResolveWarrantyClaimHandler> _logger
        ) : IRequestHandler<ResolveWarrantyClaimCommand, Result>
    {
        public async Task<Result> Handle(ResolveWarrantyClaimCommand request, CancellationToken cancellationToken)
        {
            var resolution = Enum.Parse<WarrantyResolutionType>(request.Resolution, true);   // validator restricts this to Repaired/Replaced

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var claim = await _dbContext.WarrantyClaims
                    .Include(c => c.ProductSerial)
                    .FirstOrDefaultAsync(c => c.Id == request.WarrantyClaimId, cancellationToken);

                if (claim == null)
                    return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Warranty claim not found." };

                // Open is allowed as well as InRepair: a counter swap that needs no bench time
                // shouldn't have to fake a repair step first.
                if (!claim.IsOpen)
                    return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Claim {claim.ClaimNumber} is already {claim.Status} and cannot be resolved again." };

                var now = DateTime.UtcNow;
                ProductSerial? replacement = null;

                if (resolution == WarrantyResolutionType.Replaced)
                {
                    var replacementSerialNumber = request.ReplacementSerialNumber!.Trim();
                    var original = claim.ProductSerial;

                    // Same variant, same branch, still on the shelf — re-checked now rather than
                    // trusted from whenever the picker was populated, since it may have sold since.
                    replacement = await _dbContext.ProductSerials
                        .Include(s => s.ProductVariant).ThenInclude(v => v.Product)
                        .FirstOrDefaultAsync(s =>
                            s.SerialNumber == replacementSerialNumber &&
                            s.ProductVariantId == original.ProductVariantId &&
                            s.BranchId == claim.BranchId &&
                            s.Status == SerialStatus.InStock, cancellationToken);

                    if (replacement == null)
                        return new Result { IsSuccess = false, StatusCode = 409, Status = "Error", Message = $"Serial '{replacementSerialNumber}' is not an available in-stock unit of the same product at this branch. Pick another unit." };

                    var branch = await _dbContext.Branches.FirstAsync(b => b.Id == claim.BranchId, cancellationToken);

                    var stock = await _dbContext.Stocks
                        .FirstOrDefaultAsync(s => s.BranchId == claim.BranchId && s.ProductVariantId == original.ProductVariantId, cancellationToken);

                    if (stock == null || stock.CurrentStock < 1)
                        return new Result { IsSuccess = false, StatusCode = 409, Status = "Error", Message = $"No stock on hand for '{replacement.ProductVariant.Product.ProductName}' at this branch, so no unit can be issued as a replacement." };

                    var newBalance = stock.CurrentStock - 1;
                    stock.CurrentStock = newBalance;

                    await _dbContext.InventoryTransactions.AddAsync(new InventoryTransaction
                    {
                        BranchId = claim.BranchId,
                        ProductVariantId = original.ProductVariantId,
                        // The replacement's own lot, so its cost stays recoverable — a warranty
                        // swap has no revenue but it very much has a cost.
                        SupplierPurchaseDetailsId = replacement.SupplierPurchaseDetailsId,
                        TransactionType = InventoryTxnType.ReturnOut,
                        QuantityIn = 0,
                        QuantityOut = 1,
                        BalanceAfter = newBalance,
                        TransactionDate = now,
                        Branch = branch,
                        ProductVariant = replacement.ProductVariant,
                    }, cancellationToken);

                    // Warranty continuity: the replacement expires the day the original would have.
                    replacement.Status = SerialStatus.Sold;
                    replacement.SoldDate = original.SoldDate ?? now;

                    original.Status = SerialStatus.RmaReturned;

                    claim.ReplacementSerialId = replacement.Id;
                }

                claim.Status = WarrantyClaimStatus.Resolved;
                claim.Resolution = resolution;
                claim.ResolvedAt = now;
                if (!string.IsNullOrWhiteSpace(request.Notes))
                    claim.ResolutionNotes = request.Notes.Trim();

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var row = await _dbContext.WarrantyClaims
                    .AsNoTracking()
                    .Where(c => c.Id == claim.Id)
                    .ProjectToRow()
                    .FirstAsync(cancellationToken);

                var message = resolution == WarrantyResolutionType.Replaced
                    ? $"Claim {claim.ClaimNumber} resolved — unit replaced with serial {replacement!.SerialNumber}."
                    : $"Claim {claim.ClaimNumber} resolved — unit repaired.";

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = message, Data = row.ToResponse() };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error resolving warranty claim {ClaimId} as {Resolution}", request.WarrantyClaimId, request.Resolution);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while resolving the warranty claim." };
            }
        }
    }
}
