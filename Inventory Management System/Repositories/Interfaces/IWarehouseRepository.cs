using Inventory_Management_System.Dtos;

namespace Inventory_Management_System.Repositories.Interfaces
{
    public interface IWarehouseRepository
    {
        Task<List<ViewWarehouseDto>> GetWarehouses();
        Task<ViewWarehouseDto> GetByWarehouseId(int id);
        Task<bool> AddWarehouse(CreateWarehouseDto warehouse);
        Task<ViewWarehouseDto> UpdateWarehouse(int id, CreateWarehouseDto warehouse);
        Task<bool> DeleteWarehouse(int id);
    }
}
