using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetPartyPurchaseReport
{
    public class GetPartyPurchaseReportEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-party-purchase-report", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                DateTime? startDate = null,
                DateTime? endDate = null,
                int? supplierId = null,
                int? branchId = null,
                string? purchaseType = null,
                string? status = null,
                string? invoiceNumber = null,
                string? search = null,
                string? sortBy = null,
                bool sortDescending = true) =>
            {
                var result = await mediator.Send(new GetPartyPurchaseReportQuery(
                    pageNumber, pageSize, startDate, endDate, supplierId, branchId,
                    purchaseType, status, invoiceNumber, search, sortBy, sortDescending));
                return Results.Ok(result);
            }).WithTags("Reports").RequirePermission(Permissions.ReportsView);
        }
    }
}
