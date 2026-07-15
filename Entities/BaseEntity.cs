using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Entities
{
    public class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpDatedAt { get; set; }
        public int IsActive { get; set; } = 1;

        public void Active()
        {
            IsActive = (int)EntityStatus.Active;
        }
        public void InActive()
        {
            IsActive = (int)EntityStatus.InActive;
        }
        public void Delete()
        {
            IsActive = (int)EntityStatus.Deleted;
        }
    }
}
