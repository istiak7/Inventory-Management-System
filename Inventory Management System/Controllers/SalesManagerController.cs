using Inventory_Management_System.Dtos.Products;
using Inventory_Management_System.Dtos.SaleDto;
using Inventory_Management_System.Repositories.Implementations;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [Authorize]

    public class SalesManagerController : ControllerBase
    {
        private readonly ISalesManager repository;
        public SalesManagerController(ISalesManager repository)
        {
            this.repository = repository;
        }
        [HttpPost("approve-order/")]
        public async Task<IActionResult> CountNormalProductByWarehouse(SaleManagerDto salemanagerDto)
        {
          
            try
            {
                await repository.CanApproveSale(salemanagerDto);
                return Ok("Successfully Saled this Product");
            }
            catch (NotFoundStockException message)
            {
               
                return NotFound( message.Message);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Failed to Sale Product");
            }
            catch
            {
                return StatusCode(500, "UnExpected Error");
            }
        }
    }
}
