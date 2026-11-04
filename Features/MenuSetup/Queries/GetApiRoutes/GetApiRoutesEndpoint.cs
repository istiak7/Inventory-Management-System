using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.MenuSetup.Queries.GetApiRoutes
{
    // The routes a Menu Action can be given (the "API Routes" picker of the Menu Action page).
    public class GetApiRoutesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-api-routes", (EndpointDataSource endpoints) =>
                Results.Ok(MenuSetupResults.Ok(ApiRouteCatalog.List(endpoints), "API routes retrieved successfully")))
               .WithTags("SystemSetup").RequireAuthorization("AdminOnly");
        }
    }
}
