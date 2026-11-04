using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.DataUpload.Command.ProcessDataUpload
{
    // Sent as multipart/form-data.
    public class ProcessDataUploadCommand : IRequest<Result>
    {
        public int Type { get; set; }
        public IFormFile? File { get; set; }

        // JSON list of { systemColumn, uploadedColumn } pairs from the Match Columns step,
        // e.g. [{"systemColumn":1,"uploadedColumn":"B"}]. Empty = match the headings automatically.
        public string? Mappings { get; set; }
    }
}
