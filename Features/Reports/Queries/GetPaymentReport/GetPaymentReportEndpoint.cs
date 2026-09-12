using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetPaymentReport
{
    public class GetPaymentReportEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-payment-report", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                DateTime? startDate = null,
                DateTime? endDate = null,
                string? partyType = null,
                int? partyId = null,
                string? direction = null,
                string? paymentMethod = null,
                string? status = null,
                int? branchId = null,
                string? search = null,
                string? sortBy = null,
                bool sortDescending = true) =>
            {
                var result = await mediator.Send(new GetPaymentReportQuery(
                    pageNumber, pageSize, startDate, endDate, partyType, partyId, direction,
                    paymentMethod, status, branchId, search, sortBy, sortDescending));
                return Results.Ok(result);
            }).WithTags("Reports").RequirePermission(Permissions.ReportsView);
        }
    }
}
