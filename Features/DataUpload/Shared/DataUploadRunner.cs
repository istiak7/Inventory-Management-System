using Inventory_Management_System.Features.DataUpload.Shared.Importers;

namespace Inventory_Management_System.Features.DataUpload.Shared
{
    /// <summary>A file that has been read and checked.</summary>
    public sealed record CheckedUpload(
        IReadOnlyList<UploadedHeader> Headers,
        IReadOnlyList<UploadRow> Rows,
        UploadCheckResult Result);

    /// <summary>
    /// The steps Process and the error report share: open the file, apply the column mapping,
    /// read the rows and let the importer of the type check them.
    /// </summary>
    public class DataUploadRunner(IEnumerable<IUploadImporter> _importers)
    {
        public async Task<(CheckedUpload? Upload, string? Error)> CheckAsync(
            UploadTypeDefinition definition,
            IFormFile file,
            string? mappingsJson,
            CancellationToken cancellationToken)
        {
            var (sheet, openError) = await UploadFileReader.OpenAsync(file, cancellationToken);
            if (sheet is null) return (null, openError);

            using (sheet)
            {
                var (map, mappingError) = UploadFileReader.ResolveMapping(definition, sheet.Headers, mappingsJson);
                if (map is null) return (null, mappingError);

                var rows = UploadFileReader.ReadRows(sheet, map);

                if (rows.Count == 0)
                    return (null, "The file has no data rows under the heading row.");

                if (rows.Count > UploadFileReader.MaxDataRows)
                    return (null, $"The file has {rows.Count:N0} rows; the limit is {UploadFileReader.MaxDataRows:N0} per upload. Please split it into smaller files.");

                var importer = _importers.FirstOrDefault(i => i.Type == definition.Type)
                    ?? throw new InvalidOperationException($"No importer is registered for {definition.Type}.");

                var result = await importer.CheckAndStageAsync(rows, cancellationToken);
                return (new CheckedUpload(sheet.Headers, rows, result), null);
            }
        }
    }
}
