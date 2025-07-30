using Inventory_Management_System.ApplicationDb;
using Inventory_Management_System.Dtos.Purchase;
using Inventory_Management_System.Models;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Inventory_Management_System.Repositories.Implementations
{
    public class PurchaseRepository : IPurchaseRepository
    {
        private readonly ApplicationDbContext Context;
        public PurchaseRepository(ApplicationDbContext Context)
        {
            this.Context = Context;
        }
        public async Task<bool> AddPurchase(CreatePurchaseRequestDto request)
        {
            var purchase = new Purchase
            {
                SupplierId = request.SupplierID,
                CreatedAt = DateTime.UtcNow
            };

                Context.Purchases.Add(purchase);
                await Context.SaveChangesAsync();     

            var purchaseDetails = request.Products.Select(product => new PurchaseDetails
            {
                PurchaseId = purchase.Id,
                ProductId = product.ProductId,
                Quantity = product.Quantity,
                Price = product.Price,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            }).ToList();
            Context.PurchaseDetails.AddRange(purchaseDetails);
            await Context.SaveChangesAsync();
            return true;
        }
    }
}
