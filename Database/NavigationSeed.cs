using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Database
{
    // The first menu tree, written once when the Modules table is empty. After that the menus
    // belong to the admins (System Setup -> Module / Submodule / Menu) and are never overwritten.
    // Icons are lucide icon names; the frontend maps them to components.
    public static class NavigationSeed
    {
        private sealed record Item(
            string Name,
            string Route = "",
            string Icon = "",
            string Permission = "",
            Item[]? Children = null);

        private static Item Page(string name, string route, string icon, string permission = "") =>
            new(name, route, icon, permission);

        private static Item Folder(string name, string icon, params Item[] children) =>
            new(name, Icon: icon, Children: children);

        private static readonly (string Module, string Icon, string Submodule, Item[] Menus)[] Tree =
        [
            ("Inventory", "Boxes", "Operations",
            [
                Folder("General", "",
                    Page("Dashboard", "/dashboard", "LayoutDashboard", "dashboard.view")),
                Folder("Catalog", "",
                    Page("Products", "/products", "Package", "products.view"),
                    Page("Product Variants", "/products/variants", "Boxes", "products.view"),
                    Page("Categories", "/categories", "Tags", "products.view"),
                    Page("Brands", "/brands", "Tag", "products.view")),
                Folder("Operations", "",
                    Folder("Purchasing", "ShoppingCart",
                        Page("Purchase", "/purchases", "", "purchases.view"),
                        Page("Suppliers", "/suppliers", "", "suppliers.view"),
                        Page("Supplier Ledger", "/suppliers/ledger", "", "suppliers.view")),
                    Folder("Sales", "Receipt",
                        Page("Sales", "/sales", "", "sales.view"),
                        Page("Price Quotation", "/quotations", "", "sales.view"),
                        Page("Customers", "/customers", "", "customers.view"),
                        Page("Customer Ledger", "/customers/ledger", "", "customers.view")),
                    Folder("Accounts", "Wallet",
                        Page("Supplier Account", "/suppliers/accounts", "", "suppliers.view"),
                        Page("Customer Account", "/customers/accounts", "", "customers.view")),
                    // The Office Expense endpoints have no permission, so neither do their menus.
                    Folder("Office Expense", "HandCoins",
                        Page("Expenses", "/office-expenses", ""),
                        Page("Expense Categories", "/office-expenses/categories", ""))),
                Folder("After Sales", "",
                    Folder("Warranty", "BadgeCheck",
                        Page("Warranty Lookup", "/warranty", "", "warranty.view"),
                        Page("Warranty Claims", "/warranty/claims", "", "warranty.view"))),
                Folder("Stock", "",
                    Page("Inventory", "/inventory", "Boxes", "inventory.view"),
                    Page("Warehouses", "/warehouses", "Warehouse", "branches.view"),
                    Folder("Stock Transfers", "ArrowLeftRight",
                        Page("New Transfer", "/transfers/new", "", "transfers.manage"),
                        Page("All Transfers", "/transfers", "", "transfers.view"))),
                Folder("Insights", "",
                    Folder("Reports", "BarChart3",
                        Page("Report Center", "/reports", "", "reports.view"),
                        Page("Party Ledger", "/reports/ledger", "", "reports.view"),
                        Page("Customer Sales", "/reports/party-sales", "", "reports.view"),
                        Page("Supplier Purchases", "/reports/party-purchase", "", "reports.view"),
                        Page("Payments", "/reports/payments", "", "reports.view"),
                        Page("Office Expenses", "/reports/expenses", "", "reports.view"),
                        Page("Stock Movement", "/reports/stock", "", "reports.view"))),
                Folder("Data Upload", "",
                    Page("Data Upload", "/data-upload", "FileUp")),
            ]),
            ("Admin Panel", "ShieldCheck", "Administration",
            [
                // "admin" = admins only (no permission grants it).
                Folder("System Setup", "",
                    Page("Module", "/module", "Boxes", "admin"),
                    Page("Submodule", "/submodule", "Layers", "admin"),
                    Page("Menu", "/menu", "ListTree", "admin"),
                    Page("Menu Action", "/menu-action", "MousePointerClick", "admin")),
                Folder("Configuration", "",
                    Page("Company Setup", "/company-setup", "Building2", "admin")),
                Folder("System", "",
                    Page("Users & Roles", "/users", "ShieldCheck", "admin"),
                    Page("Settings", "/settings", "Settings")),
            ]),
        ];

        public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken)
        {
            if (await db.AppModules.AnyAsync(cancellationToken)) return;

            for (var m = 0; m < Tree.Length; m++)
            {
                var (moduleName, icon, submoduleName, menus) = Tree[m];
                var module = new AppModule { Name = moduleName, Icon = icon, DisplayOrder = m + 1 };
                var submodule = new AppSubmodule { Name = submoduleName, DisplayOrder = 1, Module = module };
                module.Submodules.Add(submodule);

                for (var i = 0; i < menus.Length; i++)
                    submodule.Menus.Add(ToMenu(menus[i], i + 1, submodule, null));

                db.AppModules.Add(module);
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        private static AppMenu ToMenu(Item item, int order, AppSubmodule submodule, AppMenu? parent)
        {
            var menu = new AppMenu
            {
                Name = item.Name,
                FrontendRoute = item.Route,
                Icon = item.Icon,
                Permission = item.Permission,
                MenuOrder = order,
                IsLeaf = item.Children is null,
                Submodule = submodule,
                Parent = parent,
            };

            if (item.Children is not null)
            {
                for (var i = 0; i < item.Children.Length; i++)
                {
                    var child = ToMenu(item.Children[i], i + 1, submodule, menu);
                    menu.Children.Add(child);
                    submodule.Menus.Add(child);
                }
            }
            return menu;
        }
    }
}
