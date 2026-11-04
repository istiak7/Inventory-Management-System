using Inventory_Management_System.Features.DataUpload.Shared;

namespace Inventory_Management_System.Features.DataUpload.Queries.MatchDataUploadColumns
{
    public sealed record MatchDataUploadColumnsResponse(
        int Type,
        string TypeName,
        // Non-empty data rows under the heading row.
        int TotalRows,
        IReadOnlyList<UploadedHeader> UploadedHeaders,
        // One entry per system column, with the file column it was paired with (if any).
        IReadOnlyList<HeaderMatch> HeaderMatches,
        // File columns the system does not use; they are ignored on Process.
        int ExtraColumns);
}
