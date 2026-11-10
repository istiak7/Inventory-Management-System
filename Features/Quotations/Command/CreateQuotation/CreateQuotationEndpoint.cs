using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Quotations.Command.CreateQuotation
{
    public class CreateQuotationEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-quotation", async (CreateQuotationCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Quotations").RequirePermission(Permissions.SalesManage);
        }
    }
}
