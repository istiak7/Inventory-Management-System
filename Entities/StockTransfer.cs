using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    // Header of a branch-to-branch stock move. Draft/Pending carry no stock impact yet — the
    // approval button is what actually decrements the source and credits the destination
    // (see StockTransferDetails and the Approve handler); Rejected likewise never touches stock.
    public class StockTransfer : BaseEntity
    {
        public required string Reference { get; set; }   // e.g. TR-2026-0001, globally unique
        public int SourceBranchId { get; set; }      //FK
        public int DestinationBranchId { get; set; } //FK
        public TransferStatus Status { get; set; } = TransferStatus.Draft;
        public string? Notes { get; set; }

        public DateTime? SubmittedAt { get; set; }   // Draft -> Pending (dispatch requested at creation)
        public DateTime? ApprovedAt { get; set; }     // stock actually moved at this moment
        public DateTime? RejectedAt { get; set; }

        // Navigation property
        public required Branch SourceBranch { get; set; }
        public required Branch DestinationBranch { get; set; }
        public ICollection<StockTransferDetails> StockTransferDetails { get; set; } = [];
    }
}
