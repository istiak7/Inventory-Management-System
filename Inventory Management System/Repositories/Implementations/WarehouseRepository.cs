using Inventory_Management_System.ApplicationDb;
using Inventory_Management_System.Dtos;
using Inventory_Management_System.Models;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Repositories.Implementations
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly ApplicationDbContext Context;
        public WarehouseRepository(ApplicationDbContext Context)
        {
            this.Context = Context;
        }

        public async Task<List<ViewWarehouseDto>> GetWarehouses()
        {
            var Warehouses = await Context.Warehouses.ToListAsync();
            List<ViewWarehouseDto> WarehouseDto = [];
            foreach (var warehouse in Warehouses)
            {
                ViewWarehouseDto dto = new ViewWarehouseDto()
                {
                    Name = warehouse.Name,
                    Location = warehouse.Location,
                    Phone = warehouse.PhoneNumber
                };
                WarehouseDto.Add(dto);
            }
            return WarehouseDto;
        }

        public async Task<ViewWarehouseDto> GetByWarehouseId(int id)
        {
            var Warehouse = await Context.Warehouses.FirstOrDefaultAsync(warehouse => warehouse.Id == id);
            ViewWarehouseDto dto = new()
            {
                Name = Warehouse.Name,
                Location = Warehouse.Location,
                Phone = Warehouse.PhoneNumber
            };
            return dto;
        }

        public async Task<bool> AddWarehouse(CreateWarehouseDto warehouse)
        {
            var ExistingWarehouse = await Context.Warehouses.FirstOrDefaultAsync(name => name.Name == warehouse.Name);
            if (ExistingWarehouse != null)
            {
                return false;
            }
            var NewWarehouse = new Warehouse
            {
                Name = warehouse.Name,
                Location = warehouse.Location,
                PhoneNumber = warehouse.Phone,
                CreatedAt = DateTime.UtcNow
            };
            await Context.Warehouses.AddAsync(NewWarehouse);
            await Context.SaveChangesAsync();
            return true;
        }

        public async Task<ViewWarehouseDto> UpdateWarehouse(int id, CreateWarehouseDto warehouse)
        {
            var ExistingWarehouse = await Context.Warehouses.FirstOrDefaultAsync(i => i.Id == id);
            if (ExistingWarehouse == null)
            {
                return null;
            }

            ExistingWarehouse.Name = warehouse.Name;
            ExistingWarehouse.Location = warehouse.Location;
            ExistingWarehouse.PhoneNumber = warehouse.Phone;
            ExistingWarehouse.UpdatedAt = DateTime.UtcNow;

            Context.Warehouses.Update(ExistingWarehouse);
            await Context.SaveChangesAsync();

            return new ViewWarehouseDto
            {
                Name = ExistingWarehouse.Name,
                Location = ExistingWarehouse.Location,
                Phone = ExistingWarehouse.PhoneNumber
            };
        }

        public async Task<bool> DeleteWarehouse(int id)
        {
            var ExistingWarehouse = await Context.Warehouses.FirstOrDefaultAsync(i => i.Id == id);
            if (ExistingWarehouse == null)
            {
                return false;
            }
            Context.Warehouses.Remove(ExistingWarehouse);
            await Context.SaveChangesAsync();
            return true;
        }
    }
}
