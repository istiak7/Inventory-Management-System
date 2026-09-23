using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Entities
{
    public class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpDatedAt { get; set; }

        // Who created / last changed the row (user id). Null for rows made by the system,
        // e.g. the seeder, or rows that existed before this was added.
        // Filled automatically by AppDbContext.SaveChanges from the logged-in user.
        public int? CreatedById { get; set; }
        public int? UpdatedById { get; set; }

        // See EntityStatus: 0 = Active, 1 = InActive, 2 = Deleted.
        public int IsActive { get; set; } = (int)EntityStatus.Active;

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
