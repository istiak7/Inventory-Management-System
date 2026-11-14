using System.Text.Json;
using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Parties.Shared;
using Inventory_Management_System.Features.Returns.Shared;
using Inventory_Management_System.Features.Sales.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LedgerExtensions;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Inventory_Management_System.Features.Sales.Command.EditSale
{
    // Edits a completed sale without rewriting history. Everything below runs in ONE transaction.
    //
    //   Phase A  load the sale, its lines, payments, settlements; lock document -> stock -> customer
    //   Phase B  undo: a Credit sale's cross-ledger settlements are reversed (reversal rows in both
    //            ledgers); actual cash payments are never touched
    //   Phase C  apply the new lines, prices, discount and tax. Stock moves only by the difference,
    //            as "Adjustment" inventory rows. A line that is dropped is voided (it stays on the
    //            record); the sale's earlier returns are left exactly as they are
    //   Phase D  redo: due = new total - paid - returned. Overpaid -> the surplus payment is released
    //            from the invoice and stays the customer's advance (or is refunded in cash if asked);
    //            money released earlier is put back first; a Cash sale must be settled by a real
    //            payment; a Credit sale then runs cross-ledger FIFO again on what is still due
    //   Audit    one SaleEdits row (before / after / what moved) next to the ledger, payment,
    //            settlement and inventory rows the edit wrote
    public class EditSaleHandler(
        AppDbContext _dbContext,
        ILogger<EditSaleHandler> _logger
    ) : IRequestHandler<EditSaleCommand, Result>
    {
        private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

        public async Task<Result> Handle(EditSaleCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // ---------------------------------------------------------------- Phase A: load + lock
                // Lock order: document -> stock -> customer -> supplier (see RowLockExtensions).
                await _dbContext.LockRowAsync<CustomerSale>(request.Id, cancellationToken);
                var sale = await _dbContext.CustomerSales
                    .Include(s => s.Customer)
                    .Include(s => s.Branch)
                    .Include(s => s.SaleDetails).ThenInclude(d => d.ProductVariant).ThenInclude(v => v.Product)
                    .Include(s => s.SaleDetails).ThenInclude(d => d.ProductSerial)
                    .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
                if (sale == null)
                    return Error(404, "Sale not found.");
                if (sale.Status == SaleStatus.Voided)
                    return Error(400, $"Sale {sale.InvoiceNumber} is voided and cannot be edited.");

                var existing = sale.SaleDetails.Where(d => d.Status == SaleLineStatus.Completed).ToDictionary(d => d.Id);

                // Which lines the request keeps, and that it does not name one twice or a stranger.
                var named = new HashSet<int>();
                foreach (var item in request.Items.Where(i => i.SaleDetailsId.HasValue))
                {
                    var lineId = item.SaleDetailsId!.Value;
                    if (!existing.ContainsKey(lineId))
                        return Error(400, $"Line {lineId} is not on sale {sale.InvoiceNumber}.");
                    if (!named.Add(lineId))
                        return Error(400, $"Line {lineId} is listed more than once.");
                }
                var removed = existing.Values.Where(d => !named.Contains(d.Id)).ToList();

                var variantIds = existing.Values.Select(d => d.ProductVariantId)
                    .Concat(request.Items.Select(i => i.ProductVariantId)).Distinct().ToList();
                await _dbContext.LockStocksAsync([sale.BranchId], variantIds, cancellationToken);

                var beforeLines = existing.Values.Select(Snapshot).ToList();
                var old = new
                {
                    sale.SubTotal, sale.DiscountAmount, sale.TaxAmount, sale.TotalAmount, sale.PaidAmount, sale.DueAmount,
                };

                var now = DateTime.UtcNow;
                var costResolver = new ProductCostResolver(_dbContext);
                var soldDelta = new Dictionary<int, int>();           // variant -> units more (+) / fewer (-) on the invoice
                var variants = new Dictionary<int, ProductVariant>();
                var releasedSerialIds = new List<int>();
                var claimedSerialIds = new HashSet<int>();

                // ---------------------------------------------------------------- Phase C: lines
                // Dropped lines: only when nothing of them has been returned or claimed.
                foreach (var line in removed)
                {
                    var name = line.ProductVariant.Product.ProductName;
                    if (line.ReturnedQuantity > 0)
                        return Error(400, $"'{name}' has {line.ReturnedQuantity} unit(s) returned, so its line cannot be removed. You can reduce it down to the returned quantity.");
                    if (await _dbContext.WarrantyClaims.IgnoreQueryFilters().AnyAsync(c => c.SaleDetailsId == line.Id, cancellationToken))
                        return Error(400, $"'{name}' has a warranty claim, so its line cannot be removed.");

                    soldDelta[line.ProductVariantId] = soldDelta.GetValueOrDefault(line.ProductVariantId) - line.Quantity;
                    variants[line.ProductVariantId] = line.ProductVariant;
                    line.Status = SaleLineStatus.Voided;
                    if (line.ProductSerial is { } serial)
                    {
                        serial.Status = SerialStatus.InStock;
                        serial.SoldDate = null;
                        releasedSerialIds.Add(serial.Id);
                    }
                }

                // A replaced serial is released before any new one is claimed, so two units can swap.
                // The lines let go of their old serial and that is saved first: a serial may be on
                // only one live line (unique index), so a swap cannot be written in a single step.
                var letGo = false;
                foreach (var item in request.Items.Where(i => i.SaleDetailsId.HasValue))
                {
                    var line = existing[item.SaleDetailsId!.Value];
                    var wanted = item.SerialNumber?.Trim();
                    if (line.ProductSerial != null && !string.IsNullOrEmpty(wanted)
                        && !string.Equals(wanted, line.ProductSerial.SerialNumber, StringComparison.OrdinalIgnoreCase)
                        && line.ReturnedQuantity == 0)
                    {
                        line.ProductSerial.Status = SerialStatus.InStock;
                        line.ProductSerial.SoldDate = null;
                        releasedSerialIds.Add(line.ProductSerial.Id);
                        line.ProductSerial = null;
                        line.ProductSerialId = null;
                        letGo = true;
                    }
                }
                if (letGo)
                    await _dbContext.SaveChangesAsync(cancellationToken);

                foreach (var item in request.Items)
                {
                    if (item.SaleDetailsId is int lineId)
                    {
                        var error = await EditLineAsync(sale, existing[lineId], item, soldDelta, variants, releasedSerialIds,
                            claimedSerialIds, costResolver, cancellationToken);
                        if (error != null) return error;
                    }
                    else
                    {
                        var error = await AddLineAsync(sale, item, soldDelta, variants, releasedSerialIds,
                            claimedSerialIds, costResolver, cancellationToken);
                        if (error != null) return error;
                    }
                }

                var liveLines = sale.SaleDetails.Where(d => d.Status == SaleLineStatus.Completed).ToList();
                if (liveLines.Count == 0)
                    return Error(400, "An invoice needs at least one line. To take everything back, use Sales Return.");

                var subTotal = liveLines.Sum(d => d.TotalAmount);
                if (request.DiscountAmount > subTotal)
                    return Error(400, $"DiscountAmount {request.DiscountAmount} exceeds the subtotal {subTotal}.");
                var newTotal = subTotal - request.DiscountAmount + request.TaxAmount;
                var returned = sale.ReturnedAmount;
                if (newTotal < returned)
                    return Error(400, $"The new total {newTotal:0.00} is less than the {returned:0.00} already returned on this invoice.");

                // Stock moves by the net difference only, each as a traceable Adjustment row.
                var stockMoves = new List<object>();
                var stocks = new Dictionary<int, Stock>();
                foreach (var (variantId, delta) in soldDelta.Where(kv => kv.Value != 0))
                {
                    var variant = variants[variantId];
                    var stock = await ReturnMath.GetOrCreateStockAsync(_dbContext, stocks, sale.Branch, variant, cancellationToken);
                    if (delta > 0 && stock.CurrentStock < delta)
                        return Error(400, $"Insufficient stock for '{variant.Product.ProductName}' (SKU {variant.SKU}): {stock.CurrentStock} on hand, {delta} more needed.");

                    stock.CurrentStock -= delta;
                    await _dbContext.InventoryTransactions.AddAsync(new InventoryTransaction
                    {
                        BranchId = sale.BranchId,
                        ProductVariantId = variantId,
                        TransactionType = InventoryTxnType.Adjustment,
                        QuantityIn = delta < 0 ? -delta : 0,
                        QuantityOut = delta > 0 ? delta : 0,
                        BalanceAfter = stock.CurrentStock,
                        TransactionDate = now,
                        Branch = sale.Branch,
                        ProductVariant = variant,
                    }, cancellationToken);
                    stockMoves.Add(new { variantId, variant.SKU, productName = variant.Product.ProductName, change = -delta, balanceAfter = stock.CurrentStock });
                }

                // ---------------------------------------------------------------- Phase B: undo settlements
                var customer = sale.Customer;
                await _dbContext.LockRowAsync<Customer>(customer.Id, cancellationToken);
                var balance = await _dbContext.CustomerTransactions
                    .Where(t => t.CustomerId == customer.Id)
                    .GetLatestBalanceAsync(cancellationToken);

                var allocations = await _dbContext.SaleCustomerPayments.IgnoreQueryFilters()
                    .Where(a => a.SaleId == sale.Id)
                    .Select(a => new { a.CustomerPaymentId, a.Amount, a.CustomerPayment.PaymentMethod })
                    .ToListAsync(cancellationToken);

                decimal settlementReversed = 0;
                var settled = allocations.Where(a => a.PaymentMethod == PartySettlement.PaymentMethod).Sum(a => a.Amount);
                if (sale.SaleType == SaleType.Debit && settled > 0)
                {
                    var reversal = await new CrossLedgerSettlement(_dbContext).ReverseOnSaleAsync(
                        sale, settled, balance, now, $"Settlement undone: invoice {sale.InvoiceNumber} edited", cancellationToken);
                    settlementReversed = reversal.Reversed;
                    balance = reversal.CustomerBalance;
                }

                // ---------------------------------------------------------------- Phase D: redo
                var delta2 = newTotal - sale.TotalAmount;
                if (delta2 != 0)
                {
                    balance += delta2;
                    await _dbContext.CustomerTransactions.AddAsync(
                        CustomerTransaction.ForSaleEdit(sale, customer, delta2, now, balance), cancellationToken);
                }

                sale.SubTotal = subTotal;
                sale.DiscountAmount = request.DiscountAmount;
                sale.TaxAmount = request.TaxAmount;
                sale.TotalAmount = newTotal;
                sale.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim();
                sale.DueAmount = newTotal - sale.PaidAmount - returned;
                sale.UpDatedAt = now;

                // Cash payments are never deleted. If they now exceed what the invoice needs, the
                // surplus is released from it (the payment stays) and remains the customer's advance,
                // unless the editor asked for it to be handed back.
                decimal cashReleased = 0, cashRefunded = 0, cashReapplied = 0;
                if (sale.DueAmount < 0)
                {
                    var excess = -sale.DueAmount;
                    cashReleased = excess;

                    if (request.RefundExcess)
                    {
                        var refund = new CustomerPayment
                        {
                            CustomerId = sale.CustomerId,
                            BranchId = sale.BranchId,
                            Amount = -excess,
                            PaymentDate = now,
                            PaymentMethod = "Refund",
                            Remarks = $"Refund for edit of invoice {sale.InvoiceNumber}",
                            Customer = customer,
                            Branch = sale.Branch,
                        };
                        await _dbContext.CustomerPayments.AddAsync(refund, cancellationToken);
                        await _dbContext.SaleCustomerPayments.AddAsync(new SaleCustomerPayment
                        {
                            Amount = -excess,
                            AllocationDate = now,
                            CustomerSale = sale,
                            CustomerPayment = refund,
                        }, cancellationToken);
                        balance += excess;
                        await _dbContext.CustomerTransactions.AddAsync(
                            CustomerTransaction.ForRefund(refund, customer, excess, balance, sale), cancellationToken);
                        cashRefunded = excess;
                    }
                    else
                    {
                        // Newest cash payment first; each release is a negative allocation on that payment.
                        var net = allocations
                            .Where(a => a.PaymentMethod != PartySettlement.PaymentMethod && a.PaymentMethod != "Refund")
                            .GroupBy(a => a.CustomerPaymentId)
                            .Select(g => new { PaymentId = g.Key, Amount = g.Sum(a => a.Amount) })
                            .Where(x => x.Amount > 0)
                            .OrderByDescending(x => x.PaymentId)
                            .ToList();
                        var left = excess;
                        foreach (var p in net)
                        {
                            if (left <= 0) break;
                            var take = Math.Min(p.Amount, left);
                            var payment = await _dbContext.CustomerPayments.IgnoreQueryFilters()
                                .FirstAsync(x => x.Id == p.PaymentId, cancellationToken);
                            await _dbContext.SaleCustomerPayments.AddAsync(new SaleCustomerPayment
                            {
                                Amount = -take,
                                AllocationDate = now,
                                CustomerSale = sale,
                                CustomerPayment = payment,
                            }, cancellationToken);
                            left -= take;
                        }
                        if (left > 0)
                            return Error(500, "The invoice is overpaid by more than its cash payments cover; nothing was changed.");
                    }

                    sale.PaidAmount -= excess;
                    sale.DueAmount = 0;
                }
                else if (sale.DueAmount > 0)
                {
                    // Money released from this invoice by an earlier edit goes back on it first.
                    var pool = await _dbContext.CustomerPayments.IgnoreQueryFilters()
                        .Include(p => p.SaleCustomerPayments)
                        .Where(p => p.CustomerId == sale.CustomerId
                                    && p.Amount > 0
                                    && p.PaymentMethod != PartySettlement.PaymentMethod
                                    && p.PaymentMethod != "Refund"
                                    && p.SaleCustomerPayments.Any(a => a.SaleId == sale.Id))
                        .OrderBy(p => p.Id)
                        .ToListAsync(cancellationToken);
                    foreach (var payment in pool)
                    {
                        if (sale.DueAmount <= 0) break;
                        var free = payment.Amount - payment.SaleCustomerPayments.Sum(a => a.Amount);
                        var take = Math.Min(free, sale.DueAmount);
                        if (take <= 0) continue;
                        await _dbContext.SaleCustomerPayments.AddAsync(new SaleCustomerPayment
                        {
                            Amount = take,
                            AllocationDate = now,
                            CustomerSale = sale,
                            CustomerPayment = payment,
                        }, cancellationToken);
                        sale.ApplyPayment(take);
                        cashReapplied += take;
                    }
                }

                // New money. A Cash sale is settled only by real payments, so it has to be exact.
                var additional = request.AdditionalPayment ?? 0m;
                if (sale.SaleType == SaleType.Cash)
                {
                    if (additional != sale.DueAmount)
                        return Error(400, sale.DueAmount > 0
                            ? $"This edit adds {sale.DueAmount:0.00} to a Cash sale: collect exactly that as the additional payment."
                            : "No additional payment is needed for this edit.");
                }
                else if (additional > sale.DueAmount)
                {
                    return Error(400, $"The additional payment {additional:0.00} exceeds what is due on the invoice ({sale.DueAmount:0.00}).");
                }

                if (additional > 0)
                {
                    var cash = new CustomerPayment
                    {
                        CustomerId = sale.CustomerId,
                        BranchId = sale.BranchId,
                        Amount = additional,
                        PaymentDate = now,
                        PaymentMethod = SaleType.Cash.ToString(),
                        Remarks = $"Collected with edit of invoice {sale.InvoiceNumber}",
                        Customer = customer,
                        Branch = sale.Branch,
                    };
                    await _dbContext.CustomerPayments.AddAsync(cash, cancellationToken);
                    sale.ApplyPayment(additional);
                    await _dbContext.SaleCustomerPayments.AddAsync(new SaleCustomerPayment
                    {
                        Amount = additional,
                        AllocationDate = now,
                        CustomerSale = sale,
                        CustomerPayment = cash,
                    }, cancellationToken);
                    balance -= additional;
                    await _dbContext.CustomerTransactions.AddAsync(
                        CustomerTransaction.ForPayment(cash, customer, balance, sale), cancellationToken);
                }

                // A Credit sale to a party that is also our supplier: settle again on what is still due.
                decimal settlementRedone = 0;
                if (sale.SaleType == SaleType.Debit && sale.DueAmount > 0)
                {
                    var settlement = await new CrossLedgerSettlement(_dbContext).SettleSaleAsync(sale, customer, cancellationToken);
                    settlementRedone = settlement?.Amount ?? 0m;
                }

                // ---------------------------------------------------------------- Audit
                var afterLines = liveLines.Select(Snapshot).ToList();
                await _dbContext.SaleEdits.AddAsync(new SaleEdit
                {
                    SaleId = sale.Id,
                    CustomerSale = sale,
                    Reason = request.Reason.Trim(),
                    OldSubTotal = old.SubTotal, OldDiscountAmount = old.DiscountAmount, OldTaxAmount = old.TaxAmount,
                    OldTotalAmount = old.TotalAmount, OldPaidAmount = old.PaidAmount, OldDueAmount = old.DueAmount,
                    NewSubTotal = sale.SubTotal, NewDiscountAmount = sale.DiscountAmount, NewTaxAmount = sale.TaxAmount,
                    NewTotalAmount = sale.TotalAmount, NewPaidAmount = sale.PaidAmount, NewDueAmount = sale.DueAmount,
                    ReturnedAmount = returned,
                    SettlementReversed = settlementReversed,
                    SettlementRedone = settlementRedone,
                    CashReleased = cashReleased,
                    CashRefunded = cashRefunded,
                    CashReapplied = cashReapplied,
                    AdditionalPayment = additional,
                    ChangesJson = JsonSerializer.Serialize(new
                    {
                        before = beforeLines,
                        after = afterLines,
                        removedLineIds = removed.Select(r => r.Id),
                        stock = stockMoves,
                    }, Json),
                }, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var advance = cashReleased - cashRefunded;
                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = $"Invoice {sale.InvoiceNumber} updated: total {old.TotalAmount:0.00} -> {sale.TotalAmount:0.00}"
                              + (sale.DueAmount > 0 ? $", {sale.DueAmount:0.00} due" : "")
                              + (settlementRedone > 0 ? $", {settlementRedone:0.00} settled against what we owe this party as a supplier" : "")
                              + (advance > 0 ? $", {advance:0.00} kept as the customer's advance" : "")
                              + (cashRefunded > 0 ? $", {cashRefunded:0.00} refunded in cash" : "")
                              + ".",
                    Data = new
                    {
                        sale.Id,
                        sale.InvoiceNumber,
                        OldTotal = old.TotalAmount,
                        NewTotal = sale.TotalAmount,
                        Returned = returned,
                        sale.PaidAmount,
                        sale.DueAmount,
                        SettlementReversed = settlementReversed,
                        SettlementRedone = settlementRedone,
                        CashReleased = cashReleased,
                        CashRefunded = cashRefunded,
                        CashReapplied = cashReapplied,
                        AdditionalPayment = additional,
                        Advance = advance,
                    }
                };
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning(ex, "Sale edit {SaleId} lost a uniqueness race", request.Id);
                return Error(409, "One of those serial numbers was sold on another sale a moment ago. Nothing was changed — re-scan the unit and try again.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error editing sale {SaleId}", request.Id);
                return Error(500, "An error occurred while editing the sale. Nothing was changed.");
            }
        }

        // ------------------------------------------------------------------ lines

        private async Task<Result?> EditLineAsync(
            CustomerSale sale, SaleDetails line, EditSaleItemRequest item,
            Dictionary<int, int> soldDelta, Dictionary<int, ProductVariant> variants,
            List<int> releasedSerialIds, HashSet<int> claimedSerialIds,
            ProductCostResolver costResolver, CancellationToken cancellationToken)
        {
            var variant = line.ProductVariant;
            var name = variant.Product.ProductName;
            if (item.ProductVariantId != line.ProductVariantId)
                return Error(400, $"'{name}': a line's product cannot be changed. Remove the line and add the new product.");

            var price = item.UnitPrice.HasValue
                ? decimal.Round(item.UnitPrice.Value, 2, MidpointRounding.AwayFromZero)
                : line.UnitPrice;
            if (line.ReturnedQuantity > 0 && price != line.UnitPrice)
                return Error(400, $"'{name}' has returns booked at {line.UnitPrice:0.00}, so its price cannot be changed.");
            if (item.Quantity < line.ReturnedQuantity)
                return Error(400, $"'{name}': the quantity cannot be less than the {line.ReturnedQuantity} already returned.");

            if (variant.IsSerialized)
            {
                if (item.Quantity != 1)
                    return Error(400, $"'{name}' is serialized: each line sells exactly 1 unit.");

                var wanted = item.SerialNumber?.Trim();
                if (!string.IsNullOrEmpty(wanted)
                    && !string.Equals(wanted, line.ProductSerial?.SerialNumber, StringComparison.OrdinalIgnoreCase))
                {
                    if (line.ReturnedQuantity > 0)
                        return Error(400, $"'{name}' was returned, so its serial number cannot be changed.");

                    var claimed = await ClaimSerialAsync(sale, variant, wanted, releasedSerialIds, claimedSerialIds, cancellationToken);
                    if (claimed == null)
                        return Error(400, $"Serial number '{wanted}' is not available in stock for '{name}' at this branch.");

                    line.ProductSerial = claimed;
                    line.ProductSerialId = claimed.Id;
                    line.UnitCost = await costResolver.GetSerialCostAsync(claimed.SupplierPurchaseDetailsId, cancellationToken);
                    line.WarrantyDays = claimed.WarrantyDays;
                    claimed.Status = SerialStatus.Sold;
                    claimed.SoldDate = sale.SaleDate;
                }
                else if (item.WarrantyDays.HasValue && line.ProductSerial == null)
                {
                    line.WarrantyDays = item.WarrantyDays;
                }
            }
            else
            {
                var qtyDelta = item.Quantity - line.Quantity;
                if (qtyDelta != 0)
                {
                    soldDelta[line.ProductVariantId] = soldDelta.GetValueOrDefault(line.ProductVariantId) + qtyDelta;
                    variants[line.ProductVariantId] = variant;
                }
                if (item.WarrantyDays.HasValue)
                    line.WarrantyDays = item.WarrantyDays;
            }

            line.Quantity = item.Quantity;
            line.UnitPrice = price;
            line.TotalAmount = price * item.Quantity;
            return null;
        }

        private async Task<Result?> AddLineAsync(
            CustomerSale sale, EditSaleItemRequest item,
            Dictionary<int, int> soldDelta, Dictionary<int, ProductVariant> variants,
            List<int> releasedSerialIds, HashSet<int> claimedSerialIds,
            ProductCostResolver costResolver, CancellationToken cancellationToken)
        {
            var variant = await _dbContext.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == item.ProductVariantId, cancellationToken);
            if (variant == null)
                return Error(404, $"Product variant with id {item.ProductVariantId} not found.");
            var name = variant.Product.ProductName;

            ProductSerial? serial = null;
            if (variant.IsSerialized)
            {
                if (item.Quantity != 1)
                    return Error(400, $"'{name}' is serialized: each line sells exactly 1 unit. Add another line for additional units.");
                var serialNumber = item.SerialNumber?.Trim();
                if (string.IsNullOrEmpty(serialNumber))
                    return Error(400, $"A serial number is required for '{name}' (SKU {variant.SKU}).");
                serial = await ClaimSerialAsync(sale, variant, serialNumber, releasedSerialIds, claimedSerialIds, cancellationToken);
                if (serial == null)
                    return Error(400, $"Serial number '{serialNumber}' is not available in stock for '{name}' at this branch.");
                serial.Status = SerialStatus.Sold;
                serial.SoldDate = sale.SaleDate;
            }

            var price = decimal.Round(item.UnitPrice ?? variant.SellingPrice, 2, MidpointRounding.AwayFromZero);
            var unitCost = serial != null
                ? await costResolver.GetSerialCostAsync(serial.SupplierPurchaseDetailsId, cancellationToken)
                : await costResolver.GetAverageCostAsync(variant.Id, cancellationToken);

            soldDelta[variant.Id] = soldDelta.GetValueOrDefault(variant.Id) + item.Quantity;
            variants[variant.Id] = variant;

            sale.SaleDetails.Add(new SaleDetails
            {
                ProductVariantId = variant.Id,
                Quantity = item.Quantity,
                UnitPrice = price,
                UnitCost = unitCost,
                TotalAmount = price * item.Quantity,
                WarrantyDays = serial?.WarrantyDays ?? item.WarrantyDays,
                ProductSerialId = serial?.Id,
                Status = SaleLineStatus.Completed,
                CustomerSale = sale,
                ProductVariant = variant,
                ProductSerial = serial,
            });
            return null;
        }

        // A unit that is in stock at the sale's branch - or that this same edit just released.
        private async Task<ProductSerial?> ClaimSerialAsync(
            CustomerSale sale, ProductVariant variant, string serialNumber,
            List<int> releasedSerialIds, HashSet<int> claimedSerialIds, CancellationToken cancellationToken)
        {
            var released = releasedSerialIds.ToList();
            var serial = await _dbContext.ProductSerials.FirstOrDefaultAsync(s =>
                s.SerialNumber == serialNumber &&
                s.ProductVariantId == variant.Id &&
                s.BranchId == sale.BranchId &&
                (s.Status == SerialStatus.InStock || released.Contains(s.Id)), cancellationToken);
            return serial == null || !claimedSerialIds.Add(serial.Id) ? null : serial;
        }

        private static object Snapshot(SaleDetails d) => new
        {
            id = d.Id,
            productVariantId = d.ProductVariantId,
            productName = d.ProductVariant.Product.ProductName,
            sku = d.ProductVariant.SKU,
            quantity = d.Quantity,
            returnedQuantity = d.ReturnedQuantity,
            unitPrice = d.UnitPrice,
            totalAmount = d.TotalAmount,
            serialNumber = d.ProductSerial?.SerialNumber,
            warrantyDays = d.WarrantyDays,
        };

        private static Result Error(int statusCode, string message) =>
            new() { IsSuccess = false, StatusCode = statusCode, Status = "Error", Message = message };
    }
}
