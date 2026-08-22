using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Queries.GetAllCustomers
{
    public class GetAllCustomersEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-customers", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                string? search = null) =>
            {
                var result = await mediator.Send(new GetAllCustomersQuery(pageNumber, pageSize, search));
                return Results.Ok(result);
            }).WithTags("Customer").RequirePermission(Permissions.CustomersView);
        }
    }
}
