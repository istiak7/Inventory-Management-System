using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management_System.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryReport Repository;
        public InventoryController(IInventoryReport repository)
        {
            this.Repository = repository;
        }

        [HttpGet("Warehouse-Info/{id}")]
       public async Task<IActionResult> GetWarehouseInfoById(int id)
        {
            return Ok(await Repository.GetWarehouseInfoById(id));
        }

        [HttpGet("Available-Product/{id}")]
        public async Task<IActionResult> GetCountNormalProductById(int id)
        {
            return Ok(await Repository.GetCountNormalProductById(id));
        }
    }
}
