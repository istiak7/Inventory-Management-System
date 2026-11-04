using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.DataUpload.Queries.DownloadDataUploadErrorReport
{
    // Same form as Process. The file is checked again (nothing is saved) and the failed rows
    // come back as an Excel file the user can fix and upload again.
    public class DownloadDataUploadErrorReportQuery : IRequest<Result>
    {
        public int Type { get; set; }
        public IFormFile? File { get; set; }
        public string? Mappings { get; set; }
    }
}
