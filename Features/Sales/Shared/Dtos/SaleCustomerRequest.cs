namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    /// <summary>
    /// A customer supplied inline by the sales form, for the case where the till typed a mobile
    /// number that is not on file yet. Sent instead of <c>CustomerId</c>, never alongside it.
    ///
    /// The server resolves it by phone number rather than trusting the client's "this one is new":
    /// between the form's lookup and its save, someone else may have registered the same number,
    /// so this is a find-or-create, not a create. That resolution happens inside the sale's own
    /// transaction — if the sale then fails on stock, the customer is rolled back with it instead
    /// of being left behind as a record for a sale that never happened.
    ///
    /// Deliberately narrower than <c>CreateCustomerCommand</c>: no opening balance. A customer met
    /// at the counter starts owing nothing, and the amount they owe from this sale is what the
    /// sale's own ledger row is for. Use the Customers screen to register a prior balance.
    /// </summary>
    public class SaleCustomerRequest
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string NID { get; set; } = string.Empty;
    }
}
