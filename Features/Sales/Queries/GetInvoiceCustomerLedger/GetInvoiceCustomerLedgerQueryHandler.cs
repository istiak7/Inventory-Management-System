using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Sales.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Sales.Queries.GetInvoiceCustomerLedger
{
    public class GetInvoiceCustomerLedgerQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetInvoiceCustomerLedgerQueryHandler> _logger
    ) : IRequestHandler<GetInvoiceCustomerLedgerQuery, Result>
    {
        public async Task<Result> Handle(GetInvoiceCustomerLedgerQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var sale = await _dbContext.CustomerSales
                    .AsNoTracking()
                    .Where(s => s.InvoiceNumber == request.InvoiceNumber && s.CustomerId == request.CustomerId)
                    .Select(s => new
                    {
                        s.Id,
                        s.InvoiceNumber,
                        CustomerName = s.Customer.Name,
                        CustomerAddress = s.Customer.Address,
                        MobileNumber = s.Customer.PhoneNumber,
                        BranchName = s.Branch.Name,
                        s.SubTotal,
                        s.DiscountAmount,
                        s.TaxAmount,
                        s.TotalAmount,
                        Items = s.SaleDetails
                            .Where(d => s.Status == SaleStatus.Voided || d.Status == SaleLineStatus.Completed)
                            .OrderBy(d => d.Id)
                            .Select(d => new
                            {
                                ProductDescription = d.ProductVariant.Product.ProductName,
                                WarrantyDays = d.WarrantyDays ?? 0,
                                Quantity = d.Quantity,
                                d.UnitPrice,
                                d.TotalAmount,
                            })
                            .ToList(),
                        Allocations = s.SaleCustomerPayments
                            .OrderBy(sp => sp.Id)
                            .Select(sp => new
                            {
                                sp.Id,
                                sp.Amount,
                                sp.Discount,
                                sp.CustomerPayment.PaymentDate,
                                sp.CustomerPayment.PaymentMethod,
                            })
                            .ToList(),
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (sale == null)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Status = "Error",
                        Message = "Sale invoice not found for this customer."
                    };

                var allocation = sale.Allocations
                    // Within one second: a date sent back by the browser loses the sub-second part.
                    .Where(a => Math.Abs((a.PaymentDate - request.Date.ToUniversalTime()).TotalSeconds) < 1)
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

                // Payment-time discounts have since shrunk TotalAmount and grown DiscountAmount, so
                // rebuild the invoice's original figures: the total before any such discount (so the
                // dues reconcile) and the discount agreed when the sale was made.
                var paymentDiscounts = sale.Allocations.Sum(a => a.Discount);
                var originalTotal = sale.TotalAmount + paymentDiscounts;
                var originalDiscount = sale.DiscountAmount - paymentDiscounts;
                var dueBefore = originalTotal
                    - sale.Allocations.Where(a => a.Id < allocation.Id).Sum(a => a.Amount + a.Discount);
                var dueAfter = dueBefore - (allocation.Amount + allocation.Discount);

                var items = sale.Items
                    .Select((d, idx) => new InvoiceLineItemResponse(
                        idx + 1,
                        d.ProductDescription,
                        d.WarrantyDays,
                        d.Quantity,
                        d.UnitPrice,
                        d.TotalAmount))
                    .ToList();

                var response = new GetInvoiceCustomerLedgerResponse(
                    sale.InvoiceNumber,
                    allocation.PaymentDate,
                    sale.CustomerName,
                    sale.CustomerAddress,
                    sale.MobileNumber,
                    allocation.PaymentMethod,
                    sale.BranchName,
                    items,
                    originalTotal,
                    allocation.Amount,
                    dueBefore,
                    dueAfter,
                    sale.SubTotal,
                    originalDiscount,
                    sale.TaxAmount,
                    allocation.Discount);

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
                _logger.LogError(ex, "Error retrieving sale invoice {InvoiceNumber} for customer {CustomerId}",
                    request.InvoiceNumber, request.CustomerId);
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
