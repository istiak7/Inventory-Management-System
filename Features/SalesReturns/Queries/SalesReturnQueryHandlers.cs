using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Features.Returns.Shared;
using Inventory_Management_System.Features.SalesReturns.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.SalesReturns.Queries
{
    // Everything the return form needs about one sale: its lines with sold / returned /
    // still-returnable quantities and whether each line may come back at all.
    public class GetSaleReturnableHandler(AppDbContext _db, ILogger<GetSaleReturnableHandler> _logger)
        : IRequestHandler<GetSaleReturnableQuery, Result>
    {
        public async Task<Result> Handle(GetSaleReturnableQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var sale = await _db.CustomerSales
                    .AsNoTracking()
                    .Include(s => s.Customer)
                    .Include(s => s.Branch)
                    .Include(s => s.SaleDetails).ThenInclude(d => d.ProductVariant).ThenInclude(v => v.Product)
                    .Include(s => s.SaleDetails).ThenInclude(d => d.ProductSerial)
                    .FirstOrDefaultAsync(s => s.Id == request.SaleId, cancellationToken);
                if (sale == null)
                    return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Sale not found." };

                var eligibility = await SaleReturnEligibility.ForSaleAsync(_db, sale, cancellationToken);
                var settled = await _db.SaleCustomerPayments
                    .IgnoreQueryFilters()
                    .Where(a => a.SaleId == sale.Id && a.CustomerPayment.PaymentMethod == PartySettlement.PaymentMethod)
                    .SumAsync(a => (decimal?)a.Amount, cancellationToken) ?? 0m;
                var previous = await _db.SalesReturns
                    .AsNoTracking()
                    .Where(r => r.SaleId == sale.Id)
                    .OrderBy(r => r.Id)
                    .Select(r => new ReturnSummary(r.Id, r.ReturnNumber, r.ReturnDate, r.TotalAmount, r.Status.ToString()))
                    .ToListAsync(cancellationToken);

                var lines = sale.SaleDetails
                    .OrderBy(d => d.Id)
                    .Select(d =>
                    {
                        var rule = eligibility[d.Id];
                        return new SaleReturnableLine(
                            d.Id, d.ProductVariantId, d.ProductVariant.Product.ProductName, d.ProductVariant.SKU,
                            d.ProductSerial?.SerialNumber, rule.Sold, rule.Returned, rule.Remaining, d.UnitPrice,
                            ReturnMath.Share(d.UnitPrice, sale.SubTotal, sale.TotalAmount),
                            rule.CanReturn, rule.BlockedReason);
                    })
                    .ToList();

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Returnable lines retrieved successfully",
                    Data = new SaleReturnableResponse(
                        sale.Id, sale.InvoiceNumber, sale.SaleDate, sale.Status.ToString(), sale.SaleType.ToString(),
                        sale.CustomerId, sale.Customer.Name, sale.Customer.PhoneNumber, sale.BranchId, sale.Branch.Name,
                        sale.SubTotal, sale.DiscountAmount, sale.TaxAmount, sale.TotalAmount, sale.PaidAmount,
                        sale.DueAmount, sale.ReturnedAmount, Math.Max(0m, settled), lines, previous)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading returnable lines for sale {SaleId}", request.SaleId);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while reading the sale." };
            }
        }
    }

    public class GetSalesReturnsHandler(AppDbContext _db, ILogger<GetSalesReturnsHandler> _logger)
        : IRequestHandler<GetSalesReturnsQuery, Result>
    {
        public async Task<Result> Handle(GetSalesReturnsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _db.SalesReturns.AsNoTracking();

                var range = ReportDateRange.From(request.StartDate, request.EndDate);
                if (range.Start is DateTime start)
                    query = query.Where(r => r.ReturnDate >= start);
                if (range.EndExclusive is DateTime end)
                    query = query.Where(r => r.ReturnDate < end);
                if (request.BranchId is int branchId)
                    query = query.Where(r => r.BranchId == branchId);
                if (request.SaleId is int saleId)
                    query = query.Where(r => r.SaleId == saleId);
                if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<ReturnStatus>(request.Status.Trim(), true, out var status))
                    query = query.Where(r => r.Status == status);
                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var term = $"%{request.Search.Trim()}%";
                    query = query.Where(r =>
                        EF.Functions.ILike(r.ReturnNumber, term) ||
                        EF.Functions.ILike(r.CustomerSale.InvoiceNumber, term) ||
                        EF.Functions.ILike(r.Customer.Name, term) ||
                        EF.Functions.ILike(r.Customer.PhoneNumber, term));
                }

                var page = await query
                    .OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.Id)
                    .Select(r => new SalesReturnListItem(
                        r.Id, r.ReturnNumber, r.SaleId, r.CustomerSale.InvoiceNumber, r.CustomerId, r.Customer.Name,
                        r.Customer.PhoneNumber, r.BranchId, r.Branch.Name, r.ReturnDate,
                        r.Lines.Sum(l => l.Quantity), r.TotalAmount, r.DueReduced, r.SettlementReversed,
                        r.CashRefunded, r.Status.ToString(), r.Reason,
                        _db.Users.Where(u => u.Id == r.CreatedById).Select(u => u.Name).FirstOrDefault()))
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Sales returns retrieved successfully", Data = page };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing sales returns");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while listing sales returns." };
            }
        }
    }

    public class GetSalesReturnByIdHandler(AppDbContext _db, ILogger<GetSalesReturnByIdHandler> _logger)
        : IRequestHandler<GetSalesReturnByIdQuery, Result>
    {
        public async Task<Result> Handle(GetSalesReturnByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var detail = await _db.SalesReturns
                    .AsNoTracking()
                    .Where(r => r.Id == request.Id)
                    .Select(r => new SalesReturnDetail(
                        r.Id, r.ReturnNumber, r.SaleId, r.CustomerSale.InvoiceNumber, r.CustomerSale.SaleDate,
                        r.CustomerId, r.Customer.Name, r.Customer.PhoneNumber, r.Customer.Address,
                        r.BranchId, r.Branch.Name, r.ReturnDate, r.Status.ToString(),
                        r.SubTotal, r.DiscountAmount, r.TaxAmount, r.TotalAmount,
                        r.DueReduced, r.SettlementReversed, r.CashRefunded, r.Reason, r.Remarks,
                        _db.Users.Where(u => u.Id == r.CreatedById).Select(u => u.Name).FirstOrDefault(),
                        r.CreatedAt, r.CancelledAt, r.CancelReason,
                        r.Lines.OrderBy(l => l.Id).Select(l => new SalesReturnDetailLine(
                            l.Id, l.SaleDetailsId, l.ProductVariantId, l.ProductVariant.Product.ProductName,
                            l.ProductVariant.SKU, l.ProductSerial != null ? l.ProductSerial.SerialNumber : null,
                            l.Quantity, l.UnitPrice, l.Amount, l.Restock)).ToList()))
                    .FirstOrDefaultAsync(cancellationToken);

                return detail == null
                    ? new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Return not found." }
                    : new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Sales return retrieved successfully", Data = detail };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading sales return {Id}", request.Id);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while reading the return." };
            }
        }
    }
}
