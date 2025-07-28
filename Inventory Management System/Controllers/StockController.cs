using Inventory_Management_System.Dtos.StockInsertDto;
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
        [HttpPost("Add-from-Purchase/")]
        public async Task<IActionResult> AddStockFromPurchase([FromForm] StockInsertDto stockDto)
        {
            await repository.AddStockFromPurchase(stockDto);
            return Ok("Successfully stocked from Purchase!");
        }
    }
}
