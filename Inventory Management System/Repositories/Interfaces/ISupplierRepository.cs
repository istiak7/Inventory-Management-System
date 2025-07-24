using Inventory_Management_System.Dtos;

namespace Inventory_Management_System.Repositories.Interfaces
{
    public interface ISupplierRepository
    {
        Task<List<ViewSupplierDto>> GetSuppliers();
        Task<ViewSupplierDto> GetBySupplierId(int id);
        Task<bool> AddSupplier(CreateSupplierDto supplier);
        Task<ViewSupplierDto> UpdateSupplier(int id, CreateSupplierDto supplier);
        Task<bool> DeleteSupplier(int id);
    }
}
