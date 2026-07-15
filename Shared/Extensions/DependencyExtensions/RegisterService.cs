using Inventory_Management_System.Features.Users.Shared.Services;

namespace Inventory_Management_System.Shared.Extensions.DependencyExtensions
{
    public static class RegisterService
    {
        public static void AddServices(this IServiceCollection services)
        {
            services.AddScoped<ITokenService, TokenService>();
        }
    }
}
