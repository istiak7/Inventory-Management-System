using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.SalesReturns.Command.CreateSalesReturn
{
    public class CreateSalesReturnEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-sales-return", async (CreateSalesReturnCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Sales Return").RequirePermission(Permissions.SalesManage);
        }
    }
}
