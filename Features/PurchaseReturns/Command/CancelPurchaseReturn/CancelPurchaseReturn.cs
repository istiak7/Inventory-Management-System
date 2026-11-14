using FluentValidation;
using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Returns.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LedgerExtensions;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.PurchaseReturns.Command.CancelPurchaseReturn
{
    public class CancelPurchaseReturnCommand : IRequest<Result>
    {
        public int Id { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class CancelPurchaseReturnValidator : AbstractValidator<CancelPurchaseReturnCommand>
    {
        public CancelPurchaseReturnValidator()
        {
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Give a reason for cancelling the return.")
                .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer.");
        }
    }

    public class CancelPurchaseReturnEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/cancel-purchase-return/{id:int}", async (int id, CancelPurchaseReturnCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Purchase Return").RequirePermission(Permissions.PurchasesManage);
        }
    }

    // Undoes a purchase return entered by mistake: the goods come back into stock (serials
    // RmaReturned -> InStock) and the return's whole amount is owed on the bill again.
    // A refund the supplier paid and any settlement reversal stay as their own history.
    // Refused when a returned serial is no longer RmaReturned.
    public class CancelPurchaseReturnHandler(
        AppDbContext _dbContext,
        ILogger<CancelPurchaseReturnHandler> _logger
    ) : IRequestHandler<CancelPurchaseReturnCommand, Result>
    {
        public async Task<Result> Handle(CancelPurchaseReturnCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _dbContext.LockRowAsync<PurchaseReturn>(request.Id, cancellationToken);
                // The bill is locked too, so this cannot run beside cancelling the purchase.
                var purchaseId = await _dbContext.PurchaseReturns.AsNoTracking()
                    .Where(r => r.Id == request.Id).Select(r => (int?)r.PurchaseId).FirstOrDefaultAsync(cancellationToken);
                if (purchaseId is int lockedPurchaseId)
                    await _dbContext.LockRowAsync<SupplierPurchase>(lockedPurchaseId, cancellationToken);
                var purchaseReturn = await _dbContext.PurchaseReturns
                    .Include(r => r.SupplierPurchase)
                    .Include(r => r.Supplier)
                    .Include(r => r.Branch)
                    .Include(r => r.Lines).ThenInclude(l => l.ProductVariant)
                    .Include(r => r.Lines).ThenInclude(l => l.SupplierPurchaseDetails)
                    .Include(r => r.Lines).ThenInclude(l => l.Serials).ThenInclude(s => s.ProductSerial)
                    .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
                if (purchaseReturn == null)
                    return Error(404, "Return not found.");
                if (purchaseReturn.Status == ReturnStatus.Cancelled)
                    return Error(400, $"Return {purchaseReturn.ReturnNumber} is already cancelled.");
                if (purchaseReturn.SupplierPurchase.Status == PurchaseStatus.Cancelled)
                    return Error(400, $"Purchase {purchaseReturn.SupplierPurchase.InvoiceNumber} is cancelled, so this return can no longer be cancelled.");

                foreach (var serial in purchaseReturn.Lines.SelectMany(l => l.Serials).Select(s => s.ProductSerial))
                {
                    if (serial.Status != SerialStatus.RmaReturned)
                        return Error(400, $"Serial {serial.SerialNumber} is now {serial.Status}, so this return cannot be cancelled.");
                }

                await _dbContext.LockStocksAsync([purchaseReturn.BranchId], purchaseReturn.Lines.Select(l => l.ProductVariantId), cancellationToken);
                var now = DateTime.UtcNow;
                var stocks = new Dictionary<int, Stock>();

                // ---- Goods come back in.
                foreach (var line in purchaseReturn.Lines)
                {
                    var stock = await ReturnMath.GetOrCreateStockAsync(_dbContext, stocks, purchaseReturn.Branch, line.ProductVariant, cancellationToken);
                    stock.CurrentStock += line.Quantity;
                    await _dbContext.InventoryTransactions.AddAsync(new InventoryTransaction
                    {
                        BranchId = purchaseReturn.BranchId,
                        ProductVariantId = line.ProductVariantId,
                        SupplierPurchaseDetailsId = line.SupplierPurchaseDetailsId,
                        TransactionType = InventoryTxnType.PurchaseIn,
                        QuantityIn = line.Quantity,
                        QuantityOut = 0,
                        BalanceAfter = stock.CurrentStock,
                        TransactionDate = now,
                        Branch = purchaseReturn.Branch,
                        ProductVariant = line.ProductVariant,
                        SupplierPurchaseDetails = line.SupplierPurchaseDetails,
                    }, cancellationToken);
                    foreach (var returned in line.Serials)
                    {
                        returned.ProductSerial.Status = SerialStatus.InStock;
                        returned.ProductSerial.BranchId = purchaseReturn.BranchId;
                    }
                }

                // ---- Money: the return's amount is owed on the bill again.
                await _dbContext.LockRowAsync<Supplier>(purchaseReturn.SupplierId, cancellationToken);
                var balance = await _dbContext.SupplierTransactions
                    .Where(t => t.SupplierId == purchaseReturn.SupplierId)
                    .GetLatestBalanceAsync(cancellationToken);
                balance += purchaseReturn.TotalAmount;
                await _dbContext.SupplierTransactions.AddAsync(
                    SupplierTransaction.ForReturn(purchaseReturn, purchaseReturn.Supplier, -purchaseReturn.TotalAmount, now, balance),
                    cancellationToken);

                var purchase = purchaseReturn.SupplierPurchase;
                purchase.ReturnedAmount -= purchaseReturn.TotalAmount;
                purchase.DueAmount += purchaseReturn.TotalAmount;
                purchase.UpDatedAt = now;

                purchaseReturn.Status = ReturnStatus.Cancelled;
                purchaseReturn.CancelledAt = now;
                purchaseReturn.CancelReason = request.Reason.Trim();
                purchaseReturn.UpDatedAt = now;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = $"Return {purchaseReturn.ReturnNumber} cancelled. Stock is back and {purchaseReturn.TotalAmount:0.00} is owed on bill {purchase.InvoiceNumber} again.",
                    Data = new { purchaseReturn.Id, purchaseReturn.ReturnNumber, Status = purchaseReturn.Status.ToString() }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error cancelling purchase return {Id}", request.Id);
                return Error(500, "An error occurred while cancelling the return.");
            }
        }

        private static Result Error(int statusCode, string message) =>
            new() { IsSuccess = false, StatusCode = statusCode, Status = "Error", Message = message };
    }
}
