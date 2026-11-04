using Inventory_Management_System.Features.DataUpload.Shared;

namespace Inventory_Management_System.Features.DataUpload.Command.ProcessDataUpload
{
    public sealed record ProcessDataUploadResponse(
        int Type,
        string TypeName,
        int TotalRows,
        // Records saved (0 when any row failed: the file is saved completely or not at all).
        int CreatedRecords,
        string Summary,
        int ErrorRows,
        IReadOnlyList<RowError> Errors);
}
