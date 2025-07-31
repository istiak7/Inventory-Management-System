using Inventory_Management_System.ApplicationDb;
using Inventory_Management_System.Dtos.Purchase;
using Inventory_Management_System.Dtos.Sale;
using Inventory_Management_System.Models;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

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
            try
            {
                Context.Sales.Add(sale);
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new DbUpdateException("Invalid Customer Id");
            }
            catch (Exception)
            {
                throw new Exception("An Error Occurs");
            }
            var SaleDetails = request.Products.Select(product => new SaleDetails
            {
                SaleId = sale.Id,
                ProductId = product.ProductId,
                Quantity = product.Quantity,
                Price = product.Price,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            }).ToList();
            try
            {
                Context.SaleDetails.AddRange(SaleDetails);
                await Context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                throw new DbUpdateException("Invalid Product Id or Data updation error");
            }
            catch (Exception)
            {
                throw new Exception("An Error Occurs");
            }

        }
    }
}
