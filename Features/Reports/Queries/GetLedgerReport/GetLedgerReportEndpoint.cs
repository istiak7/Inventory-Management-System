using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetLedgerReport
{
    public class GetLedgerReportEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-ledger-report", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                string? partyType = null,
                int? partyId = null,
                DateTime? startDate = null,
                DateTime? endDate = null,
                string? transactionType = null,
                string? search = null,
                int? branchId = null) =>
            {
                var result = await mediator.Send(new GetLedgerReportQuery(
                    pageNumber, pageSize, partyType, partyId, startDate, endDate, transactionType, search, branchId));
                return Results.Ok(result);
            }).WithTags("Reports").RequirePermission(Permissions.ReportsView);
        }
    }
}
