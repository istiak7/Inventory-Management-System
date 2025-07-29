using Inventory_Management_System.Dtos.Products;
using Inventory_Management_System.Dtos.SaleDto;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management_System.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]

    public class SalesManagerController : ControllerBase
    {
        private readonly ISalesManager repository;
        public SalesManagerController(ISalesManager repository)
        {
            this.repository = repository;
        }
        [HttpPost("Sales-Manager/")]
        public async Task<IActionResult> CountNormalProductByWarehouse(SaleManagerDto salemanagerDto)
        {
            bool result = await repository.CanApproveSale(salemanagerDto);
            if(result == false)
            {
                return Ok("You Cannot Sale this Product From this Warehouse");
            }
            return Ok("Successfully Saled this product");
        }
    }
}
