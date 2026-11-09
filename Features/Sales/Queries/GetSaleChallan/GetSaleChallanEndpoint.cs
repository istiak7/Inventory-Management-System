using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Queries.GetSaleChallan
{
    public class GetSaleChallanEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-sale-challan/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetSaleChallanQuery(id));
                return Results.Ok(result);
            }).WithTags("Sales").RequirePermission(Permissions.SalesView);
        }
    }
}
