using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Sales.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Sales.Queries.GetSaleChallan
{
    /// <summary>
    /// The delivery challan for one sale: what was handed to the customer, without prices.
    ///
    /// Built entirely from the sale already on record — nothing is stored for a challan. Voided
    /// lines were returned to stock and never delivered, so they are left off; a fully voided sale
    /// has no challan at all. Serialized units are stored one per line, so lines of the same
    /// product are folded into one row carrying all of its serials and the summed quantity.
    /// </summary>
    public class GetSaleChallanQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetSaleChallanQueryHandler> _logger
    ) : IRequestHandler<GetSaleChallanQuery, Result>
    {
        /// <summary>
        /// Products carry no unit of measure — every sale line is counted in pieces — so the
        /// challan prints PCS, as the reference does. One place to change once a UoM field exists.
        /// </summary>
        private const string DefaultUom = "PCS";

        public async Task<Result> Handle(GetSaleChallanQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var sale = await _dbContext.CustomerSales
                    .AsNoTracking()
                    .Where(s => s.Id == request.SaleId)
                    .Select(s => new
                    {
                        s.Id,
                        s.InvoiceNumber,
                        s.SaleDate,
                        s.Status,
                        CustomerName = s.Customer.Name,
                        CustomerAddress = s.Customer.Address,
                        MobileNumber = s.Customer.PhoneNumber,
                        BranchName = s.Branch.Name,
                        SoldBy = _dbContext.Users
                            .Where(u => u.Id == s.CreatedById)
                            .Select(u => u.Name)
                            .FirstOrDefault(),
                        Lines = s.SaleDetails
                            .Where(d => d.Status == SaleLineStatus.Completed)
                            .OrderBy(d => d.Id)
                            .Select(d => new
                            {
                                d.ProductVariantId,
                                ProductName = d.ProductVariant.Product.ProductName,
                                d.Quantity,
                                SerialNumber = d.ProductSerial != null ? d.ProductSerial.SerialNumber : null,
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
                        Message = "Sale not found."
                    };

                if (sale.Status == SaleStatus.Voided)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = "This sale was voided. Nothing was delivered, so it has no challan."
                    };

                // One row per product, in the order the products were first entered on the sale.
                var items = sale.Lines
                    .GroupBy(l => l.ProductVariantId)
                    .Select((g, idx) => new ChallanLineResponse(
                        idx + 1,
                        g.First().ProductName,
                        g.Where(l => !string.IsNullOrWhiteSpace(l.SerialNumber))
                            .Select(l => l.SerialNumber!)
                            .ToList(),
                        g.Sum(l => l.Quantity),
                        DefaultUom))
                    .ToList();

                var response = new GetSaleChallanResponse(
                    sale.Id,
                    sale.InvoiceNumber,
                    sale.InvoiceNumber,
                    sale.SaleDate,
                    sale.CustomerName,
                    sale.CustomerAddress,
                    sale.MobileNumber,
                    sale.BranchName,
                    sale.SoldBy,
                    items,
                    items.Sum(i => i.Quantity));

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Challan retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving challan for sale {SaleId}", request.SaleId);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the challan."
                };
            }
        }
    }
}
