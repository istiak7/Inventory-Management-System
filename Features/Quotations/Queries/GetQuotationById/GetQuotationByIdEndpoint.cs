using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Quotations.Queries.GetQuotationById
{
    // The quotation with its lines: used by the details view, the edit form, the printed
    // quotation and to pre-fill the sale when converting.
    public class GetQuotationByIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-quotation/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetQuotationByIdQuery(id));
                return Results.Ok(result);
            }).WithTags("Quotations").RequirePermission(Permissions.SalesView);
        }
    }
}
