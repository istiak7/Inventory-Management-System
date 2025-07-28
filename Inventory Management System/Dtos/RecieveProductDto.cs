namespace Inventory_Management_System.Dtos
{
    public class RecieveProductDto
    {
        public int StockId { get; set; }
        public int GoodQuantity { get; set; }
        public int BadQuantity { get; set; }
        public int MissingQuantity { get; set; }
    }
}
