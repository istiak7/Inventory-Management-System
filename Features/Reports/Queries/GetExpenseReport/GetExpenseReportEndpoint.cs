using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetExpenseReport
{
    public class GetExpenseReportEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-expense-report", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                DateTime? startDate = null,
                DateTime? endDate = null,
                int? categoryId = null,
                string? paymentMethod = null,
                int? branchId = null,
                string? search = null,
                string? sortBy = null,
                bool sortDescending = true) =>
            {
                var result = await mediator.Send(new GetExpenseReportQuery(
                    pageNumber, pageSize, startDate, endDate, categoryId, paymentMethod,
                    branchId, search, sortBy, sortDescending));
                return Results.Ok(result);
            }).WithTags("Reports").RequirePermission(Permissions.ReportsView);
        }
    }
}
