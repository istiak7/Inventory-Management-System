using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    public class WarrantyClaim : BaseEntity
    {
        public required string ClaimNumber { get; set; }   
        public int ProductSerialId { get; set; }  
        public int SaleDetailsId { get; set; }   
        public int CustomerId { get; set; }       
        public int BranchId { get; set; }        
        public required string DefectDescription { get; set; }
        public string? AccessoriesReceived { get; set; } 
        public string? TechnicianName { get; set; }
        public DateTime WarrantyExpiryDate { get; set; }
        public WarrantyClaimStatus Status { get; set; } = WarrantyClaimStatus.Open;
        public WarrantyResolutionType? Resolution { get; set; }   
        public string? ResolutionNotes { get; set; }
        public int? ReplacementSerialId { get; set; }

        public DateTime ClaimDate { get; set; } = DateTime.UtcNow;
        public DateTime? RepairStartedAt { get; set; }   
        public DateTime? ResolvedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }       

        // Navigation property
        public required ProductSerial ProductSerial { get; set; }
        public required SaleDetails SaleDetails { get; set; }
        public required Customer Customer { get; set; }
        public required Branch Branch { get; set; }
        public ProductSerial? ReplacementSerial { get; set; }

        public bool IsOpen => Status is WarrantyClaimStatus.Open or WarrantyClaimStatus.InRepair;
    }
}
