using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Database
{
    // One-time setup of System Setup -> Menu Action, so permissions work right after deploying:
    //
    //   1. Every page (found by its route) that has no actions yet gets its default actions
    //      (View / Create / Update / Delete and workflow steps), each with the API routes its
    //      screens call.
    //   2. Every existing role (except Admin, who always has everything) is given the new
    //      actions its old fixed permissions already allowed, so nobody gains or loses access.
    //   3. A "Salesman" role is created with only the day-to-day sales actions.
    //
    // It runs once (recorded in SeedHistory). After that, actions and grants belong to the admins
    // and are changed only from System Setup -> Menu Action and Users & Roles.
    public static class MenuActionSeed
    {
        private const string SeedName = "MenuActionDefaults.v1";
        public const string SalesmanRoleName = "Salesman";

        // Legacy = the fixed permission that allowed this before: "" = every logged-in user,
        // "admin" = admins only. Used only to carry existing roles' access over (step 2).
        private sealed record ActionDef(string Name, string Code, string Legacy, string[] Routes);

        private static ActionDef View(string legacy, params string[] routes) => new("View", "view", legacy, routes);
        private static ActionDef Create(string legacy, params string[] routes) => new("Create", "create", legacy, routes);
        private static ActionDef Update(string legacy, params string[] routes) => new("Update", "update", legacy, routes);
        private static ActionDef Delete(string legacy, params string[] routes) => new("Delete", "delete", legacy, routes);
        private static ActionDef Step(string name, string code, string legacy, params string[] routes) => new(name, code, legacy, routes);

        // Lookups many screens need (the warehouse picker, party pickers, product search).
        private const string Branches = "GET /get-all-branches";
        private const string Customers = "GET /get-all-customers";
        private const string Suppliers = "GET /get-all-suppliers";
        private const string ProductSearch = "GET /search-products";

        private static readonly Dictionary<string, ActionDef[]> Defaults = new(StringComparer.OrdinalIgnoreCase)
        {
            // ---------------------------------------------------------------- Inventory module
            ["/dashboard"] =
            [
                View("dashboard.view", "GET /get-dashboard-summary", Branches),
                // Profit figures are their own action, so a sales role can use the dashboard
                // without seeing margins.
                Step("View Profit", "profit", "reports.view", "GET /get-daily-gross-profit",
                    "GET /get-monthly-gross-profit"),
            ],
            ["/products"] =
            [
                View("products.view", "GET /get-all-products", "GET /get-products/{id:int}", "GET /get-product-variants",
                    "GET /get-all-brands", "GET /get-all-categories", "GET /get-all-sub-categories"),
                Create("products.manage", "POST /create-products", "POST /create-product-variants",
                    "GET /get-all-brands", "GET /get-all-sub-categories"),
                Update("products.manage", "PUT /update-products/{id:int}", "PUT /update-product-variants/{id:int}",
                    "GET /get-products/{id:int}", "GET /get-all-brands", "GET /get-all-sub-categories"),
                Delete("products.manage", "DELETE /delete-products/{id:int}"),
            ],
            ["/products/variants"] =
            [
                View("products.view", "GET /get-product-variants", "GET /get-all-products", ProductSearch),
                Create("products.manage", "POST /create-product-variants"),
                Update("products.manage", "PUT /update-product-variants/{id:int}"),
                Delete("products.manage", "DELETE /delete-product-variants/{id:int}"),
            ],
            ["/categories"] =
            [
                View("products.view", "GET /get-all-categories", "GET /get-all-sub-categories", "GET /categories/{id:int}",
                    "GET /sub-categories/{id:int}", "GET /categories/{categoryId:int}/sub-categories",
                    "GET /sub-categories/{subCategoryId:int}/products"),
                Create("products.manage", "POST /categories", "POST /create-sub-categories"),
                Update("products.manage", "PUT /update-categories/{id:int}", "PUT /update-sub-categories/{id:int}"),
                Delete("products.manage", "DELETE /delete-categories/{id:int}", "DELETE /delete-sub-categories/{id:int}"),
            ],
            ["/brands"] =
            [
                View("products.view", "GET /get-all-brands", "GET /get-brands/{id:int}", "GET /get-brands/{brandId:int}/products"),
                Create("products.manage", "POST /create-brands"),
                Update("products.manage", "PUT /update-brands/{id:int}"),
                Delete("products.manage", "DELETE /delete-brands/{id:int}"),
            ],
            ["/purchases"] =
            [
                View("purchases.view", "GET /get-all-purchase-orders", "GET /get-purchase-order/{id:int}",
                    "GET /get-invoice-supplier-ledger", Branches),
                Create("purchases.manage", "POST /create-purchase-order", Suppliers, ProductSearch, Branches),
                Update("purchases.manage", "PUT /update-purchase-order/{id:int}", Suppliers, ProductSearch),
                Delete("purchases.manage", "DELETE /delete-purchase-order/{id:int}"),
                Step("Receive", "receive", "purchases.manage", "POST /receive-purchase-order/{id:int}"),
                Step("Reject", "reject", "purchases.manage", "POST /reject-purchase-order/{id}"),
            ],
            ["/suppliers"] =
            [
                View("suppliers.view", Suppliers, "GET /get-suppliers/{id:int}"),
                Create("suppliers.manage", "POST /create-suppliers"),
                Update("suppliers.manage", "PUT /update-suppliers/{id:int}", "GET /get-suppliers/{id:int}"),
                Delete("suppliers.manage", "DELETE /delete-suppliers/{id:int}"),
            ],
            ["/suppliers/ledger"] =
            [
                View("suppliers.view", "GET /get-supplier-transactions", Suppliers, "GET /get-invoice-supplier-ledger", Branches),
            ],
            ["/suppliers/accounts"] =
            [
                View("suppliers.view", "GET /get-supplier-accounts", "GET /get-supplier-payment-history",
                    "GET /get-supplier-payments", "GET /get-invoice-supplier-ledger", Branches),
                Step("Make Payment", "payment", "suppliers.manage", "POST /create-supplier-payment"),
            ],
            ["/sales"] =
            [
                View("sales.view", "GET /get-all-sales", "GET /get-sale/{id:int}", "GET /get-invoice-customer-ledger", Branches),
                Create("sales.manage", "POST /create-sale", ProductSearch, "GET /get-available-serials",
                    "GET /get-customer-by-phone", Customers, Branches),
                // Money and stock are committed when a sale is made; only its remark can change.
                Step("Update Remarks", "update", "sales.manage", "PUT /update-sale-remarks/{id:int}"),
            ],
            ["/customers"] =
            [
                View("customers.view", Customers, "GET /get-customer-by-phone"),
                Create("customers.manage", "POST /create-customers"),
                Update("customers.manage", "PUT /update-customers/{id:int}"),
                Delete("customers.manage", "DELETE /delete-customers/{id:int}"),
            ],
            ["/customers/ledger"] =
            [
                View("customers.view", "GET /get-customer-transactions", Customers, "GET /get-invoice-customer-ledger", Branches),
            ],
            ["/customers/accounts"] =
            [
                View("customers.view", "GET /get-customer-accounts", "GET /get-customer-payment-history",
                    "GET /get-customer-payments", "GET /get-invoice-customer-ledger", Branches),
                Step("Collect Payment", "payment", "customers.manage", "POST /create-customer-payment"),
            ],
            // The Office Expense endpoints had no permission, so every role keeps them.
            ["/office-expenses"] =
            [
                View("", "GET /office-expense", "GET /office-expense/categories", Branches),
                Create("", "POST /create-office-expense", "GET /office-expense/categories", Branches),
                Update("", "PUT /office-expense", "GET /office-expense/categories"),
                Delete("", "DELETE /delete-office-expense/{id:int}"),
            ],
            ["/office-expenses/categories"] =
            [
                View("", "GET /office-expense/categories"),
                Create("", "POST /office-expense/categories"),
                Update("", "PUT /expense-categories"),
                Delete("", "DELETE /delete-expense-categories/{id:int}"),
            ],
            ["/warranty"] =
            [
                View("warranty.view", "GET /lookup-warranty-serial", "GET /get-sale/{id:int}"),
                Step("Create Claim", "create", "warranty.manage", "POST /create-warranty-claim"),
            ],
            ["/warranty/claims"] =
            [
                View("warranty.view", "GET /get-all-warranty-claims", "GET /get-warranty-claim/{id:int}",
                    "GET /get-replacement-candidates/{claimId:int}", Branches),
                Step("Process Claim", "process", "warranty.manage", "POST /start-warranty-repair/{id:int}",
                    "POST /resolve-warranty-claim/{id:int}", "POST /reject-warranty-claim/{id:int}",
                    "POST /deliver-warranty-claim/{id:int}"),
            ],
            ["/inventory"] =
            [
                View("inventory.view", "GET /get-all-stock", "GET /get-inventory-transactions",
                    "GET /get-branch-stock/{branchId:int}", "GET /get-branch-stock/{branchId:int}/variant/{productVariantId:int}",
                    "GET /get-stock-by-variant/{variantId:int}", "GET /get-stock-by-variant-and-branch", Branches),
            ],
            ["/warehouses"] =
            [
                View("branches.view", Branches, "GET /get-branch-stock/{branchId:int}", "GET /get-all-stock"),
                Create("branches.manage", "POST /create-branches"),
                Update("branches.manage", "PUT /update-branches/{id:int}"),
                Delete("branches.manage", "DELETE /delete-branches/{id:int}"),
            ],
            ["/transfers/new"] =
            [
                Create("transfers.manage", "POST /create-stock-transfer", ProductSearch, Branches,
                    "GET /get-stock-by-variant-and-branch", "GET /get-branch-stock/{branchId:int}/variant/{productVariantId:int}"),
            ],
            ["/transfers"] =
            [
                View("transfers.view", "GET /get-all-stock-transfers", "GET /get-stock-transfer-by-id/{id:int}", Branches),
                Step("Approve", "approve", "transfers.manage", "POST /approve-stock-transfer/{id:int}"),
                Step("Reject", "reject", "transfers.manage", "POST /reject-stock-transfer/{id:int}"),
            ],
            ["/reports/ledger"] =
            [
                View("reports.view", "GET /get-ledger-report", Customers, Suppliers, Branches),
                Step("Export", "export", "reports.view", "GET /export-ledger-report"),
            ],
            ["/reports/party-sales"] =
            [
                View("reports.view", "GET /get-party-sales-report", Customers, Branches),
                Step("Export", "export", "reports.view", "GET /export-party-sales-report"),
            ],
            ["/reports/party-purchase"] =
            [
                View("reports.view", "GET /get-party-purchase-report", Suppliers, Branches),
                Step("Export", "export", "reports.view", "GET /export-party-purchase-report"),
            ],
            ["/reports/payments"] =
            [
                View("reports.view", "GET /get-payment-report", Branches),
                Step("Export", "export", "reports.view", "GET /export-payment-report"),
            ],
            ["/reports/expenses"] =
            [
                View("reports.view", "GET /get-expense-report", "GET /office-expense/categories", Branches),
                Step("Export", "export", "reports.view", "GET /export-expense-report"),
            ],
            ["/reports/stock"] =
            [
                View("reports.view", "GET /get-stock-date-wise-report", "GET /get-all-categories",
                    "GET /get-all-sub-categories", Branches),
                Step("Export", "export", "reports.view", "GET /export-stock-date-wise-report"),
            ],
            // Each upload type still checks its own permission inside (DataUploadCatalog).
            ["/data-upload"] =
            [
                View("", "GET /get-data-upload-types", "GET /download-data-upload-template"),
                Step("Upload", "upload", "", "POST /match-data-upload-columns", "POST /process-data-upload",
                    "POST /download-data-upload-error-report"),
            ],

            // ---------------------------------------------------------------- Admin Panel module
            ["/module"] =
            [
                View("admin", "GET /get-all-modules"),
                Create("admin", "POST /create-module"),
                Update("admin", "PUT /update-module/{id:int}"),
                Delete("admin", "DELETE /delete-module/{id:int}"),
            ],
            ["/submodule"] =
            [
                View("admin", "GET /get-all-submodules", "GET /get-all-modules"),
                Create("admin", "POST /create-submodule"),
                Update("admin", "PUT /update-submodule/{id:int}"),
                Delete("admin", "DELETE /delete-submodule/{id:int}"),
            ],
            ["/menu"] =
            [
                View("admin", "GET /get-menus", "GET /get-all-submodules", "GET /get-all-modules"),
                Create("admin", "POST /create-menu"),
                Update("admin", "PUT /update-menu/{id:int}"),
                Delete("admin", "DELETE /delete-menu/{id:int}"),
            ],
            ["/menu-action"] =
            [
                View("admin", "GET /get-menu-actions", "GET /get-menus", "GET /get-all-submodules",
                    "GET /get-all-modules", "GET /get-api-routes", "GET /get-all-roles", "GET /get-menu-action-tree"),
                Create("admin", "POST /create-menu-action"),
                Update("admin", "PUT /update-menu-action/{id:int}"),
                Delete("admin", "DELETE /delete-menu-action/{id:int}"),
            ],
            ["/company-setup"] =
            [
                View("admin", "GET /get-company-setting"),
                Update("admin", "PUT /save-company-setting"),
            ],
            // Users and roles; a role's menu access is edited here, so the menu tree is read too.
            ["/users"] =
            [
                View("admin", "GET /get-all-users", "GET /get-user/{id:int}", "GET /get-all-roles", "GET /get-menu-action-tree",
                    "GET /get-all-permissions", "GET /get-menus", "GET /get-menu-actions", "GET /get-all-submodules",
                    "GET /get-all-modules", Branches),
                Create("admin", "POST /create-user", "POST /create-role"),
                Update("admin", "PUT /update-user/{id:int}", "PUT /update-role/{id:int}"),
                Delete("admin", "DELETE /delete-user/{id:int}", "DELETE /delete-role/{id:int}"),
            ],
            ["/settings"] =
            [
                View("", Branches),
            ],
        };

        // What a salesman does every day: sell, look up stock and products, look after customers
        // and their payments, and take in warranty claims. Nothing in purchasing, stock movement,
        // reports, configuration or user management.
        private static readonly (string Route, string[] Codes)[] SalesmanGrants =
        [
            ("/dashboard", ["view"]),
            ("/products", ["view"]),
            ("/inventory", ["view"]),
            ("/sales", ["view", "create", "update"]),
            ("/customers", ["view", "create", "update"]),
            ("/customers/ledger", ["view"]),
            ("/customers/accounts", ["view", "payment"]),
            ("/warranty", ["view", "create"]),
            ("/warranty/claims", ["view"]),
            ("/settings", ["view"]),
        ];

        public static async Task SeedAsync(
            AppDbContext db,
            IReadOnlySet<string> knownRoutes,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            if (await db.SeedHistory.AnyAsync(s => s.Name == SeedName, cancellationToken)) return;

            const int active = (int)EntityStatus.Active;
            var menus = await db.AppMenus
                .Where(m => m.IsActive == active && m.IsLeaf && m.FrontendRoute != "")
                .Include(m => m.Actions)
                .ToListAsync(cancellationToken);

            // Step 1: default actions for pages that have none.
            var created = new List<(AppMenuAction Action, string Route, string Legacy)>();
            foreach (var menu in menus)
            {
                if (!Defaults.TryGetValue(menu.FrontendRoute, out var defs)) continue;
                if (menu.Actions.Any(a => a.IsActive == active)) continue;

                foreach (var def in defs)
                {
                    var unknown = def.Routes.Where(r => !knownRoutes.Contains(r)).ToList();
                    if (unknown.Count > 0)
                        logger.LogWarning("Menu action seed: {Route} {Action} skips unknown API routes {Unknown}",
                            menu.FrontendRoute, def.Name, string.Join(", ", unknown));

                    var action = new AppMenuAction
                    {
                        Menu = menu,
                        Name = def.Name,
                        ComponentCode = def.Code,
                        ApiRoutes = def.Routes.Where(knownRoutes.Contains).Distinct().ToList(),
                        CreatedAt = DateTime.UtcNow,
                    };
                    db.AppMenuActions.Add(action);
                    created.Add((action, menu.FrontendRoute, def.Legacy));
                }
            }
            await db.SaveChangesAsync(cancellationToken);

            // Step 2: existing roles keep exactly what their fixed permissions allowed.
            var roles = await db.Roles
                .Where(r => r.IsActive == active && r.Name != MenuActionAccess.AdminRoleName)
                .Select(r => new
                {
                    r.Id,
                    Permissions = r.RolePermissions.Select(rp => rp.Permission.Name).ToList(),
                })
                .ToListAsync(cancellationToken);

            foreach (var role in roles)
            {
                foreach (var (action, _, legacy) in created)
                {
                    var allowed = legacy.Length == 0
                        || (legacy != "admin" && role.Permissions.Contains(legacy));
                    if (allowed)
                        db.RoleMenuActions.Add(new RoleMenuAction { RoleId = role.Id, MenuActionId = action.Id });
                }
            }

            // Step 3: the Salesman role.
            if (!await db.Roles.AnyAsync(r => r.Name == SalesmanRoleName, cancellationToken))
            {
                var salesman = new Role
                {
                    Name = SalesmanRoleName,
                    Description = "Sales counter: sales, customers and their payments, warranty intake",
                };
                db.Roles.Add(salesman);

                var actionsByRoute = await db.AppMenuActions
                    .Where(a => a.IsActive == active && a.Menu.IsActive == active)
                    .Select(a => new { a.Id, a.Menu.FrontendRoute, a.ComponentCode })
                    .ToListAsync(cancellationToken);

                foreach (var (route, codes) in SalesmanGrants)
                {
                    foreach (var action in actionsByRoute.Where(a =>
                                 string.Equals(a.FrontendRoute, route, StringComparison.OrdinalIgnoreCase)
                                 && codes.Contains(a.ComponentCode)))
                    {
                        db.RoleMenuActions.Add(new RoleMenuAction { Role = salesman, MenuActionId = action.Id });
                    }
                }
            }

            db.SeedHistory.Add(new SeedHistory { Name = SeedName, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Menu action seed: {Count} default actions created", created.Count);
        }
    }
}
