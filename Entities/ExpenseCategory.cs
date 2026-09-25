namespace Inventory_Management_System.Entities
{
    public class ExpenseCategory : BaseEntity
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public ICollection<Expense> Expenses { get; set; }


        // Create Factory Static Method
        public static ExpenseCategory Create(string name, string? description = null)
        {
            return new ExpenseCategory
            {
                Name = name,
                Description = description
            };
        }

        public void UpdateCategory(string name, string? description = null)
        {
            Name = name;
            Description = description;
            
        }
    }
}
