namespace Inventory_Management_System.Repositories.Interfaces
{
    public interface IStockRepository
    {
        Task<bool> UpdateStockPurchase();
        Task<bool> UpdateStockSale();
    }
}
