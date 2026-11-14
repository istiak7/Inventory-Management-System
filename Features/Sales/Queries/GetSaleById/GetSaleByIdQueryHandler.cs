using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Sales.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Sales.Queries.GetSaleById
{
    public class GetSaleByIdQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetSaleByIdQueryHandler> _logger
    ) : IRequestHandler<GetSaleByIdQuery, Result>
    {
        public async Task<Result> Handle(GetSaleByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var sale = await _dbContext.CustomerSales
                    .AsNoTracking()
                    .Where(s => s.Id == request.Id)
                    .Select(s => new
                    {
                        s.Id,
                        s.CustomerId,
                        CustomerName = s.Customer.Name,
                        CustomerPhoneNumber = s.Customer.PhoneNumber,
                        CustomerAddress = s.Customer.Address,
                        s.BranchId,
                        BranchName = s.Branch.Name,
                        s.InvoiceNumber,
                        s.Remarks,
                        s.SaleDate,
                        s.Status,
                        s.SaleType,
                        s.SubTotal,
                        s.DiscountAmount,
                        s.TaxAmount,
                        s.TotalAmount,
                        s.PaidAmount,
                        s.DueAmount,
                        s.ReturnedAmount,
                        s.VoidedAt,
                        s.VoidReason,
                        PreparedBy = _dbContext.Users
                            .Where(u => u.Id == s.CreatedById)
                            .Select(u => u.Name)
                            .FirstOrDefault(),
                        // Lines an invoice edit dropped stay on the record but are not on the invoice.
                        Lines = s.SaleDetails.Where(d => s.Status == SaleStatus.Voided || d.Status == SaleLineStatus.Completed).Select(d => new
                        {
                            d.Id,
                            d.ReturnedQuantity,
                            d.ProductVariantId,
                            Sku = d.ProductVariant.SKU,
                            ProductName = d.ProductVariant.Product.ProductName,
                            d.ProductVariant.IsSerialized,
                            d.Quantity,
                            d.UnitPrice,
                            d.TotalAmount,
                            d.WarrantyDays,
                            d.Status,
                            SerialNumber = d.ProductSerial != null ? d.ProductSerial.SerialNumber : null
                        }).ToList(),
                        Payments = s.SaleCustomerPayments.Select(p => new
                        {
                            p.CustomerPaymentId,
                            p.Amount,
                            p.CustomerPayment.PaymentDate,
                            p.CustomerPayment.PaymentMethod
                        }).ToList()
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (sale == null)
                {
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Status = "Error",
                        Message = "Sale not found."
                    };
                }

                // Money from earlier edits of this invoice that was released from it and is still
                // free (the customer's advance). An edit that grows the invoice puts it back first.
                var free = await _dbContext.CustomerPayments.IgnoreQueryFilters()
                    .Where(p => p.Amount > 0
                                && p.PaymentMethod != PartySettlement.PaymentMethod
                                && p.PaymentMethod != "Refund"
                                && p.SaleCustomerPayments.Any(a => a.SaleId == request.Id))
                    .Select(p => p.Amount - p.SaleCustomerPayments.Sum(a => a.Amount))
                    .ToListAsync(cancellationToken);
                var releasedPayment = free.Where(x => x > 0).Sum();

                var response = new SaleDetailResponse(
                    sale.Id, sale.CustomerId, sale.CustomerName, sale.CustomerPhoneNumber, sale.CustomerAddress,
                    sale.BranchId, sale.BranchName, sale.InvoiceNumber, sale.Remarks, sale.SaleDate,
                    sale.Status.ToString(), sale.SaleType.ToString(),
                    sale.SubTotal, sale.DiscountAmount, sale.TaxAmount, sale.TotalAmount,
                    sale.PaidAmount, sale.DueAmount, sale.Lines.Count,
                    sale.Lines.Select(l => new SaleLineResponse(
                        l.Id, l.ProductVariantId, l.Sku, l.ProductName, l.IsSerialized,
                        l.Quantity, l.UnitPrice, l.TotalAmount,
                        l.WarrantyDays, l.Status.ToString(), l.SerialNumber, l.ReturnedQuantity)).ToList(),
                    sale.Payments.Select(p => new SalePaymentResponse(
                        p.CustomerPaymentId, p.Amount, p.PaymentDate, p.PaymentMethod)).ToList(),
                    sale.PreparedBy,
                    sale.VoidedAt,
                    sale.VoidReason,
                    sale.ReturnedAmount,
                    releasedPayment);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Sale retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sale {SaleId}", request.Id);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the sale."
                };
            }
        }
    }
}
