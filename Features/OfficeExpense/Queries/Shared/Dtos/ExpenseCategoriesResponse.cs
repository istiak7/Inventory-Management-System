namespace Inventory_Management_System.Features.OfficeExpense.Queries.Shared.Dtos
{
    public class ExpenseCategoriesResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
