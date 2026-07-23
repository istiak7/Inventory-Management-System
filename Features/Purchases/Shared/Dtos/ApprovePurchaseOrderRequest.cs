namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public class ApprovePurchaseOrderRequest
    {
        // Optional: null/omitted = approve on account (full due), Amount == total = full payment, otherwise partial.
        public PaymentRequest? Payment { get; set; }
    }
}
