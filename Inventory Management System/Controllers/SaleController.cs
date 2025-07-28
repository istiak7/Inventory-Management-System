//using Inventory_Management_System.Dtos.Purchase;
//using Inventory_Management_System.Dtos.Sale;
//using Inventory_Management_System.Repositories.Interfaces;
//using Microsoft.AspNetCore.Mvc;

//namespace Inventory_Management_System.Controllers
//{
//    [ApiController]
//    [Route("api/[Controller]")]
   
//        public class SaleController : ControllerBase
//        {
//            private readonly ISaleRepository repository;
//            public SaleController(ISaleRepository repository)
//            {
//                this.repository = repository;
//            }

//            [HttpPost("Sale/")]
//            public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequestDto request)
//            {
//                await repository.SellProduct(request);
//                return Ok("Purchase Sucessfully!");
//            }
//        }
//    }
