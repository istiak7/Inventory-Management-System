using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Purchases.Command.CreatePurchaseOrder
{
    public class CreatePurchaseOrderHandler(
            AppDbContext _dbContext,
            ILogger<CreatePurchaseOrderHandler> _logger
        ) : IRequestHandler<CreatePurchaseOrderCommand, Result>
    {
        public async Task<Result> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            if (request.Items.Count == 0)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "At least one purchase item is required." };

            var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);
            if (supplier == null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Supplier not found." };

            var branch = await _dbContext.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
            if (branch == null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Branch not found." };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var purchase = new SupplierPurchase
                {
                    SupplierId = request.SupplierId,
                    BranchId = request.BranchId,
                    PurchaseDate = request.PurchaseDate ?? DateTime.Now,
                    InvoiceNumber = request.InvoiceNumber,
                    Status = PurchaseStatus.Pending,   // receipt lifecycle; goods not received yet
                    Supplier = supplier,
                    Branch = branch,
                };

                // Build lines against variants. TotalAmount is recomputed server-side (client total ignored).
                decimal totalAmount = 0;
                foreach (var item in request.Items)
                {
                    var variant = await _dbContext.ProductVariants.FirstOrDefaultAsync(v => v.Id == item.ProductVariantId, cancellationToken);
                    if (variant == null)
                        return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = $"Product variant with id {item.ProductVariantId} not found." };

                    var lineTotal = item.Quantity * item.UnitPrice;
                    totalAmount += lineTotal;

                    purchase.SupplierPurchaseDetails.Add(new SupplierPurchaseDetails
                    {
                        ProductVariantId = item.ProductVariantId,
                        OrderedQuantity = item.Quantity,
                        ReceivedQuantity = null,          // unknown until goods receipt
                        UnitPrice = item.UnitPrice,
                        TotalAmount = lineTotal,
                        WarrantyMonths = item.WarrantyMonths,
                        Status = LineStatus.Pending,
                        SupplierPurchase = purchase,
                        ProductVariant = variant,
                    });
                }

                // Payment mode inferred from Amount vs total (no silent clamping).
                var paidAmount = request.Payment?.Amount ?? 0;
                if (paidAmount < 0)
                    return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Payment amount cannot be negative." };
                if (paidAmount > totalAmount)
                    return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Payment exceeds the total. Use full (Cash) payment or a smaller amount." };

                purchase.TotalAmount = totalAmount;
                purchase.PaidAmount = 0;
                purchase.DueAmount = totalAmount;
                purchase.PurchaseType = paidAmount >= totalAmount && totalAmount > 0 ? PurchaseType.Cash : PurchaseType.Credit;

                // Ledger (money): the purchase debits the supplier account (we owe more).
                var runningBalance = await GetCurrentSupplierBalanceAsync(purchase.SupplierId, cancellationToken);
                runningBalance += totalAmount;
                await _dbContext.SupplierTransactions.AddAsync(new SupplierTransaction
                {
                    SupplierId = purchase.SupplierId,
                    TransactionType = "Purchase",
                    TransactionDate = purchase.PurchaseDate,
                    Debit = totalAmount,
                    Credit = 0,
                    BalanceAfter = runningBalance,
                    SupplierPurchase = purchase,
                    Supplier = supplier,
                }, cancellationToken);

                SupplierPayment? payment = null;
                if (paidAmount > 0)
                {
                    purchase.ApplyPayment(paidAmount);   // updates Paid/Due consistently

                    payment = new SupplierPayment
                    {
                        SupplierId = purchase.SupplierId,
                        BranchId = purchase.BranchId,
                        Amount = paidAmount,
                        PaymentDate = request.Payment?.PaymentDate ?? DateTime.Now,
                        PaymentMethod = request.Payment?.PaymentMethod ?? "Cash",
                        Supplier = supplier,
                        Branch = branch,
                    };
                    await _dbContext.SupplierPayments.AddAsync(payment, cancellationToken);

                    // This payment settles this purchase (manual 1:1 pairing at creation).
                    await _dbContext.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
                    {
                        Amount = paidAmount,
                        AllocationDate = payment.PaymentDate,
                        SupplierPurchase = purchase,
                        SupplierPayment = payment,
                    }, cancellationToken);

                    // Ledger (money): the payment credits the supplier account (we owe less).
                    runningBalance -= paidAmount;
                    await _dbContext.SupplierTransactions.AddAsync(new SupplierTransaction
                    {
                        SupplierId = purchase.SupplierId,
                        TransactionType = "Payment",
                        TransactionDate = payment.PaymentDate,
                        Debit = 0,
                        Credit = paidAmount,
                        BalanceAfter = runningBalance,
                        SupplierPayment = payment,
                        Supplier = supplier,
                    }, cancellationToken);
                }

                await _dbContext.SupplierPurchases.AddAsync(purchase, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new PurchaseOrderResponse(
                    purchase.Id, purchase.SupplierId, purchase.BranchId, purchase.PurchaseDate,
                    purchase.InvoiceNumber, purchase.Status.ToString(), purchase.PurchaseType.ToString(),
                    purchase.TotalAmount, purchase.DueAmount,
                    payment == null ? null : new PaymentResponse(payment.Id, payment.Amount, payment.PaymentDate, payment.PaymentMethod)
                );

                return new Result { IsSuccess = true, StatusCode = 201, Status = "Success", Message = "Purchase order created successfully", Data = response };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating purchase order");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while creating the purchase order." };
            }
        }

        /// <summary>Supplier's current overall balance = BalanceAfter of their latest transaction (0 if none).</summary>
        private async Task<decimal> GetCurrentSupplierBalanceAsync(int supplierId, CancellationToken cancellationToken)
        {
            return await _dbContext.SupplierTransactions
                .AsNoTracking()
                .Where(t => t.SupplierId == supplierId)
                .OrderByDescending(t => t.Id)
                .Select(t => t.BalanceAfter)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
