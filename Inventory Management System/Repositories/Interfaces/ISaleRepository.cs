using Inventory_Management_System.Dtos.Purchase;
using Inventory_Management_System.Dtos.Sale;

namespace Inventory_Management_System.Repositories.Interfaces
{
    public interface ISaleRepository
    {
        Task<bool> SellProduct(CreateSaleRequestDto products);
    }
}
