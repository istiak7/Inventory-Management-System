using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Quotations.Command.ChangeQuotationStatus
{
    public class ChangeQuotationStatusEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/change-quotation-status/{id:int}", async (int id, ChangeQuotationStatusCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Quotations").RequirePermission(Permissions.SalesManage);
        }
    }
}
