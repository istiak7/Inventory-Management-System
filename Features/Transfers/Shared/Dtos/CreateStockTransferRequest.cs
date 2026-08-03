namespace Inventory_Management_System.Features.Transfers.Shared.Dtos
{
    public class CreateStockTransferRequest
    {
        public int SourceBranchId { get; set; }
        public int DestinationBranchId { get; set; }
        public List<TransferItemRequest> Items { get; set; } = [];
        public string? Notes { get; set; }
        // false parks the transfer as a Draft (no approval requested, no stock impact); true
        // submits it as Pending, ready for an approver to act on. Either way the actual stock
        // move only happens when the transfer is approved.
        public bool Dispatch { get; set; }
    }
}
