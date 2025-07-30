using Inventory_Management_System.ApplicationDb;
using Inventory_Management_System.Dtos.Products;
using Inventory_Management_System.Dtos.SaleDto;
using Inventory_Management_System.Models;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Repositories.Implementations
{
    public class NotFoundStockException : Exception
    {
        public NotFoundStockException(string message) : base(message) { }
    }
    public class SalesManagerRepository : ISalesManager
    {
        private readonly ApplicationDbContext Context;
        public SalesManagerRepository(ApplicationDbContext Context)
        {
            this.Context = Context;
        }

        public async Task<bool> CanApproveSale(SaleManagerDto salemanagerDto)
        {

            //Total Normal Product Based on Warehouse, Product.
            var TotalNormalProductPerWarehouse = await Context.ProductReport.Where(report => report.Stock.PurchaseDetails.ProductId == salemanagerDto.ProductId &&
            report.Stock.WarehouseId == salemanagerDto.WarehouseId).
            GroupBy(report => report.Stock.PurchaseDetails.Product.Name).
            Select(g => new ViewProductReportDto
            {
                ProductName = g.Key,
                CountNormalProduct = g.Sum(r => r.Normal)
            }).FirstOrDefaultAsync();

            if(TotalNormalProductPerWarehouse == null)
            {
                throw new NotFoundStockException("No Available in Stock");
            }
            //Total Saled Product Based on Warehouse, Product
            int totalSoldProductPerWarehouse = await Context.SalesManager
             .Where(m =>
                 m.WarehouseId == salemanagerDto.WarehouseId &&
                 m.SaleDetails.ProductId == salemanagerDto.ProductId &&
                 m.SaleDetails.Status == "Approved")
             .SumAsync(m => m.SaleDetails.Quantity);
            

            //Curent Quantity Which means Current Order Quantity
            var CurrentQuantity = await Context.SaleDetails.Where(CQ => CQ.Id == salemanagerDto.SaleDetailsId).
                Select(s => s.Quantity).FirstOrDefaultAsync();

            //Total Available Stock Based on this Warehouse, Product

            var AvailableProduct = TotalNormalProductPerWarehouse.CountNormalProduct - totalSoldProductPerWarehouse;

            
            //Logic if Product is Available in Stock, Move to approve 
            if(AvailableProduct >= CurrentQuantity)
            {
              var saleDetails = await Context.SaleDetails
             .FirstOrDefaultAsync(s => s.Id == salemanagerDto.SaleDetailsId);

                if (saleDetails != null)
                {
                    saleDetails.Status = "Approved";
                    await Context.SaveChangesAsync();
                    var Manager = new SaleManager
                    {
                        WarehouseId = salemanagerDto.WarehouseId,
                        SaleDetailsId = salemanagerDto.SaleDetailsId,
                        CreatedAt = DateTime.UtcNow
                    };
                    Context.SalesManager.Add(Manager);
                    await Context.SaveChangesAsync();
                    return true;
                }
                
            }
             return false;
        }
    }
}
