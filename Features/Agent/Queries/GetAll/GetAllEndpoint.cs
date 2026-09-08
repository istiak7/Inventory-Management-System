using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.CurrentUser;
using MediatR;

namespace Inventory_Management_System.Features.Agent.Queries.GetAll
{
    public class GetAllEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/agent/get-all", async (IMediator mediator, string searchTerm) =>
            {
                var result = await mediator.Send(new GetAllQuery(searchTerm));
                return Results.Ok(result);
            })
            .WithTags("Agent");
            //.WithSummary("Runs the reporting agent against a search term.")
            //.RequirePermission(Permissions.ReportsView);
        }
    }
}
