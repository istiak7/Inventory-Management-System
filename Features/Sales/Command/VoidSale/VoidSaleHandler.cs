using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LedgerExtensions;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_System.Features.Parties.Shared;

namespace Inventory_Management_System.Features.Sales.Command.VoidSale
{
    // Cancels a completed sale without erasing it:
    //   - every unit goes back on the shelf (stock + a ReturnIn movement, serials InStock again),
    //   - the customer ledger gets a "Void" row taking the sale off what they owe,
    //   - money already taken is refunded: a negative "Refund" payment allocated to the invoice
    //     and a "Refund" ledger row, so cash reports show it leaving on the void date,
    //   - the sale is marked Voided with the reason; reports and gross profit leave it out.
    public class VoidSaleHandler(
        AppDbContext _dbContext,
        ILogger<VoidSaleHandler> _logger
    ) : IRequestHandler<VoidSaleCommand, Result>
    {
        public async Task<Result> Handle(VoidSaleCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Lock the sale so a payment or a second void at the same moment waits for this one.
                await _dbContext.LockRowAsync<CustomerSale>(request.Id, cancellationToken);

                var sale = await _dbContext.CustomerSales
                    .Include(s => s.Customer)
                    .Include(s => s.Branch)
                    .Include(s => s.SaleDetails).ThenInclude(d => d.ProductSerial)
                    .Include(s => s.SaleDetails).ThenInclude(d => d.ProductVariant)
                    .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

                if (sale == null)
                    return Error(404, "Sale not found.");

                if (sale.Status == SaleStatus.Voided)
                    return Error(400, $"Sale {sale.InvoiceNumber} is already voided.");

                // A warranty claim is tied to the unit as sold; voiding under it would leave the
                // claim pointing at a sale that never happened.
                var claim = await _dbContext.WarrantyClaims
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(c => c.SaleDetails.SaleId == sale.Id)
                    .Select(c => c.ClaimNumber)
                    .FirstOrDefaultAsync(cancellationToken);
                if (claim != null)
                    return Error(400, $"Sale {sale.InvoiceNumber} has warranty claim {claim}, so it cannot be voided.");

                foreach (var line in sale.SaleDetails)
                {
                    if (line.ProductSerial is { } soldSerial && soldSerial.Status != SerialStatus.Sold)
                        return Error(400, $"Serial '{soldSerial.SerialNumber}' on this sale is now {soldSerial.Status}, so the sale cannot be voided.");
                }

                await _dbContext.LockStocksAsync(
                    [sale.BranchId],
                    sale.SaleDetails.Select(d => d.ProductVariantId),
                    cancellationToken);

                var now = DateTime.UtcNow;
                var stocks = new Dictionary<int, Stock>();

                foreach (var line in sale.SaleDetails)
                {
                    if (!stocks.TryGetValue(line.ProductVariantId, out var stock))
                    {
                        stock = await _dbContext.Stocks
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(s => s.BranchId == sale.BranchId && s.ProductVariantId == line.ProductVariantId, cancellationToken);
                        if (stock == null)
                        {
                            stock = new Stock
                            {
                                BranchId = sale.BranchId,
                                ProductVariantId = line.ProductVariantId,
                                CurrentStock = 0,
                                Branch = sale.Branch,
                                ProductVariant = line.ProductVariant,
                            };
                            await _dbContext.Stocks.AddAsync(stock, cancellationToken);
                        }
                        stocks[line.ProductVariantId] = stock;
                    }

                    stock.CurrentStock += line.Quantity;

                    await _dbContext.InventoryTransactions.AddAsync(new InventoryTransaction
                    {
                        BranchId = sale.BranchId,
                        ProductVariantId = line.ProductVariantId,
                        SupplierPurchaseDetailsId = line.ProductSerial?.SupplierPurchaseDetailsId,
                        TransactionType = InventoryTxnType.ReturnIn,
                        QuantityIn = line.Quantity,
                        QuantityOut = 0,
                        BalanceAfter = stock.CurrentStock,
                        TransactionDate = now,
                        Branch = sale.Branch,
                        ProductVariant = line.ProductVariant,
                    }, cancellationToken);

                    if (line.ProductSerial is { } serial)
                    {
                        serial.Status = SerialStatus.InStock;
                        serial.SoldDate = null;
                    }

                    line.Status = SaleLineStatus.Voided;
                }

                // Lock the customer so a payment saved at the same moment cannot read the same
                // running balance.
                await _dbContext.LockRowAsync<Customer>(sale.CustomerId, cancellationToken);
                var balance = await _dbContext.CustomerTransactions
                    .Where(t => t.CustomerId == sale.CustomerId)
                    .GetLatestBalanceAsync(cancellationToken);

                balance -= sale.TotalAmount;
                await _dbContext.CustomerTransactions.AddAsync(
                    CustomerTransaction.ForVoid(sale, sale.Customer, now, balance), cancellationToken);

                // Any part of this sale settled against what we owe the party as a supplier is
                // undone first: those supplier bills become due again. That part is not cash,
                // so only the rest is refunded.
                var (reversed, balanceAfterReversal) = await new CrossLedgerSettlement(_dbContext)
                    .ReverseForVoidedSaleAsync(sale, balance, now, cancellationToken);
                balance = balanceAfterReversal;

                var refunded = sale.PaidAmount - reversed;
                if (refunded > 0)
                {
                    var refund = new CustomerPayment
                    {
                        CustomerId = sale.CustomerId,
                        BranchId = sale.BranchId,
                        Amount = -refunded,
                        PaymentDate = now,
                        PaymentMethod = "Refund",
                        Remarks = $"Refund for voided sale {sale.InvoiceNumber}",
                        Customer = sale.Customer,
                        Branch = sale.Branch,
                    };
                    await _dbContext.CustomerPayments.AddAsync(refund, cancellationToken);

                    await _dbContext.SaleCustomerPayments.AddAsync(new SaleCustomerPayment
                    {
                        Amount = -refunded,
                        AllocationDate = now,
                        CustomerSale = sale,
                        CustomerPayment = refund,
                    }, cancellationToken);

                    balance += refunded;
                    await _dbContext.CustomerTransactions.AddAsync(
                        CustomerTransaction.ForRefund(refund, sale.Customer, refunded, balance, sale), cancellationToken);
                }

                sale.Status = SaleStatus.Voided;
                sale.VoidedAt = now;
                sale.VoidReason = request.Reason.Trim();
                sale.PaidAmount = 0;
                sale.DueAmount = 0;
                sale.UpDatedAt = now;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = $"Sale {sale.InvoiceNumber} voided. Stock returned"
                        + (refunded > 0 ? $", {refunded:0.00} refunded" : "")
                        + (reversed > 0 ? $", {reversed:0.00} supplier settlement reversed" : "")
                        + ".",
                    Data = new
                    {
                        sale.Id,
                        sale.InvoiceNumber,
                        Status = sale.Status.ToString(),
                        sale.VoidedAt,
                        sale.VoidReason,
                        Refunded = refunded,
                        SettlementReversed = reversed
                    }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error voiding sale {SaleId}", request.Id);
                return Error(500, "An error occurred while voiding the sale.");
            }
        }

        private static Result Error(int statusCode, string message) =>
            new() { IsSuccess = false, StatusCode = statusCode, Status = "Error", Message = message };
    }
}
