namespace Inventory_Management_System.Entities
{
    public class ExpenseCategory : BaseEntity
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public ICollection<Expense> Expenses { get; set; }
    }
}
