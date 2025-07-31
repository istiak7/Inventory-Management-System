using Inventory_Management_System.Dtos;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management_System.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [Authorize]
    public class BrandController : ControllerBase
    {
        private readonly IBrandRepository BrandRepository;
        public BrandController(IBrandRepository BrandRepository)
        {
            this.BrandRepository = BrandRepository;
        }

        // Get All Brand 

        [HttpGet("Brands/")]
        public async Task<IActionResult> GetBrands()
        {
            return Ok(await BrandRepository.GetBrands());
        }


        //Get Customer By Id 

        [HttpGet("Brands/{id}")]
        public async Task<IActionResult> GetByBrandId(int id)
        {
            return Ok(await BrandRepository.GetByBrandId(id));
        }


        //Add Customer 

        [HttpPost("/Brand")]
        public async Task<IActionResult> AddBrand([FromForm] CreateBrandDto brand)
        {
            await BrandRepository.AddBrand(brand);
            return Ok(brand);
        }

        //Update Customer

        [HttpPut("/Brand/{id}")]
        public async Task<IActionResult> UpdateBrand(int id, [FromForm] CreateBrandDto brand)
        {
            var UpdateBrand = await BrandRepository.UpdateBrand(id, brand);
            if (UpdateBrand == null)
            {
                return NotFound($"Brand with ID {id} is not found");
            }
            return Ok(UpdateBrand);
        }

        //Delete Brand

        [HttpDelete("/Brand{id}")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var DeleteBrand = await BrandRepository.DeleteBrand(id);
            if (DeleteBrand == false)
            {
                return NotFound($"Brand with ID {id} is not found");
            }
            return Ok($"Brand with ID {id} is Delete Successfully");
        }
    }
}
