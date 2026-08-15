using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Command.UpdateSaleRemarks
{
    // A completed sale is otherwise immutable — stock has already moved and the customer ledger
    // is written — so the remark is the only field that can be corrected afterwards. It carries
    // no money or stock meaning, which is exactly why editing it is safe.
    public class UpdateSaleRemarksCommand : IRequest<Result>
    {
        public int Id { get; set; }

        // Null or blank clears the note.
        public string? Remarks { get; set; }
    }
}
