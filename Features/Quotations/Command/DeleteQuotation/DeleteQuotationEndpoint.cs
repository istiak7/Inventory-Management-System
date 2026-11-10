using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Quotations.Command.DeleteQuotation
{
    public class DeleteQuotationEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-quotation/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteQuotationCommand(id));
                return Results.Ok(result);
            }).WithTags("Quotations").RequirePermission(Permissions.SalesManage);
        }
    }
}
