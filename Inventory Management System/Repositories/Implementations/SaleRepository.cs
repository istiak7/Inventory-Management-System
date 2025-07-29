using Inventory_Management_System.ApplicationDb;
using Inventory_Management_System.Dtos.Purchase;
using Inventory_Management_System.Dtos.Sale;
using Inventory_Management_System.Models;
using Inventory_Management_System.Repositories.Interfaces;

namespace Inventory_Management_System.Repositories.Implementations
{
    public class SaleRepository : ISaleRepository
    {
        private readonly ApplicationDbContext Context;
        public SaleRepository(ApplicationDbContext Context)
        {
            this.Context = Context;
        }
        public async Task<bool> SellProduct(CreateSaleRequestDto request)
        {
            var sale = new Sale
            {
                CustomerId = request.CustomerID,
                CreatedAt = DateTime.UtcNow
            };
            Context.Sales.Add(sale);
            await Context.SaveChangesAsync();

            var SaleDetails = request.Products.Select(product => new SaleDetails
            {
                SaleId = sale.Id,
                ProductId = product.ProductId,
                Quantity = product.Quantity,
                Price = product.Price,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            }).ToList();
            Context.SaleDetails.AddRange(SaleDetails);
            await Context.SaveChangesAsync();
            return true;
        }
    }
}
