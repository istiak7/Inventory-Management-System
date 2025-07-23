using Inventory_Management_System.ApplicationDb;
using Inventory_Management_System.Dtos;
using Inventory_Management_System.Models;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Repositories.Implementations
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly ApplicationDbContext Context;
        public CustomerRepository(ApplicationDbContext Context)
        {
            this.Context = Context;
        }

        public async Task<List<ViewCustomerDto>> GetCustomers()
        {
            var Customers = await Context.Customers.ToListAsync();
            List<ViewCustomerDto> CustomerDto = [];
            foreach(var customer in Customers)
            {
                ViewCustomerDto dto = new ViewCustomerDto()
                {
                    Name = customer.Name,
                    Phone = customer.Phone,
                    Email = customer.Email
                };
                CustomerDto.Add(dto);
            }
            return CustomerDto;
        }


       public async Task<ViewCustomerDto> GetByCustomerId(int id)
        {
            var Customer = await Context.Customers.FirstOrDefaultAsync(customer => customer.Id == id);
            ViewCustomerDto dto = new()
            {
                Name = Customer.Name,
                Phone = Customer.Phone,
                Email = Customer.Email
            };
            return dto;
        }


        public async Task<bool> AddCustomer(CreateCustomerDto customer)
        {
            var ExistingUser = await Context.Customers.FirstOrDefaultAsync(email => email.Email == customer.Email);
            if(ExistingUser != null)
            {
                return false;
            }
            var NewCustomer = new Customer
            {
                Name = customer.Name,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address,
                CreatedAt = DateTime.UtcNow
            };
            await Context.Customers.AddAsync(NewCustomer);
            await Context.SaveChangesAsync();
            return true;
        }

        public async Task<ViewCustomerDto> UpdateCustomer(int id, CreateCustomerDto customer)
        {
            var ExistingUser = await Context.Customers.FirstOrDefaultAsync(i => i.Id == id);
            if(ExistingUser == null)
            {
                return null;
            }

            ExistingUser.Name = customer.Name;
            ExistingUser.Phone = customer.Phone;
            ExistingUser.Email = customer.Email;
            ExistingUser.Address = customer.Address;
            ExistingUser.UpdatedAt = DateTime.UtcNow;

            Context.Customers.Update(ExistingUser);
            await Context.SaveChangesAsync();

            return new ViewCustomerDto
            {
                Name = ExistingUser.Name,
                Phone = ExistingUser.Phone,
                Email = ExistingUser.Email
            };
            
        }


        public async Task<bool> DeleteCustomer(int id)
        {
            var ExistingUser = await Context.Customers.FirstOrDefaultAsync(i => i.Id == id);
            if(ExistingUser == null)
            {
                return false;
            }
            Context.Customers.Remove(ExistingUser);
            await Context.SaveChangesAsync();
            return true;
        }

    }
}
