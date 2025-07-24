using Inventory_Management_System.Dtos.Purchase;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management_System.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class PurchaseController : ControllerBase
    {
        private readonly IPurchaseRepository repository;
        public PurchaseController(IPurchaseRepository repository)
        {
            this.repository = repository;
        }

        [HttpPost("Purchase/")]
        public async Task<IActionResult> CreatePurchase([FromBody] CreatePurchaseRequestDto request)
        {
            await repository.AddPurchase(request);
            return Ok("Purchase Sucessfully!");
        }
    }
}
