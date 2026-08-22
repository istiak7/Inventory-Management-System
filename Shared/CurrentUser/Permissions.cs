namespace Inventory_Management_System.Shared.CurrentUser
{
    // Permission names used by endpoints and the seeder. One place to avoid typos.
    public static class Permissions
    {
        public const string DashboardView = "dashboard.view";

        public const string ProductsView = "products.view";
        public const string ProductsManage = "products.manage";

        public const string InventoryView = "inventory.view";
        public const string InventoryManage = "inventory.manage";

        public const string PurchasesView = "purchases.view";
        public const string PurchasesManage = "purchases.manage";

        public const string SalesView = "sales.view";
        public const string SalesManage = "sales.manage";

        public const string SuppliersView = "suppliers.view";
        public const string SuppliersManage = "suppliers.manage";

        public const string CustomersView = "customers.view";
        public const string CustomersManage = "customers.manage";

        public const string TransfersView = "transfers.view";
        public const string TransfersManage = "transfers.manage";

        public const string WarrantyView = "warranty.view";
        public const string WarrantyManage = "warranty.manage";

        public const string BranchesView = "branches.view";
        public const string BranchesManage = "branches.manage";

        public const string ReportsView = "reports.view";

        public const string UsersManage = "users.manage";
        public const string RolesManage = "roles.manage";

        // Full list, used by the seeder to create the permission rows.
        public static readonly (string Name, string Description)[] All =
        [
            (DashboardView,   "View the dashboard"),
            (ProductsView,    "View products and catalog"),
            (ProductsManage,  "Create and edit products, categories and brands"),
            (InventoryView,   "View inventory and stock"),
            (InventoryManage, "Adjust inventory and stock"),
            (PurchasesView,   "View purchase orders"),
            (PurchasesManage, "Create and receive purchase orders"),
            (SalesView,       "View sales"),
            (SalesManage,     "Create sales"),
            (SuppliersView,   "View suppliers"),
            (SuppliersManage, "Create and edit suppliers"),
            (CustomersView,   "View customers"),
            (CustomersManage, "Create and edit customers"),
            (TransfersView,   "View stock transfers"),
            (TransfersManage, "Create and approve stock transfers"),
            (WarrantyView,    "View warranty claims"),
            (WarrantyManage,  "Handle warranty claims"),
            (BranchesView,    "View branches"),
            (BranchesManage,  "Create and edit branches"),
            (ReportsView,     "View reports"),
            (UsersManage,     "Create and edit users"),
            (RolesManage,     "Create and edit roles"),
        ];

        // Default day-to-day access for branch staff.
        public static readonly string[] StaffDefaults =
        [
            DashboardView,
            ProductsView,
            InventoryView,
            SalesView, SalesManage,
            PurchasesView,
            SuppliersView,
            CustomersView, CustomersManage,
            TransfersView,
            WarrantyView, WarrantyManage,
            BranchesView,
            ReportsView,
        ];
    }
}
