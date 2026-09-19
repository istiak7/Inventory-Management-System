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

        public Branch? Branch { get; set; }
        public ExpenseCategory? ExpenseCategory { get; set; }
        public User? RecordByUser { get; set; }

        public static Expense CreateExpense(string name, string description, decimal amount, int branchId, int expenseCategoryId, DateTime expenseDate, string paymentMethod, int recordByUserId)
        {
            return new Expense
            {
                Name = name,
                Description = description,
                Amount = amount,
                BranchId = branchId,
                ExpenseCategoryId = expenseCategoryId,
                ExpenseDate = expenseDate,
                RecordByUserId = recordByUserId,
                PaymentMethod = paymentMethod
            };
        }

    }
}
