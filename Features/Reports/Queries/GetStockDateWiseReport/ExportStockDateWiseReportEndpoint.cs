using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Features.Reports.Shared.Dtos;
using Inventory_Management_System.Features.Reports.Shared.Export;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetStockDateWiseReport
{
    public class ExportStockDateWiseReportEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/export-stock-date-wise-report", async (
                IMediator mediator,
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
                    1, ReportPaging.MaxExportPageSize, startDate, endDate, branchId, productVariantId,
                    productId, categoryId, subCategoryId, search, sortBy, sortDescending)
                { IsExport = true });

                if (!result.IsSuccess || result.Data is not StockDateWiseReportResponse report)
                    return Results.Ok(result);

                var rows = report.Rows.Items;
                var summary = report.Summary;

                var bytes = ReportWorkbook.Build(
                    "Stock Date-wise",
                    "Stock Report (Date-wise)",
                    new[]
                    {
                        ("Date range", ReportWorkbook.DateRangeLabel(startDate, endDate)),
                        ("Search", ReportWorkbook.Or(search, "-")),
                        ("Rows", rows.Count.ToString()),
                    },
                    new ReportColumn<StockDateWiseRow>[]
                    {
                        new("SKU", r => r.SKU),
                        new("Product", r => r.ProductName),
                        new("Category", r => r.CategoryName),
                        new("Sub category", r => r.SubCategoryName),
                        new("Warehouse", r => r.BranchName),
                        new("Opening", r => r.OpeningStock, ReportWorkbook.WholeNumberFormat),
                        new("Purchase in", r => r.PurchaseInQuantity, ReportWorkbook.WholeNumberFormat),
                        new("Return in", r => r.ReturnInQuantity, ReportWorkbook.WholeNumberFormat),
                        new("Transfer in", r => r.TransferInQuantity, ReportWorkbook.WholeNumberFormat),
                        new("Total in", r => r.StockInQuantity, ReportWorkbook.WholeNumberFormat),
                        new("Sale out", r => r.SaleOutQuantity, ReportWorkbook.WholeNumberFormat),
                        new("Return out", r => r.ReturnOutQuantity, ReportWorkbook.WholeNumberFormat),
                        new("Transfer out", r => r.TransferOutQuantity, ReportWorkbook.WholeNumberFormat),
                        new("Damage out", r => r.DamageOutQuantity, ReportWorkbook.WholeNumberFormat),
                        new("Total out", r => r.StockOutQuantity, ReportWorkbook.WholeNumberFormat),
                        new("Adjustment", r => r.AdjustmentQuantity, ReportWorkbook.WholeNumberFormat),
                        new("Closing", r => r.ClosingStock, ReportWorkbook.WholeNumberFormat),
                    },
                    rows,
                    new ReportTotal[]
                    {
                        new("Items", summary.ItemCount, ReportWorkbook.WholeNumberFormat),
                        new("Opening stock", summary.OpeningStock, ReportWorkbook.WholeNumberFormat),
                        new("Stock in", summary.StockInQuantity, ReportWorkbook.WholeNumberFormat),
                        new("Stock out", summary.StockOutQuantity, ReportWorkbook.WholeNumberFormat),
                        new("Adjustment", summary.AdjustmentQuantity, ReportWorkbook.WholeNumberFormat),
                        new("Closing stock", summary.ClosingStock, ReportWorkbook.WholeNumberFormat),
                    });

                return Results.File(bytes, ReportWorkbook.ContentType,
                    ReportWorkbook.FileName("Stock_Datewise", startDate, endDate));
            }).WithTags("Reports").RequirePermission(Permissions.ReportsView);
        }
    }
}
