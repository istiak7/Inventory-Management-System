using Inventory_Management_System.Shared.McpTools;

namespace Inventory_Management_System.Shared.Extensions.DependencyExtensions
{
    public static class McpToolsRegistration
    {
        public static void AddMcpTools(this IServiceCollection services)
        {
            //DI registration for MCP (My Cool Project) tools.
            services.AddScoped<DatabaseMcpTools>();
        }
    }
}
