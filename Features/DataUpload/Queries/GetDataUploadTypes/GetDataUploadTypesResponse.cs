namespace Inventory_Management_System.Features.DataUpload.Queries.GetDataUploadTypes
{
    public sealed record DataUploadColumnResponse(int Order, string Name, bool Required, string Hint, int? MaxLength);

    public sealed record DataUploadTypeResponse(
        int Id,
        string Code,
        string Name,
        string Description,
        IReadOnlyList<DataUploadColumnResponse> Columns);
}
