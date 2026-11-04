using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.DataUpload.Queries.MatchDataUploadColumns
{
    // Sent as multipart/form-data: the Excel file and the upload type.
    // Nothing is saved: the file is only read to pair its headings with the system columns.
    public class MatchDataUploadColumnsQuery : IRequest<Result>
    {
        public int Type { get; set; }
        public IFormFile? File { get; set; }
    }
}
