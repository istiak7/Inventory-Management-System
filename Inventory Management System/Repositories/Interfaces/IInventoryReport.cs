using Inventory_Management_System.Dtos.Products;
using Inventory_Management_System.Dtos.WarehouseInfo;

namespace Inventory_Management_System.Repositories.Interfaces
{
    public interface IInventoryReport
    {
        Task<List<ViewWarehouseInfoDto>> GetWarehouseInfoById(int id);
        Task<ViewProductReportDto> GetCountNormalProductById(int id);
    }
}
