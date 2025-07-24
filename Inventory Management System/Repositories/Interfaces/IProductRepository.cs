using Inventory_Management_System.Dtos;

namespace Inventory_Management_System.Repositories.Interfaces
{
    public interface IProductRepository
    {
        Task<List<ViewProductDto>> GetProducts();
        Task<ViewProductDto> GetByProductId(int id);
        Task<bool> AddProduct(CreateProductDto product);
        Task<ViewProductDto> UpdateProduct(int id, CreateProductDto product);
        Task<bool> DeleteProduct(int id);
    }
}
