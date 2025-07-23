using Inventory_Management_System.ApplicationDb;
using Inventory_Management_System.Dtos;
using Inventory_Management_System.Models;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Repositories.Implementations
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext Context;
        public CategoryRepository(ApplicationDbContext Context)
        {
            this.Context = Context;
        }

        public async Task<List<ViewCategoryDto>> GetCategories()
        {
            var Categories = await Context.Category.ToListAsync();
            List<ViewCategoryDto> CategoryDto = [];
            foreach (var category in Categories)
            {
                ViewCategoryDto dto = new ViewCategoryDto()
                {
                    Name = category.Name,
                    Description = category.Description
                };
                CategoryDto.Add(dto);
            }
            return CategoryDto;
        }

        public async Task<ViewCategoryDto> GetByCategoryId(int id)
        {
            var Category = await Context.Category.FirstOrDefaultAsync(category => category.Id == id);
            ViewCategoryDto dto = new()
            {
                Name = Category.Name,
                Description = Category.Description
            };
            return dto;
        }

        public async Task<bool> AddCategory(CreateCategoryDto category)
        {
            var ExistingCategory = await Context.Category.FirstOrDefaultAsync(name => name.Name == category.Name);
            if (ExistingCategory != null)
            {
                return false;
            }
            var NewCategory = new Category
            {
                Name = category.Name,
                Description = category.Description
            };
            await Context.Category.AddAsync(NewCategory);
            await Context.SaveChangesAsync();
            return true;
        }

        public async Task<ViewCategoryDto> UpdateCategory(int id, CreateCategoryDto category)
        {
            var ExistingCategory = await Context.Category.FirstOrDefaultAsync(i => i.Id == id);
            if (ExistingCategory == null)
            {
                return null;
            }

            ExistingCategory.Name = category.Name;
            ExistingCategory.Description = category.Description;

            Context.Category.Update(ExistingCategory);
            await Context.SaveChangesAsync();

            return new ViewCategoryDto
            {
                Name = ExistingCategory.Name,
                Description = ExistingCategory.Description
            };
        }

        public async Task<bool> DeleteCategory(int id)
        {
            var ExistingCategory = await Context.Category.FirstOrDefaultAsync(i => i.Id == id);
            if (ExistingCategory == null)
            {
                return false;
            }
            Context.Category.Remove(ExistingCategory);
            await Context.SaveChangesAsync();
            return true;
        }
    }
}
