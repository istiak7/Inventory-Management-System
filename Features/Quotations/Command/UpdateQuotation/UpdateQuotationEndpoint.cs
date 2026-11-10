using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Quotations.Command.UpdateQuotation
{
    public class UpdateQuotationEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/update-quotation/{id:int}", async (int id, UpdateQuotationCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Quotations").RequirePermission(Permissions.SalesManage);
        }
    }
}
