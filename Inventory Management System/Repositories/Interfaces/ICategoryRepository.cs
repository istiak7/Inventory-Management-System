using Inventory_Management_System.Dtos;

namespace Inventory_Management_System.Repositories.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<ViewCategoryDto>> GetCategories();
        Task<ViewCategoryDto> GetByCategoryId(int id);
        Task<bool> AddCategory(CreateCategoryDto brand);
        Task<ViewCategoryDto> UpdateCategory(int id, CreateCategoryDto brand);
        Task<bool> DeleteCategory(int id);
    }
}
