using Inventory_Management_System.ApplicationDb;
using Inventory_Management_System.Dtos;
using Inventory_Management_System.Models;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Repositories.Implementations
{
    public class BrandRepository : IBrandRepository
    {
        private readonly ApplicationDbContext Context;
        public BrandRepository(ApplicationDbContext Context)
        {
            this.Context = Context;
        }

        public async Task<List<ViewBrandDto>> GetBrands()
        {
            var Brands = await Context.Brand.ToListAsync();
            List<ViewBrandDto> BrandDto = [];
            foreach (var brand in Brands)
            {
                ViewBrandDto dto = new ViewBrandDto()
                {
                    Name = brand.Name,
                    Description = brand.Description
                };
                BrandDto.Add(dto);
            }
            return BrandDto;
        }


        public async Task<ViewBrandDto> GetByBrandId(int id)
        {
            var Brand = await Context.Brand.FirstOrDefaultAsync(brand => brand.Id == id);
            ViewBrandDto dto = new()
            {
                Name = Brand.Name,
                Description = Brand.Description
            };
            return dto;
        }


        public async Task<bool> AddBrand(CreateBrandDto brand)
        {
            var ExistingBrand = await Context.Brand.FirstOrDefaultAsync(name => name.Name == brand.Name);
            if (ExistingBrand != null)
            {
                return false;
            }
            var NewBrand = new Brand
            {
                Name = brand.Name,
                Description = brand.Description,
                CreatedAt = DateTime.UtcNow     
            };
            await Context.Brand.AddAsync(NewBrand);
            await Context.SaveChangesAsync();
            return true;
        }

        public async Task<ViewBrandDto> UpdateBrand(int id, CreateBrandDto brand)
        {
            var ExistingBrand = await Context.Brand.FirstOrDefaultAsync(i => i.Id == id);
            if (ExistingBrand == null)
            {
                return null;
            }

            ExistingBrand.Name = brand.Name;
            ExistingBrand.Description = brand.Description;
            ExistingBrand.UpdatedAt = DateTime.UtcNow;

            Context.Brand.Update(ExistingBrand);
            await Context.SaveChangesAsync();

            return new ViewBrandDto
            {
                Name = ExistingBrand.Name,
                Description = ExistingBrand.Description
            };

        }


        public async Task<bool> DeleteBrand(int id)
        {
            var ExistingBrand = await Context.Brand.FirstOrDefaultAsync(i => i.Id == id);
            if (ExistingBrand == null)
            {
                return false;
            }
            Context.Brand.Remove(ExistingBrand);
            await Context.SaveChangesAsync();
            return true;
        }
    }
}
