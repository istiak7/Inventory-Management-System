using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Inventory.Queries.GetAllInventoryTransactions
{
    public class GetAllInventoryTransactionsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-inventory-transactions", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                int? branchId = null,
                int? productVariantId = null,
                string? transactionType = null) =>
            {
                var result = await mediator.Send(new GetAllInventoryTransactionsQuery(pageNumber, pageSize, branchId, productVariantId, transactionType));
                return Results.Ok(result);
            }).WithTags("Inventory").RequirePermission(Permissions.InventoryView);
        }
    }
}
