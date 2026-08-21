namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    public class CreateSaleRequest
    {
        public int? CustomerId { get; set; }
        public SaleCustomerRequest? Customer { get; set; }

        public int BranchId { get; set; }
        public DateTime? SaleDate { get; set; }

        public string? InvoiceNumber { get; set; }

        public string? Remarks { get; set; }

        public List<SaleItemRequest> Items { get; set; } = [];

        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }

        public SalePaymentRequest? Payment { get; set; }
    }
}
