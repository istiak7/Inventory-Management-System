using Inventory_Management_System.Dtos;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management_System.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [Authorize]
    public class WarehousesController : ControllerBase
    {
        private readonly IWarehouseRepository WarehouseRepository;
        public WarehousesController(IWarehouseRepository WarehouseRepository)
        {
            this.WarehouseRepository = WarehouseRepository;
        }

        // Get All Warehouses
        [HttpGet("Warehouses/")]
        public async Task<IActionResult> GetWarehouses()
        {
            return Ok(await WarehouseRepository.GetWarehouses());
        }

        //Get Warehouse By Id
        [HttpGet("Warehouses/{id}")]
        public async Task<IActionResult> GetByWarehouseId(int id)
        {
            return Ok(await WarehouseRepository.GetByWarehouseId(id));
        }

        //Add Warehouse
        [HttpPost("/Warehouse")]
        public async Task<IActionResult> AddWarehouse([FromForm] CreateWarehouseDto warehouse)
        {
            await WarehouseRepository.AddWarehouse(warehouse);
            return Ok(warehouse);
        }

        //Update Warehouse
        [HttpPut("/Warehouse/{id}")]
        public async Task<IActionResult> UpdateWarehouse(int id, [FromForm] CreateWarehouseDto warehouse)
        {
            var UpdateWarehouse = await WarehouseRepository.UpdateWarehouse(id, warehouse);
            if (UpdateWarehouse == null)
            {
                return NotFound($"Warehouse with ID {id} is not found");
            }
            return Ok(UpdateWarehouse);
        }

        //Delete Warehouse
        [HttpDelete("/Warehouse{id}")]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            var DeleteWarehouse = await WarehouseRepository.DeleteWarehouse(id);
            if (DeleteWarehouse == false)
            {
                return NotFound($"Warehouse with ID {id} is not found");
            }
            return Ok($"Warehouse with ID {id} is Deleted Successfully");
        }
    }
}
