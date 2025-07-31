using Inventory_Management_System.Dtos.Products;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductRepository ProductRepository;
        public ProductController(IProductRepository ProductRepository)
        {
            this.ProductRepository = ProductRepository;
        }

        // Get All Products
        [HttpGet("Products/")]
        public async Task<IActionResult> GetProducts()
        {
            return Ok(await ProductRepository.GetProducts());
        }

        //Get Product By Id 
        [HttpGet("Products/{id}")]
        public async Task<IActionResult> GetByProductId(int id)
        {
            return Ok(await ProductRepository.GetByProductId(id));
        }

        //Add Product 
        [HttpPost("/Product")]
        public async Task<IActionResult> AddProduct([FromForm] CreateProductDto product)
        {
            try
            {
                await ProductRepository.AddProduct(product);
                return Ok("Successfully Added Product");
            }
            catch(InvalidOperationException message)
            {
                return BadRequest(message.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An Error Occurs");
            }
               
        }

        //Update Product
        [HttpPut("/Product/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] CreateProductDto product)
        {
            try
            {
                var UpdateProduct = await ProductRepository.UpdateProduct(id, product);
                if (UpdateProduct == null)
                {
                    return NotFound($"Product with ID {id} is not found");
                }
                return Ok(UpdateProduct);
            }
           
            catch (DbUpdateException message)
            {
                return BadRequest(message.Message);
            }
            catch (Exception message)
            {
                return StatusCode(500,message.Message);
            }
         
        }

        //Delete Product
        [HttpDelete("/Product{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var DeleteProduct = await ProductRepository.DeleteProduct(id);
            if (DeleteProduct == false)
            {
                return NotFound($"Product with ID {id} is not found");
            }
            return Ok($"Product with ID {id} is Delete Successfully");
        }
    }
}
