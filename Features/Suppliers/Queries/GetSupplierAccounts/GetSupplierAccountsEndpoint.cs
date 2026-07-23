using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierAccounts
{
    public class GetSupplierAccountsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-supplier-accounts", async (IMediator mediator, int? supplierId = null) =>
            {
                var result = await mediator.Send(new GetSupplierAccountsQuery(supplierId));
                return Results.Ok(result);
            }).WithTags("Supplier");
        }
    }
}
