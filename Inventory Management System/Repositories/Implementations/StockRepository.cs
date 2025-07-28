using Inventory_Management_System.ApplicationDb;
using Inventory_Management_System.Dtos.StockInsertDto;
using Inventory_Management_System.Models;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Repositories.Implementations
{
    public class StockRepository : IStockRepository
    {
        private readonly ApplicationDbContext Context;
        public StockRepository(ApplicationDbContext Context)
        {
            this.Context = Context;
        }

        public async Task<bool> AddStockFromPurchase(StockInsertDto stockDto)
        {
            var purchaseDetails = await Context.PurchaseDetails
           .FirstOrDefaultAsync(p => p.Id == stockDto.PurchaseDetailsId && p.Status == "Pending");

            if (purchaseDetails == null)
            {
                return false;
            }
            var stock = new Stock
            {
                WarehouseId = stockDto.WarehouseId,
                PurchaseDetailsId = stockDto.PurchaseDetailsId,
                Quantity = purchaseDetails.Quantity,
                Status = "Stored"
            };
            Context.Stocks.Add(stock);
            purchaseDetails.Status = "Shifed";
            await Context.SaveChangesAsync();
            return true;
            
        }
    }
}
