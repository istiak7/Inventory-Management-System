using Inventory_Management_System.ApplicationDb;
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

        public async Task<bool> UpdateStockPurchase()
        {

            var PendingProduct = await Context.PurchaseDetails.Include(p => p.Purchase).Where(purchase => purchase.Status == "Pending").ToListAsync();
            foreach(var product in PendingProduct)
            {
                product.Status = "Shifted";
            }
            await Context.SaveChangesAsync();

            foreach(var product in PendingProduct)
            {
                Console.WriteLine(product.ProductId);
                var stock = await Context.Stocks.FirstOrDefaultAsync(s => s.ProductId == product.ProductId &&
                  s.WarehouseId == product.Purchase.WarehouseId);
                if(stock != null)
                {
                    stock.CurrentStock += product.Quantity;
                }
                else
                {
                    var newstock = new Stock
                    {
                        ProductId = product.ProductId,
                        WarehouseId = product.Purchase.WarehouseId,
                        CurrentStock = product.Quantity,
                        CreatedAt = DateTime.UtcNow
                       
                    };
                    Context.Stocks.Add(newstock);
                }
            }
            await Context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> UpdateStockSale()
        {

            var PendingProduct = await Context.SaleDetails.Include(p => p.Sale).Where(purchase => purchase.Status == "Pending").ToListAsync();
            foreach (var product in PendingProduct)
            {
                product.Status = "Shifted";
            }
            await Context.SaveChangesAsync();

            foreach (var product in PendingProduct)
            {
                var stock = await Context.Stocks.FirstOrDefaultAsync(s => s.ProductId == product.ProductId &&
                  s.WarehouseId == product.Sale.WarehouseId);
                if (stock != null && product.Quantity <= stock.CurrentStock)
                {
                    stock.CurrentStock -= product.Quantity;
                }
                else
                {
                    return false;
                }
            }
            await Context.SaveChangesAsync();
            return true;
        }
    }
}
