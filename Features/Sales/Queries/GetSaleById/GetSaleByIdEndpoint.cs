using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Queries.GetSaleById
{
    public class GetSaleByIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-sale/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetSaleByIdQuery(id));
                return Results.Ok(result);
            }).WithTags("Sales");
        }
    }
}
