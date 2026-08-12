using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Purchases.Queries.GetInvoiceSupplierLedger
{
   
    public class GetInvoiceSupplierLedgerQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetInvoiceSupplierLedgerQueryHandler> _logger
    ) : IRequestHandler<GetInvoiceSupplierLedgerQuery, Result>
    {
        public async Task<Result> Handle(GetInvoiceSupplierLedgerQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var purchase = await _dbContext.SupplierPurchases
                    .AsNoTracking()
                    .Where(p => p.InvoiceNumber == request.InvoiceNumber && p.SupplierId == request.SupplierId)
                    .Select(p => new
                    {
                        p.Id,
                        p.InvoiceNumber,
                        SupplierName = p.Supplier.Name,
                        SupplierAddress = p.Supplier.Description,
                        MobileNumber = p.Supplier.PhoneNumber,
                        BranchName = p.Branch.Name,
                        p.TotalAmount,
                        Items = p.SupplierPurchaseDetails
                            .OrderBy(d => d.Id)
                            .Select(d => new
                            {
                                ProductDescription = d.ProductVariant.Product.ProductName,
                                d.WarrantyMonths,
                                Quantity = d.OrderedQuantity,
                                d.UnitPrice,
                                d.TotalAmount,
                            })
                            .ToList(),
                        Allocations = p.SupplierPurchasePayments
                            .OrderBy(pp => pp.Id)
                            .Select(pp => new
                            {
                                pp.Id,
                                pp.Amount,
                                pp.SupplierPayment.PaymentDate,
                                pp.SupplierPayment.PaymentMethod,
                            })
                            .ToList(),
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (purchase == null)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Status = "Error",
                        Message = "Purchase invoice not found for this supplier."
                    };

                // fetch the latest allocation for the given date, if any
                var allocation = purchase.Allocations
                    .Where(a => a.PaymentDate == request.Date)
                    .OrderByDescending(a => a.Id)
                    .FirstOrDefault();

                if (allocation == null)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Status = "Error",
                        Message = "No payment was found against this invoice on that date."
                    };

                var dueBefore = purchase.TotalAmount
                    - purchase.Allocations.Where(a => a.Id < allocation.Id).Sum(a => a.Amount);
                var dueAfter = dueBefore - allocation.Amount;

                var items = purchase.Items
                    .Select((d, idx) => new InvoiceLineItemResponse(
                        idx + 1,
                        d.ProductDescription,
                        d.WarrantyMonths,
                        d.Quantity,
                        d.UnitPrice,
                        d.TotalAmount))
                    .ToList();

                var response = new GetInvoiceSupplierLedgerResponse(
                    purchase.InvoiceNumber,
                    allocation.PaymentDate,
                    purchase.SupplierName,
                    purchase.SupplierAddress,
                    purchase.MobileNumber,
                    allocation.PaymentMethod,
                    purchase.BranchName,
                    items,
                    purchase.TotalAmount,
                    allocation.Amount,
                    dueBefore,
                    dueAfter);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Invoice retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving purchase invoice {InvoiceNumber} for supplier {SupplierId}",
                    request.InvoiceNumber, request.SupplierId);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the invoice."
                };
            }
        }
    }
}
