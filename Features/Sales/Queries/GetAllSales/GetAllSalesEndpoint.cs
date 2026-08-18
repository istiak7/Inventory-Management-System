using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Queries.GetAllSales
{
    public class GetAllSalesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-sales", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                int? customerId = null,
                int? branchId = null,
                string? saleType = null,
                string? search = null,
                DateTime? startDate = null,
                DateTime? endDate = null) =>
            {
                var result = await mediator.Send(
                    new GetAllSalesQuery(pageNumber, pageSize, customerId, branchId, saleType, search, startDate, endDate));
                return Results.Ok(result);
            }).WithTags("Sales");
        }
    }
}
