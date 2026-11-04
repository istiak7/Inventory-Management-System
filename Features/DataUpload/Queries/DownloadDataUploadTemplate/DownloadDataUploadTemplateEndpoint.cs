using Inventory_Management_System.Features.DataUpload.Shared;
using Inventory_Management_System.Shared;
using System.Security.Claims;

namespace Inventory_Management_System.Features.DataUpload.Queries.DownloadDataUploadTemplate
{
    // The empty Excel file for one upload type: the heading row plus an Instructions sheet.
    public class DownloadDataUploadTemplateEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/download-data-upload-template", (int type, ClaimsPrincipal user) =>
            {
                var definition = DataUploadCatalog.Find(type);
                if (definition is null) return DataUploadCatalog.UnknownType(type);
                if (!DataUploadCatalog.CanUpload(user, definition)) return DataUploadCatalog.Forbidden(definition);

                return Results.File(
                    DataUploadWorkbook.Template(definition),
                    DataUploadWorkbook.ContentType,
                    DataUploadWorkbook.FileName(definition, "Template"));
            }).WithTags("DataUpload").RequireAuthorization();
        }
    }
}
