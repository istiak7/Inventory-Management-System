using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetDailyGrossProfit
{
    public class GetDailyGrossProfitEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-daily-gross-profit", async (
                IMediator mediator,
                DateTime? date = null,
                int? branchId = null) =>
            {
                var result = await mediator.Send(new GetDailyGrossProfitQuery(date, branchId));
                return Results.Ok(result);
            }).WithTags("Reports").RequirePermission(Permissions.ReportsView);
        }
    }
}
