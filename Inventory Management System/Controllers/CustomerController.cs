using Inventory_Management_System.Dtos;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management_System.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [Authorize]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerRepository CustomerRepository;
        public CustomerController(ICustomerRepository CustomerRepository)
        {
            this.CustomerRepository = CustomerRepository;
        }
        
        // Get All Customer 

        [HttpGet("Customers/")]
        public async Task<IActionResult> GetCustomers()
        {
            return Ok(await CustomerRepository.GetCustomers());
        }


        //Get Customer By Id 

        [HttpGet("Customers/{id}")]
        public async Task<IActionResult> GetByCustomerId(int id)
        {
            return Ok(await CustomerRepository.GetByCustomerId(id));
        }


        //Add Customer 

        [HttpPost("/Customer")]
        public async Task<IActionResult> AddCustomer([FromForm] CreateCustomerDto customer)
        {
            await CustomerRepository.AddCustomer(customer);
            return Ok(customer);
        }

        //Update Customer

        [HttpPut("/Customer/{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromForm] CreateCustomerDto customer)
        {
            var UpdateCustomer = await CustomerRepository.UpdateCustomer(id, customer);
            if(UpdateCustomer == null)
            {
                return NotFound($"Customer with ID {id} is not found");
            }
            return Ok(UpdateCustomer);
        }

        //Delete Customer

        [HttpDelete("/Customer{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var DeleteCustomer = await CustomerRepository.DeleteCustomer(id);
            if (DeleteCustomer == false)
            {
                return NotFound($"Customer with ID {id} is not found");
            }
            return Ok($"Customer with ID {id} is Delete Successfully");
        }

    }
}
