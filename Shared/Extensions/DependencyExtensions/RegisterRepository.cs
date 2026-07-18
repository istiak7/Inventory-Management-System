using Inventory_Management_System.Features.Suppliers.Shared.Repository;
using Inventory_Management_System.Shared.Repository;

namespace Inventory_Management_System.Shared.Extensions.DependencyExtensions
{
    public static class RegisterRepository
    {
        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

            // Feature-specific repositories
            services.AddScoped<ISupplierRepository, SupplierRepository>();
        }
    }
}
