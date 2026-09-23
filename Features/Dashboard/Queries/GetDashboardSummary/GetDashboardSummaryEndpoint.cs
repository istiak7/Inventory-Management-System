using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.CurrentUser;
using MediatR;

namespace Inventory_Management_System.Features.Dashboard.Queries.GetDashboardSummary
{
    public class GetDashboardSummaryEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-dashboard-summary", async (
                IMediator mediator,
                int days = 30,
                int lowStockThreshold = 5) =>
            {
                var result = await mediator.Send(new GetDashboardSummaryQuery(days, lowStockThreshold));
                return Results.Ok(result);
            }).WithTags("Dashboard").RequirePermission(Permissions.DashboardView);
        }
    }
}
