using Inventory_Management_System.ApplicationDb;
using Inventory_Management_System.Dtos.Products;
using Inventory_Management_System.Dtos.SaleDto;
using Inventory_Management_System.Models;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Repositories.Implementations
{
    public class SalesManagerRepository : ISalesManager
    {
        private readonly ApplicationDbContext Context;
        public SalesManagerRepository(ApplicationDbContext Context)
        {
            this.Context = Context;
        }

        public async Task<bool> CanApproveSale(SaleManagerDto salemanagerDto)
        {
            var TotalNormalPerWarehouse= await Context.ProductReport.Where(report => report.Stock.PurchaseDetails.ProductId == salemanagerDto.ProductId &&
            report.Stock.WarehouseId == salemanagerDto.WarehouseId).
            GroupBy(report => report.Stock.PurchaseDetails.Product.Name).
            Select(g => new ViewProductReportDto
            {
                ProductName = g.Key,
                CountNormalProduct = g.Sum(r => r.Normal)
            }).FirstOrDefaultAsync();

          

            var totalSoldPerWarehouse = await Context.SalesManager
             .Where(m =>
                 m.WarehouseId == salemanagerDto.WarehouseId &&
                 m.SaleDetails.ProductId == salemanagerDto.ProductId &&
                 m.SaleDetails.Status == "Approved")
             .SumAsync(m => m.SaleDetails.Quantity);

            var CurrentQuantity = await Context.SaleDetails.Where(CQ => CQ.Id == salemanagerDto.SaleDetailsId).
                Select(s => s.Quantity).FirstOrDefaultAsync();

            var AvailableProduct = TotalNormalPerWarehouse.CountNormalProduct - totalSoldPerWarehouse;
            if(AvailableProduct >= CurrentQuantity)
            {
              var saleDetail = await Context.SaleDetails
             .FirstOrDefaultAsync(s => s.Id == salemanagerDto.SaleDetailsId);

                if (saleDetail != null)
                {
                    saleDetail.Status = "Approved";
                    await Context.SaveChangesAsync();
                    return true;
                }
                
            }
             return false;
        }
    }
}
