using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Quotations.Queries.GetAllQuotations
{
    public class GetAllQuotationsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-quotations", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                int? customerId = null,
                string? status = null,
                string? search = null,
                DateTime? startDate = null,
                DateTime? endDate = null) =>
            {
                var result = await mediator.Send(
                    new GetAllQuotationsQuery(pageNumber, pageSize, customerId, status, search, startDate, endDate));
                return Results.Ok(result);
            }).WithTags("Quotations").RequirePermission(Permissions.SalesView);
        }
    }
}
