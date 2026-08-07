using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    // One after-sales service job against one physical unit. The claim owns nothing that can be
    // derived: the warranty terms live on the sale line that sold the unit, the customer lives on
    // that line's invoice. What it does own is the intake decision and the outcome.
    //
    // Replacement is settled here rather than as a zero-price sale: a warranty swap is not revenue
    // and must never appear in the sales books. The Resolve handler moves the stock, flips both
    // serials and records ReplacementSerialId — this row IS the audit trail of the swap, which is
    // why the original sale line is left untouched.
    public class WarrantyClaim : BaseEntity
    {
        public required string ClaimNumber { get; set; }   // e.g. WC-2026-0001, globally unique
        public int ProductSerialId { get; set; }  //FK -> the unit the customer brought in
        public int SaleDetailsId { get; set; }    //FK -> the line that sold it (warranty terms + invoice)
        public int CustomerId { get; set; }       //FK, denormalized from the sale for "all claims by this customer"
        public int BranchId { get; set; }         //FK -- the branch that received the unit

        public required string DefectDescription { get; set; }
        public string? AccessoriesReceived { get; set; }   // "charger, box" — what physically came in with it
        public string? TechnicianName { get; set; }

        // Snapshot of the terms that were true the day intake accepted this claim. Stored, not
        // recomputed, so a later correction to the sale line's warranty can never retroactively
        // make an accepted claim look like it should have been refused.
        public DateTime WarrantyExpiryDate { get; set; }

        public WarrantyClaimStatus Status { get; set; } = WarrantyClaimStatus.Open;
        public WarrantyResolutionType? Resolution { get; set; }   // null until the claim ends
        public string? ResolutionNotes { get; set; }
        public int? ReplacementSerialId { get; set; } //FK -> the unit issued instead; only when Resolution == Replaced

        public DateTime ClaimDate { get; set; } = DateTime.UtcNow;
        public DateTime? RepairStartedAt { get; set; }   // Open -> InRepair
        public DateTime? ResolvedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }       // handed back to the customer

        // Navigation property
        public required ProductSerial ProductSerial { get; set; }
        public required SaleDetails SaleDetails { get; set; }
        public required Customer Customer { get; set; }
        public required Branch Branch { get; set; }
        public ProductSerial? ReplacementSerial { get; set; }

        /// <summary>
        /// True while the shop still physically holds the unit and can act on it. Both terminal
        /// outcomes and the collection step are past that point.
        /// </summary>
        public bool IsOpen => Status is WarrantyClaimStatus.Open or WarrantyClaimStatus.InRepair;
    }
}
