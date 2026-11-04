using Inventory_Management_System.Database;
using Inventory_Management_System.Features.DataUpload.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Inventory_Management_System.Features.DataUpload.Command.ProcessDataUpload
{
    /// <summary>
    /// Reads the file, checks every row, and saves all rows in one transaction.
    /// If even one row is wrong nothing is saved, and every wrong row is returned with its reasons.
    /// </summary>
    public class ProcessDataUploadCommandHandler(
        DataUploadRunner _runner,
        AppDbContext _db,
        ILogger<ProcessDataUploadCommandHandler> _logger
    ) : IRequestHandler<ProcessDataUploadCommand, Result>
    {
        public async Task<Result> Handle(ProcessDataUploadCommand request, CancellationToken cancellationToken)
        {
            var definition = DataUploadCatalog.Find(request.Type)!;

            var (upload, error) = await _runner.CheckAsync(definition, request.File!, request.Mappings, cancellationToken);
            if (upload is null)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = error! };

            var check = upload.Result;
            var totalRows = upload.Rows.Count;

            if (check.Errors.Count > 0)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"{check.Errors.Count} of {totalRows} row(s) have errors, so nothing was saved. Fix them and upload the file again.",
                    Data = new ProcessDataUploadResponse((int)definition.Type, definition.Name, totalRows, 0, string.Empty, check.Errors.Count, check.Errors)
                };
            }

            try
            {
                // One transaction for the whole file: every row is saved, or none is.
                await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Someone saved the same SKU / email / phone between our check and our save.
                _logger.LogWarning(ex, "Data upload of {Type} hit a unique index", definition.Type);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 409,
                    Status = "Error",
                    Message = "Some of these records were just saved by someone else, so nothing was saved. Please process the file again to see which rows already exist."
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error saving data upload of {Type}", definition.Type);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while saving the file. Nothing was saved."
                };
            }

            return new Result
            {
                IsSuccess = true,
                StatusCode = 201,
                Status = "Success",
                Message = $"Upload completed: {check.Summary} saved from {totalRows} row(s).",
                Data = new ProcessDataUploadResponse((int)definition.Type, definition.Name, totalRows, check.CreatedRecords, check.Summary, 0, [])
            };
        }
    }
}
