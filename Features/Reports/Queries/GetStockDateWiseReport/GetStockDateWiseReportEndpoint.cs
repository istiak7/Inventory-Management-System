using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetStockDateWiseReport
{
    public class GetStockDateWiseReportEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-stock-date-wise-report", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                DateTime? startDate = null,
                DateTime? endDate = null,
                int? branchId = null,
                int? productVariantId = null,
                int? productId = null,
                int? categoryId = null,
                int? subCategoryId = null,
                string? search = null,
                string? sortBy = null,
                bool sortDescending = false) =>
            {
                var result = await mediator.Send(new GetStockDateWiseReportQuery(
                    pageNumber, pageSize, startDate, endDate, branchId, productVariantId,
                    productId, categoryId, subCategoryId, search, sortBy, sortDescending));
                return Results.Ok(result);
            }).WithTags("Reports").RequirePermission(Permissions.ReportsView);
        }
    }
}
