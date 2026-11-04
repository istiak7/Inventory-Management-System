using Inventory_Management_System.Database;
using Inventory_Management_System.Features.DataUpload.Shared;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.DataUpload.Queries.DownloadDataUploadErrorReport
{
    public class DownloadDataUploadErrorReportQueryHandler(
        DataUploadRunner _runner,
        AppDbContext _db
    ) : IRequestHandler<DownloadDataUploadErrorReportQuery, Result>
    {
        public async Task<Result> Handle(DownloadDataUploadErrorReportQuery request, CancellationToken cancellationToken)
        {
            var definition = DataUploadCatalog.Find(request.Type)!;

            var (upload, error) = await _runner.CheckAsync(definition, request.File!, request.Mappings, cancellationToken);

            // The check may have staged new records; this request never saves, so drop them.
            _db.ChangeTracker.Clear();

            if (upload is null)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = error! };

            if (upload.Result.Errors.Count == 0)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "The file has no errors any more. You can process it now." };

            return new Result
            {
                IsSuccess = true,
                StatusCode = 200,
                Status = "Success",
                Message = "Error report created",
                Data = new DataUploadErrorReportFile(
                    DataUploadWorkbook.ErrorReport(definition, upload.Headers, upload.Rows, upload.Result.Errors),
                    DataUploadWorkbook.FileName(definition, "Errors"))
            };
        }
    }
}
