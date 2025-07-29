using Inventory_Management_System.ApplicationDb;
using Inventory_Management_System.Dtos.Products;
using Inventory_Management_System.Dtos.WarehouseInfo;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Repositories.Implementations
{
    public class InventoryReportRepository : IInventoryReport
    {
        private readonly ApplicationDbContext Context;
        public InventoryReportRepository(ApplicationDbContext Context)
        {
            this.Context = Context;
        }

        public async Task<List<ViewWarehouseInfoDto>> GetWarehouseInfoById(int id)
        {
            var result = await Context.Stocks.Where(s => s.WarehouseId == id)
                .Include(s => s.PurchaseDetails)
                     .ThenInclude(pd => pd.Product)
                
                .Include(pd => pd.PurchaseDetails.Purchase)
                        .ThenInclude(p => p.Supplier)
                .Select(s => new ViewWarehouseInfoDto
                {
                    PurchaseId = s.PurchaseDetails.Purchase.Id,
                    Supplier = s.PurchaseDetails.Purchase.Supplier.Name,
                    Product = s.PurchaseDetails.Product.Name,
                    Quantity = s.Quantity,
                    PurchaseDate = s.PurchaseDetails.Purchase.CreatedAt
                }).ToListAsync();

            return result;
        }

        public async Task<ViewProductReportDto> GetCountNormalProductById(int id)
        {
            var result = await Context.ProductReport.Where(report => report.Stock.PurchaseDetails.ProductId == id).
               GroupBy(report => report.Stock.PurchaseDetails.Product.Name).
               Select(s => new ViewProductReportDto
               {
                   ProductName = s.Key,
                   CountNormalProduct = s.Sum(r => r.Normal)
               }).FirstOrDefaultAsync();
            return result;
        }
    }
}
