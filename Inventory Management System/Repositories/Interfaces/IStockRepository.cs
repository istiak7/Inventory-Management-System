using Inventory_Management_System.Dtos.StockInsertDto;

namespace Inventory_Management_System.Repositories.Interfaces
{
    public interface IStockRepository
    {
        Task<bool> AddStockFromPurchase(StockInsertDto stockDto);
    }
}
