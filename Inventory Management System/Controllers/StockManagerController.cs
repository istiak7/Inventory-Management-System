using Inventory_Management_System.Dtos;
using Inventory_Management_System.Dtos.StockInsertDto;
using Inventory_Management_System.Repositories.Implementations;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management_System.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class StockManagerController : ControllerBase
    {
        private readonly IStockRepository repository;
        private readonly IRecieveProduct RecieveProductrepository;
        public StockManagerController(IStockRepository repository, IRecieveProduct RecieveProductrepository)
        {
            this.repository = repository;
            this.RecieveProductrepository = RecieveProductrepository;
        }
        [HttpPost("Add-from-Purchase/")]
        public async Task<IActionResult> AddStockFromPurchase([FromForm] StockInsertDto stockDto)
        {
            var result = await repository.AddStockFromPurchase(stockDto);
            if(result == false)
            {
                return NotFound();
            }
            return Ok("Successfully stocked from Purchase!");
        }



        [HttpPost("Receive")]
        public async Task<IActionResult> ReceiveProduct([FromBody] RecieveProductDto dto)
        {
            var result = await RecieveProductrepository.RecieveProduct(dto);
            if (!result)
                return BadRequest("Invalid data or stock not found");
            return Ok("Product received and recorded");
        }
    }
}
