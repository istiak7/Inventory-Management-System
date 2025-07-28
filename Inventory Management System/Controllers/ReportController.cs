using Inventory_Management_System.Dtos;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management_System.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class ReportController : ControllerBase
    {
        private readonly IRecieveProduct repository;
        public ReportController(IRecieveProduct repository)
        {
            this.repository = repository;
        }

        [HttpPost("receive")]
        public async Task<IActionResult> ReceiveProduct([FromBody] RecieveProductDto dto)
        {
            var result = await repository.RecieveProduct(dto);
            if (!result)
                return BadRequest("Invalid data or stock not found");
            return Ok("Product received and recorded");
        }
    }
}
