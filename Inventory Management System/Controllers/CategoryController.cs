using Inventory_Management_System.Dtos;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management_System.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [Authorize]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository CategoryRepository;
        public CategoryController(ICategoryRepository CategoryRepository)
        {
            this.CategoryRepository = CategoryRepository;
        }

        // Get All Categories

        [HttpGet("Categories/")]
        public async Task<IActionResult> GetCategories()
        {
            return Ok(await CategoryRepository.GetCategories());
        }

        //Get Categories By Id 
        [HttpGet("Categories/{id}")]
        public async Task<IActionResult> GetByCategoryId(int id)
        {
            return Ok(await CategoryRepository.GetByCategoryId(id));
        }

        //Add Category
        [HttpPost("/Category")]
        public async Task<IActionResult> AddCategory([FromForm] CreateCategoryDto category)
        {
            await CategoryRepository.AddCategory(category);
            return Ok(category);
        }

        //Update Category
        [HttpPut("/Category/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromForm] CreateCategoryDto category)
        {
            var UpdateCategory = await CategoryRepository.UpdateCategory(id, category);
            if (UpdateCategory == null)
            {
                return NotFound($"Category with ID {id} is not found");
            }
            return Ok(UpdateCategory);
        }

        //Delete Category
        [HttpDelete("/Category{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var DeleteCategory = await CategoryRepository.DeleteCategory(id);
            if (DeleteCategory == false)
            {
                return NotFound($"Category with ID {id} is not found");
            }
            return Ok($"Category with ID {id} is Delete Successfully");
        }
    }
}
