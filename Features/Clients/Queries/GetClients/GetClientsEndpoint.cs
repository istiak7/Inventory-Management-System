using Inventory_Management_System.Features.Clients.Shared;
using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.Clients.Queries.GetClients
{
    // The client list of the header's client dropdown. Super Admin only (403 for everyone else,
    // the Admin role included).
    public class GetClientsEndpoint : IEndpoint
    {
        public const string Route = "/get-clients";

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet(Route, () => Results.Ok(new Result
            {
                IsSuccess = true,
                StatusCode = 200,
                Status = "Success",
                Message = "Clients retrieved successfully",
                Data = ClientCatalog.All
            }))
            .WithTags("Clients")
            .RequireAuthorization(MenuActionAuthorizationSetup.SuperAdminOnlyPolicy);
        }
    }
}
