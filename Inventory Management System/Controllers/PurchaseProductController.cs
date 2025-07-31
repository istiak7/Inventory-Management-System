using Inventory_Management_System.Dtos.Purchase;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [Authorize]
    public class PurchaseProductController : ControllerBase
    {
        private readonly IPurchaseRepository repository;
        public PurchaseProductController(IPurchaseRepository repository)
        {
            this.repository = repository;
        }

        [HttpPost("Purchase-Product/")]
        public async Task<IActionResult> CreatePurchase([FromBody] CreatePurchaseRequestDto request)
        {
            try
            {
                await repository.AddPurchase(request);
                return Ok("Successfully Purchase Added.");
            }
            catch (DbUpdateException)
            {
                return BadRequest("Invalid Supplier Id or ProductId");
            }
            catch
            {
                return StatusCode(500,"UnExpected Error");
            }
            
            
        }
    }
}
