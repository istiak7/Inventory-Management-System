using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetMonthlyGrossProfit
{
    public class GetMonthlyGrossProfitEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-monthly-gross-profit", async (
                IMediator mediator,
                int? year = null,
                int? month = null,
                int? branchId = null) =>
            {
                var result = await mediator.Send(new GetMonthlyGrossProfitQuery(year, month, branchId));
                return Results.Ok(result);
            }).WithTags("Reports").RequirePermission(Permissions.ReportsView);
        }
    }
}
