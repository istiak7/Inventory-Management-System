using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.SalesReturns.Command.CreateSalesReturn
{
    public class CreateSalesReturnCommand : IRequest<Result>
    {
        public int SaleId { get; set; }
        // Defaults to now. Profit reports count the return on this date.
        public DateTime? ReturnDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public List<SalesReturnLineRequest> Lines { get; set; } = [];
    }

    public class SalesReturnLineRequest
    {
        public int SaleDetailsId { get; set; }
        public int Quantity { get; set; }
        // True = back into sellable stock. False = damaged, not restocked.
        public bool Restock { get; set; } = true;
    }
}
