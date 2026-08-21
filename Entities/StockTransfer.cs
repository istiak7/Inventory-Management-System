using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    public class StockTransfer : BaseEntity
    {
        public required string Reference { get; set; }   
        public int SourceBranchId { get; set; }      
        public int DestinationBranchId { get; set; }
        public TransferStatus Status { get; set; } = TransferStatus.Draft;
        public string? Notes { get; set; }

        public DateTime? SubmittedAt { get; set; }   
        public DateTime? ApprovedAt { get; set; }     
        public DateTime? RejectedAt { get; set; }

        // Navigation property
        public required Branch SourceBranch { get; set; }
        public required Branch DestinationBranch { get; set; }
        public ICollection<StockTransferDetails> StockTransferDetails { get; set; } = [];
    }
}
