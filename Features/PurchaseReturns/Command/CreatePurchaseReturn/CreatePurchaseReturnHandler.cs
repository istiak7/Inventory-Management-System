using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Parties.Shared;
using Inventory_Management_System.Features.PurchaseReturns.Shared;
using Inventory_Management_System.Features.Returns.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LedgerExtensions;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.PurchaseReturns.Command.CreatePurchaseReturn
{
    // Sends some lines (some quantity of each) of one approved purchase back to the supplier.
    //
    // Stock: the units leave (ReturnOut); serials become RmaReturned.
    // Money, for return worth R (the lines' share of the bill total, discount included):
    //   1. what we still owe on the bill is reduced first, up to R;
    //   2. the rest we had already paid: a part paid by a cross-ledger Adjustment is reversed
    //      (the party owes us as a customer again, no cash moves);
    //   3. anything still left the supplier pays back in cash (a negative "Refund" payment).
    // The bill's TotalAmount stays; its ReturnedAmount grows by R.
    public class CreatePurchaseReturnHandler(
        AppDbContext _dbContext,
        ILogger<CreatePurchaseReturnHandler> _logger
    ) : IRequestHandler<CreatePurchaseReturnCommand, Result>
    {
        public async Task<Result> Handle(CreatePurchaseReturnCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _dbContext.LockRowAsync<SupplierPurchase>(request.PurchaseId, cancellationToken);
                var purchase = await _dbContext.SupplierPurchases
                    .Include(p => p.Supplier)
                    .Include(p => p.Branch)
                    .Include(p => p.SupplierPurchaseDetails).ThenInclude(d => d.ProductVariant).ThenInclude(v => v.Product)
                    .FirstOrDefaultAsync(p => p.Id == request.PurchaseId && p.IsActive != (int)EntityConstant.EntityStatus.Deleted, cancellationToken);
                if (purchase == null)
                    return Error(404, "Purchase not found.");
                if (purchase.Status != PurchaseStatus.Approved)
                    return Error(400, $"Purchase {purchase.InvoiceNumber} is {purchase.Status}. Only an approved purchase can be returned.");

                var returnDate = request.ReturnDate ?? DateTime.UtcNow;
                if (returnDate < purchase.PurchaseDate)
                    return Error(400, "The return date cannot be before the purchase date.");

                // Stock before anything reads it (document -> stock -> customer -> supplier).
                var variantIds = purchase.SupplierPurchaseDetails
                    .Where(d => request.Lines.Any(l => l.SupplierPurchaseDetailsId == d.Id))
                    .Select(d => d.ProductVariantId)
                    .ToList();
                if (variantIds.Count > 0)
                    await _dbContext.LockStocksAsync([purchase.BranchId], variantIds, cancellationToken);

                var eligibility = await PurchaseReturnEligibility.ForPurchaseAsync(_dbContext, purchase, cancellationToken);
                var picked = new List<(SupplierPurchaseDetails Line, int Quantity, List<ProductSerial> Serials)>();
                foreach (var item in request.Lines)
                {
                    var line = purchase.SupplierPurchaseDetails.FirstOrDefault(d => d.Id == item.SupplierPurchaseDetailsId);
                    if (line == null)
                        return Error(400, $"Line {item.SupplierPurchaseDetailsId} is not on purchase {purchase.InvoiceNumber}.");
                    var rule = eligibility[line.Id];
                    var name = line.ProductVariant.Product.ProductName;
                    if (rule.BlockedReason != null)
                        return Error(400, $"'{name}' cannot be returned: {rule.BlockedReason}");
                    if (item.Quantity > rule.Remaining)
                        return Error(400, $"'{name}': only {rule.Remaining} can be returned (received {rule.Received}, already returned {rule.Returned}, in stock {rule.InStock}).");

                    var serials = new List<ProductSerial>();
                    if (line.ProductVariant.IsSerialized)
                    {
                        var numbers = (item.SerialNumbers ?? []).Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().ToList();
                        if (numbers.Count != item.Quantity)
                            return Error(400, $"'{name}' is serialized: pick exactly {item.Quantity} serial number(s) to return.");
                        var unknown = numbers.Except(rule.AvailableSerials).ToList();
                        if (unknown.Count > 0)
                            return Error(400, $"'{name}': these serial numbers are not in stock from this bill: {string.Join(", ", unknown)}.");
                        serials = await _dbContext.ProductSerials
                            .IgnoreQueryFilters()
                            .Where(s => s.SupplierPurchaseDetailsId == line.Id && numbers.Contains(s.SerialNumber))
                            .ToListAsync(cancellationToken);
                    }
                    picked.Add((line, item.Quantity, serials));
                }

                // ---- What it is worth: the lines' share of the bill total.
                var grosses = picked.Select(p => p.Line.UnitPrice * p.Quantity).ToList();
                var thisGross = grosses.Sum();
                var returnedGross = await PurchaseReturnEligibility.ReturnedGrossAsync(_dbContext, purchase.Id, cancellationToken);
                var billGross = purchase.TotalAmount + purchase.DiscountAmount;
                var everythingBack = purchase.SupplierPurchaseDetails
                    .Where(d => d.Status != LineStatus.Rejected)
                    .All(d => (d.ReceivedQuantity ?? 0) - eligibility[d.Id].Returned
                              - picked.Where(p => p.Line.Id == d.Id).Sum(p => p.Quantity) == 0);
                var total = ReturnMath.ReturnTotal(thisGross, returnedGross, billGross, purchase.TotalAmount,
                    purchase.TotalAmount - purchase.ReturnedAmount, everythingBack);
                var lineAmounts = ReturnMath.Split(total, grosses);

                var year = BusinessClock.ToLocal(returnDate).Year;
                var purchaseReturn = new PurchaseReturn
                {
                    ReturnNumber = await DocumentNumbers.NextAsync(
                        _dbContext, _dbContext.PurchaseReturns.IgnoreQueryFilters().Select(r => r.ReturnNumber),
                        $"PR-{year}-", cancellationToken),
                    PurchaseId = purchase.Id,
                    SupplierId = purchase.SupplierId,
                    BranchId = purchase.BranchId,
                    ReturnDate = returnDate,
                    Status = ReturnStatus.Completed,
                    SubTotal = thisGross,
                    TotalAmount = total,
                    DiscountAmount = thisGross - total,
                    Reason = request.Reason.Trim(),
                    Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim(),
                    SupplierPurchase = purchase,
                    Supplier = purchase.Supplier,
                    Branch = purchase.Branch,
                };

                // ---- Stock and serials leave.
                var stocks = new Dictionary<int, Stock>();
                for (var i = 0; i < picked.Count; i++)
                {
                    var (line, quantity, serials) = picked[i];
                    var returnLine = new PurchaseReturnLine
                    {
                        SupplierPurchaseDetailsId = line.Id,
                        ProductVariantId = line.ProductVariantId,
                        Quantity = quantity,
                        UnitPrice = line.UnitPrice,
                        Amount = lineAmounts[i],
                        PurchaseReturn = purchaseReturn,
                        SupplierPurchaseDetails = line,
                        ProductVariant = line.ProductVariant,
                    };
                    foreach (var serial in serials)
                    {
                        serial.Status = SerialStatus.RmaReturned;
                        returnLine.Serials.Add(new PurchaseReturnSerial
                        {
                            PurchaseReturnLine = returnLine,
                            ProductSerial = serial,
                        });
                    }
                    purchaseReturn.Lines.Add(returnLine);

                    var stock = await ReturnMath.GetOrCreateStockAsync(_dbContext, stocks, purchase.Branch, line.ProductVariant, cancellationToken);
                    stock.CurrentStock -= quantity;
                    await _dbContext.InventoryTransactions.AddAsync(new InventoryTransaction
                    {
                        BranchId = purchase.BranchId,
                        ProductVariantId = line.ProductVariantId,
                        SupplierPurchaseDetailsId = line.Id,
                        TransactionType = InventoryTxnType.ReturnOut,
                        QuantityIn = 0,
                        QuantityOut = quantity,
                        BalanceAfter = stock.CurrentStock,
                        TransactionDate = returnDate,
                        Branch = purchase.Branch,
                        ProductVariant = line.ProductVariant,
                        SupplierPurchaseDetails = line,
                    }, cancellationToken);
                }
                await _dbContext.PurchaseReturns.AddAsync(purchaseReturn, cancellationToken);

                // ---- Money. A settlement reversal touches the linked customer, which is locked
                // before the supplier (lock order).
                var linkedCustomerId = await _dbContext.Customers
                    .Where(c => c.SupplierId == purchase.SupplierId)
                    .Select(c => (int?)c.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                if (linkedCustomerId is int customerId)
                    await _dbContext.LockRowAsync<Customer>(customerId, cancellationToken);
                await _dbContext.LockRowAsync<Supplier>(purchase.SupplierId, cancellationToken);
                var balance = await _dbContext.SupplierTransactions
                    .Where(t => t.SupplierId == purchase.SupplierId)
                    .GetLatestBalanceAsync(cancellationToken);

                var dueReduced = Math.Min(total, purchase.DueAmount);
                balance -= total;
                await _dbContext.SupplierTransactions.AddAsync(
                    SupplierTransaction.ForReturn(purchaseReturn, purchase.Supplier, total, returnDate, balance), cancellationToken);

                var paidPart = total - dueReduced;
                var (reversed, balanceAfterReversal) = await new CrossLedgerSettlement(_dbContext).ReverseOnPurchaseAsync(
                    purchase, paidPart, balance, returnDate,
                    $"Settlement reversed: purchase return {purchaseReturn.ReturnNumber}", cancellationToken);
                balance = balanceAfterReversal;

                var cash = paidPart - reversed;
                if (cash > 0)
                {
                    var refund = new SupplierPayment
                    {
                        SupplierId = purchase.SupplierId,
                        BranchId = purchase.BranchId,
                        Amount = -cash,
                        PaymentDate = returnDate,
                        PaymentMethod = "Refund",
                        Remarks = $"Refund from supplier for return {purchaseReturn.ReturnNumber} (bill {purchase.InvoiceNumber})",
                        Supplier = purchase.Supplier,
                        Branch = purchase.Branch,
                    };
                    await _dbContext.SupplierPayments.AddAsync(refund, cancellationToken);
                    await _dbContext.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
                    {
                        Amount = -cash,
                        AllocationDate = returnDate,
                        SupplierPurchase = purchase,
                        SupplierPayment = refund,
                    }, cancellationToken);
                    purchase.ReversePayment(cash);
                    balance += cash;
                    await _dbContext.SupplierTransactions.AddAsync(
                        SupplierTransaction.ForRefund(refund, purchase.Supplier, cash, balance, purchase, purchaseReturn), cancellationToken);
                }

                // Reversal and refund each put their part back on the due; the whole return then
                // comes off it. Net: the due drops by dueReduced.
                purchase.DueAmount -= total;
                purchase.ReturnedAmount += total;
                purchase.UpDatedAt = DateTime.UtcNow;

                purchaseReturn.DueReduced = dueReduced;
                purchaseReturn.SettlementReversed = reversed;
                purchaseReturn.CashRefunded = cash;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = $"Return {purchaseReturn.ReturnNumber} saved: {total:0.00}"
                              + (dueReduced > 0 ? $", {dueReduced:0.00} taken off what we owe" : "")
                              + (reversed > 0 ? $", {reversed:0.00} customer settlement reversed" : "")
                              + (cash > 0 ? $", {cash:0.00} to collect back from the supplier" : "")
                              + ".",
                    Data = new
                    {
                        purchaseReturn.Id,
                        purchaseReturn.ReturnNumber,
                        purchaseReturn.TotalAmount,
                        purchaseReturn.DueReduced,
                        purchaseReturn.SettlementReversed,
                        purchaseReturn.CashRefunded,
                    }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating purchase return for purchase {PurchaseId}", request.PurchaseId);
                return Error(500, "An error occurred while saving the return.");
            }
        }

        private static Result Error(int statusCode, string message) =>
            new() { IsSuccess = false, StatusCode = statusCode, Status = "Error", Message = message };
    }
}
