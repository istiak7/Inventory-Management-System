using Inventory_Management_System.Dtos.Products;
using Inventory_Management_System.Dtos.SaleDto;

namespace Inventory_Management_System.Repositories.Interfaces
{
    public interface ISalesManager
    {
        Task<bool> CanApproveSale(SaleManagerDto salemanagerDto);
    }
}
