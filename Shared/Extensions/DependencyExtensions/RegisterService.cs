using Inventory_Management_System.Features.Users.Shared.Services;
using Inventory_Management_System.Shared.CurrentUser;
using Inventory_Management_System.Shared.McpTools;
using Inventory_Management_System.Shared.Services.AgentService;

namespace Inventory_Management_System.Shared.Extensions.DependencyExtensions
{
    public static class RegisterService
    {
        public static void AddServices(this IServiceCollection services)
        {
            services.AddScoped<ITokenService, TokenService>();

            // Reads the logged-in user from the request; used for branch filtering.
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, CurrentUserService>();
            services.AddSingleton<ReportAgentService>();
        }
    }
}
