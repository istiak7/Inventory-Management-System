using Inventory_Management_System.Features.Users.Shared.Services;
using Inventory_Management_System.Shared.Services.Outbox;

namespace Inventory_Management_System.Shared.Extensions.DependencyExtensions
{
    public static class RegisterService
    {
        public static void AddServices(this IServiceCollection services)
        {
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IOutboxService, OutboxService>();
        }
    }
}
