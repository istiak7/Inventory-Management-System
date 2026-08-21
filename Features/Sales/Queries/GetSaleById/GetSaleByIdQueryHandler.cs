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
                        Lines = s.SaleDetails.Select(d => new
                        {
                            d.Id,
                            d.ProductVariantId,
                            Sku = d.ProductVariant.SKU,
                            ProductName = d.ProductVariant.Product.ProductName,
                            d.ProductVariant.IsSerialized,
                            d.Quantity,
                            d.UnitPrice,
                            d.DiscountPerItem,
                            d.TotalAmount,
                            d.WarrantyMonths,
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

                var response = new SaleDetailResponse(
                    sale.Id, sale.CustomerId, sale.CustomerName, sale.CustomerPhoneNumber, sale.CustomerAddress,
                    sale.BranchId, sale.BranchName, sale.InvoiceNumber, sale.Remarks, sale.SaleDate,
                    sale.Status.ToString(), sale.SaleType.ToString(),
                    sale.SubTotal, sale.DiscountAmount, sale.TaxAmount, sale.TotalAmount,
                    sale.PaidAmount, sale.DueAmount, sale.Lines.Count,
                    sale.Lines.Select(l => new SaleLineResponse(
                        l.Id, l.ProductVariantId, l.Sku, l.ProductName, l.IsSerialized,
                        l.Quantity, l.UnitPrice, l.DiscountPerItem, l.TotalAmount,
                        l.WarrantyMonths, l.Status.ToString(), l.SerialNumber)).ToList(),
                    sale.Payments.Select(p => new SalePaymentResponse(
                        p.CustomerPaymentId, p.Amount, p.PaymentDate, p.PaymentMethod)).ToList());

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
