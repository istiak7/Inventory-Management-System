namespace Inventory_Management_System.Entities
{
    // One row per one-time seed step that has run (e.g. "MenuActionDefaults.v1"), so a step
    // never runs twice and never undoes what an admin changed afterwards.
    public class SeedHistory : BaseEntity
    {
        public required string Name { get; set; }
    }
}
