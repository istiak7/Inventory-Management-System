namespace Inventory_Management_System.Entities
{
    public class Expense : BaseEntity
    {
        public int BranchId { get; set; }
        public int ExpenseCategoryId { get; set; }

        public string? Name { get; set; }
        public string? Description { get; set; } = string.Empty;
        public decimal Amount { get; set; } = 0;
        public DateTime ExpenseDate { get; set; } = DateTime.Now;
        public string PaymentMethod { get; set; } = "Cash";
        public int RecordByUserId { get; set; }

        public required Branch Branch { get; set; }
        public required ExpenseCategory ExpenseCategory { get; set; }

    }
}
