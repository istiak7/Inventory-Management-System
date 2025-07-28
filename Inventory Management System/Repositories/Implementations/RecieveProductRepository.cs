using Inventory_Management_System.ApplicationDb;
using Inventory_Management_System.Dtos;
using Inventory_Management_System.Models;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Repositories.Implementations
{
    public class RecieveProductRepository : IRecieveProduct
    {
        private readonly ApplicationDbContext  Context;
        public RecieveProductRepository(ApplicationDbContext Context)
        {
            this.Context = Context;
        }

        public async Task<bool> RecieveProduct(RecieveProductDto rDto)
        {
            var stock = await Context.Stocks.FirstOrDefaultAsync(s => s.Id == rDto.StockId);
            if (stock == null)
                return false;

            int total = rDto.GoodQuantity + rDto.BadQuantity + rDto.MissingQuantity;
            if (total != stock.Quantity) return false;

            var received = new RecivedProductReport
            {
                StockId = rDto.StockId,
                Normal = rDto.GoodQuantity,
                Damage = rDto.BadQuantity,
                Missing = rDto.MissingQuantity,
                CreatedAt = DateTime.UtcNow
                
            };
            Context.ProductReport.Add(received);
            await Context.SaveChangesAsync();
            return true;
        }
    }
}
