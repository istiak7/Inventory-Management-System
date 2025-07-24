using Inventory_Management_System.Dtos;
using Inventory_Management_System.Dtos.Purchase;

namespace Inventory_Management_System.Repositories.Interfaces
{
    public interface IPurchaseRepository
    {
         Task<bool> AddPurchase(CreatePurchaseRequestDto brand);
        
    }
}
