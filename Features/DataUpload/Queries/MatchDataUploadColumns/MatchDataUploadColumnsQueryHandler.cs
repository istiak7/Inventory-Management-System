using Inventory_Management_System.Features.DataUpload.Shared;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.DataUpload.Queries.MatchDataUploadColumns
{
    public class MatchDataUploadColumnsQueryHandler : IRequestHandler<MatchDataUploadColumnsQuery, Result>
    {
        public async Task<Result> Handle(MatchDataUploadColumnsQuery request, CancellationToken cancellationToken)
        {
            var definition = DataUploadCatalog.Find(request.Type)!;

            var (sheet, error) = await UploadFileReader.OpenAsync(request.File!, cancellationToken);
            if (sheet is null)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = error! };

            using (sheet)
            {
                var matches = HeaderMatcher.Match(definition.Columns, sheet.Headers);
                var usedColumns = matches.Where(m => m.UploadedColumn is not null).Select(m => m.UploadedColumn).ToHashSet();

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Columns matched. Please check the pairs and save.",
                    Data = new MatchDataUploadColumnsResponse(
                        (int)definition.Type,
                        definition.Name,
                        UploadFileReader.CountDataRows(sheet),
                        sheet.Headers,
                        matches,
                        sheet.Headers.Count(h => h.Name.Length > 0 && !usedColumns.Contains(h.Column)))
                };
            }
        }
    }
}
