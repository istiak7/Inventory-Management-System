using Inventory_Management_System.ApplicationDb;
using Inventory_Management_System.Dtos;
using Inventory_Management_System.Models;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Repositories.Implementations
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly ApplicationDbContext Context;
        public SupplierRepository(ApplicationDbContext Context)
        {
            this.Context = Context;
        }

        public async Task<List<ViewSupplierDto>> GetSuppliers()
        {
            var Suppliers = await Context.Suppliers.ToListAsync();
            List<ViewSupplierDto> SupplierDto = [];
            foreach (var supplier in Suppliers)
            {
                ViewSupplierDto dto = new ViewSupplierDto()
                {
                    Name = supplier.Name,
                    Phone = supplier.Phone,
                    Email = supplier.Email,
                };
                SupplierDto.Add(dto);
            }
            return SupplierDto;
        }

        public async Task<ViewSupplierDto> GetBySupplierId(int id)
        {
            var Supplier = await Context.Suppliers.FirstOrDefaultAsync(supplier => supplier.Id == id);
            ViewSupplierDto dto = new()
            {
                Name = Supplier.Name,
                Phone = Supplier.Phone,
                Email = Supplier.Email,
            };
            return dto;
        }

        public async Task<bool> AddSupplier(CreateSupplierDto supplier)
        {
            var ExistingSupplier = await Context.Suppliers.FirstOrDefaultAsync(email => email.Email == supplier.Email);
            if (ExistingSupplier != null)
            {
                return false;
            }
            var NewSupplier = new Supplier
            {
                Name = supplier.Name,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                CreatedAt = DateTime.UtcNow
            };
            await Context.Suppliers.AddAsync(NewSupplier);
            await Context.SaveChangesAsync();
            return true;
        }

        public async Task<ViewSupplierDto> UpdateSupplier(int id, CreateSupplierDto supplier)
        {
            var ExistingSupplier = await Context.Suppliers.FirstOrDefaultAsync(i => i.Id == id);
            if (ExistingSupplier == null)
            {
                return null;
            }

            ExistingSupplier.Name = supplier.Name;
            ExistingSupplier.Phone = supplier.Phone;
            ExistingSupplier.Email = supplier.Email;
            ExistingSupplier.Address = supplier.Address;
            ExistingSupplier.UpdatedAt = DateTime.UtcNow;

            Context.Suppliers.Update(ExistingSupplier);
            await Context.SaveChangesAsync();

            return new ViewSupplierDto
            {
                Name = ExistingSupplier.Name,
                Phone = ExistingSupplier.Phone,
                Email = ExistingSupplier.Email,
            };
        }

        public async Task<bool> DeleteSupplier(int id)
        {
            var ExistingSupplier = await Context.Suppliers.FirstOrDefaultAsync(i => i.Id == id);
            if (ExistingSupplier == null)
            {
                return false;
            }
            Context.Suppliers.Remove(ExistingSupplier);
            await Context.SaveChangesAsync();
            return true;
        }
    }
}
