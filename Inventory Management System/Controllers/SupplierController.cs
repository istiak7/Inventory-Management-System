using Inventory_Management_System.Dtos;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management_System.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [Authorize]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierRepository SupplierRepository;
        public SupplierController(ISupplierRepository SupplierRepository)
        {
            this.SupplierRepository = SupplierRepository;
        }

        // Get All Suppliers
        [HttpGet("Suppliers/")]
        public async Task<IActionResult> GetSuppliers()
        {
            return Ok(await SupplierRepository.GetSuppliers());
        }

        //Get Supplier By Id
        [HttpGet("Suppliers/{id}")]
        public async Task<IActionResult> GetBySupplierId(int id)
        {
            return Ok(await SupplierRepository.GetBySupplierId(id));
        }

        //Add Supplier
        [HttpPost("/Supplier")]
        public async Task<IActionResult> AddSupplier([FromForm] CreateSupplierDto supplier)
        {
            await SupplierRepository.AddSupplier(supplier);
            return Ok(supplier);
        }

        //Update Supplier
        [HttpPut("/Supplier/{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromForm] CreateSupplierDto supplier)
        {
            var UpdateSupplier = await SupplierRepository.UpdateSupplier(id, supplier);
            if (UpdateSupplier == null)
            {
                return NotFound($"Supplier with ID {id} is not found");
            }
            return Ok(UpdateSupplier);
        }

        //Delete Supplier
        [HttpDelete("/Supplier{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var DeleteSupplier = await SupplierRepository.DeleteSupplier(id);
            if (DeleteSupplier == false)
            {
                return NotFound($"Supplier with ID {id} is not found");
            }
            return Ok($"Supplier with ID {id} is Deleted Successfully");
        }
    }
}
