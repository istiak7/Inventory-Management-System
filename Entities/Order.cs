using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Inventory_Management_System.Entities
{
    public class Order : BaseEntity
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
    public class OutboxMessage : BaseEntity
    {
        public string EventType { get; set; } = string.Empty;
        public string Content { get; set; }
        public DateTime OccurredOn { get; set; }
        public DateTime ?ProcessedOn { get; set; }
        public string ?Errors { get; set; }
        public int RetryCount { get; set; } = 0;
        public OutBoxStatus Status { get; set; } = OutBoxStatus.Pending;
    }
    public enum OutBoxStatus
    {
        Pending = 0,
        Processed = 1,
        Failed = 2,
    }
}
