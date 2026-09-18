namespace Inventory_Management_System.Features.OfficeExpense.Queries.Shared.Dtos
{
    public class ExpenseResponse
    {
        public string Expensecategories { get; set; } = string.Empty;
        public string ExpenseName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string EntryBy { get; set; } = string.Empty;
    }
}
