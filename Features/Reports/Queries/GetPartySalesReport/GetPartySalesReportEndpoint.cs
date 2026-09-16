using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetPartySalesReport
{
    public class GetPartySalesReportEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-party-sales-report", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                DateTime? startDate = null,
                DateTime? endDate = null,
                int? customerId = null,
                int? branchId = null,
                string? saleType = null,
                string? invoiceNumber = null,
                string? search = null,
                string? sortBy = null,
                bool sortDescending = true) =>
            {
                var result = await mediator.Send(new GetPartySalesReportQuery(
                    pageNumber, pageSize, startDate, endDate, customerId, branchId,
                    saleType, invoiceNumber, search, sortBy, sortDescending));
                return Results.Ok(result);
            }).WithTags("Reports").RequirePermission(Permissions.ReportsView);
        }
    }
}
