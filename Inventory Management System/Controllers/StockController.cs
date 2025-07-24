using Inventory_Management_System.Repositories.Implementations;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management_System.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class StockController : ControllerBase
    {
        private readonly IStockRepository repository;
        public StockController(IStockRepository repository)
        {
            this.repository = repository;
        }
        [HttpPatch("Update-Stock-Purchase/")]
        public async Task<IActionResult> UpdateStockPurchase()
        {
            await repository.UpdateStockPurchase();
            return Ok("Successfully Updated!");
        }
        [HttpPatch("Update-Stock-Sale/")]
        public async Task<IActionResult> UpdateStockSale()
        {
            await repository.UpdateStockSale();
            return Ok("Successfully Updated!");
        }
    }
}
