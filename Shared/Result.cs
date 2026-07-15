namespace Inventory_Management_System.Shared
{
    public sealed class Result
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}
