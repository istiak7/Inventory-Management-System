using Inventory_Management_System.Features.Categories.Shared.Repository;
using Inventory_Management_System.Features.Products.Shared.Repository;
using Inventory_Management_System.Features.Suppliers.Shared.Repository;
using Inventory_Management_System.Shared.Repository;

namespace Inventory_Management_System.Shared.Extensions.DependencyExtensions
{
    public static class RegisterRepository
    {
        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

            // Supplier
            services.AddScoped<ISupplierRepository, SupplierRepository>();

            // Category & SubCategory
            services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
            services.AddScoped<IProductSubCategoryRepository, ProductSubCategoryRepository>();

            // Product
            services.AddScoped<IProductRepository, ProductRepository>();
        }
    }
}
