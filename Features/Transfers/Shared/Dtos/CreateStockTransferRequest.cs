namespace Inventory_Management_System.Features.Transfers.Shared.Dtos
{
    public class CreateStockTransferRequest
    {
        public int SourceBranchId { get; set; }
        public int DestinationBranchId { get; set; }
        public List<TransferItemRequest> Items { get; set; } = [];
        public string? Notes { get; set; }
        public bool Dispatch { get; set; }
    }
}
