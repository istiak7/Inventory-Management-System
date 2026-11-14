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
        // The sale's printable delivery challan, opened from Sale Details beside the invoice.
        private const string SaleChallan = "GET /get-sale-challan/{id:int}";

        // API routes added after MenuActionDefaults.v1 had already run. That seed never runs
        // again, so on an existing database each batch appends its routes to the page's existing
        // action instead (once, recorded in SeedHistory) — whoever could already open that action
        // can use the new route, and nobody else. Defaults above carry them for new databases.
        private static readonly (string SeedName, string MenuRoute, string ActionCode, string[] ApiRoutes)[] RouteAdditions =
        [
            ("MenuActionRoutes.SaleChallan", "/sales", "view", [SaleChallan]),
            ("MenuActionRoutes.SaleEdits", "/sales", "view", [SaleEdits]),
        ];

        // Whole actions added to an existing page after MenuActionDefaults.v1 had already run
        // (once each, recorded in SeedHistory). Nobody is granted them: Admin always has every
        // action, and an admin ticks them for other roles in Users & Roles.
        private static readonly (string SeedName, string MenuRoute, string Name, string Code, string[] ApiRoutes)[] ActionAdditions =
        [
            ("MenuActionAdded.SaleEdit", "/sales", "Edit Invoice", "edit", [EditSale, ProductSearch, "GET /get-available-serials"]),
        ];

        // Actions whose API was removed after MenuActionDefaults.v1 had already run. On an existing
        // database each one is soft-deleted once (SeedHistory name), so it leaves System Setup and
        // the Users & Roles grid. Role grants stay on the deleted row (nothing reads them).
        private static readonly (string SeedName, string MenuRoute, string ActionCode)[] ActionRemovals =
        [
            // Sale Void (POST /void-sale) was replaced by Sales Return, which also handles partial returns.
            ("MenuActionRemoved.SaleVoid", "/sales", "void"),
        ];

        private const string EditSale = "PUT /edit-sale/{id:int}";
        private const string SaleEdits = "GET /get-sale-edits/{saleId:int}";

        // Price Quotation's API routes (Features/Quotations).
        private const string QuotationList = "GET /get-all-quotations";
        private const string QuotationById = "GET /get-quotation/{id:int}";

        // Pages added after MenuActionDefaults.v1. Each runs once (SeedHistory name), on new and
        // existing databases alike: it creates the menu row beside its sibling page when the menu
        // tree has none (existing databases; NavigationSeed already made it on new ones), adds the
        // page's actions if it has none, and grants the listed actions to the Salesman role.
        // Other roles get nothing until an admin ticks the actions in Users & Roles; Admin always
        // has everything.
        // InsertBefore puts the new menu in front of its sibling instead of after it.
        // ShareWithRolesOn: every role that already holds an action on one of these pages also gets
        // the SalesmanCodes actions, so a hub page (Report Center) appears for whoever already
        // uses the pages it links to.
        private sealed record PageAddition(
            string SeedName, string Route, string Name, string SiblingRoute, string Permission,
            ActionDef[] Actions, string[] SalesmanCodes,
            bool InsertBefore = false, string[]? ShareWithRolesOn = null);

        // The report pages the Report Center links to.
        private static readonly string[] ReportPages =
        [
            "/reports/ledger", "/reports/party-sales", "/reports/party-purchase",
            "/reports/payments", "/reports/expenses", "/reports/stock",
        ];

        private static readonly PageAddition[] PageAdditions =
        [
            new("MenuPage.PriceQuotation", "/quotations", "Price Quotation", "/sales", "sales.view",
            [
                View("sales.view", QuotationList, QuotationById, Customers),
                Create("sales.manage", "POST /create-quotation", ProductSearch, Customers),
                Update("sales.manage", "PUT /update-quotation/{id:int}", QuotationById, ProductSearch, Customers),
                Delete("sales.manage", "DELETE /delete-quotation/{id:int}"),
                Step("Print / Download", "print", "sales.view", QuotationById),
                Step("Change Status", "status", "sales.manage", "POST /change-quotation-status/{id:int}"),
                // Makes the sale, so it also needs what the New Sale form reads.
                Step("Convert to Invoice", "convert", "sales.manage", "POST /convert-quotation-to-sale/{id:int}",
                    QuotationById, ProductSearch, "GET /get-available-serials", Branches),
            ],
            ["view", "create", "update", "print", "status", "convert"]),
            // The Reports home page. It calls no API of its own (each report keeps its own
            // actions), so its View action only decides whether the page and its menu show.
            new("MenuPage.ReportCenter", "/reports", "Report Center", "/reports/ledger", "reports.view",
            [
                View("reports.view"),
            ],
            [], InsertBefore: true, ShareWithRolesOn: ReportPages),
            // Partial returns of a sale's lines. Whoever already works on Sales may view and make
            // returns; cancelling one is granted by an admin.
            new("MenuPage.SalesReturn", "/sales-returns", "Sales Return", "/sales", "sales.view",
            [
                View("sales.view", "GET /get-sales-returns", "GET /get-sales-return/{id:int}", Branches),
                Create("sales.manage", "POST /create-sales-return", "GET /get-sale-returnable/{saleId:int}",
                    "GET /get-all-sales", "GET /get-sale/{id:int}"),
                Step("Cancel Return", "cancel", "sales.manage", "POST /cancel-sales-return/{id:int}"),
            ],
            ["view", "create"], ShareWithRolesOn: ["/sales"]),
            // Goods sent back to a supplier. Purchase roles see the page; making or cancelling a
            // return is granted by an admin (the Salesman role gets nothing here).
            new("MenuPage.PurchaseReturn", "/purchase-returns", "Purchase Return", "/purchases", "purchases.view",
            [
                View("purchases.view", "GET /get-purchase-returns", "GET /get-purchase-return/{id:int}", Branches),
                Create("purchases.manage", "POST /create-purchase-return", "GET /get-purchase-returnable/{purchaseId:int}",
                    "GET /get-all-purchase-orders", "GET /get-purchase-order/{id:int}"),
                Step("Cancel Return", "cancel", "purchases.manage", "POST /cancel-purchase-return/{id:int}"),
            ],
            [], ShareWithRolesOn: ["/purchases"]),
        ];

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
                View("sales.view", "GET /get-all-sales", "GET /get-sale/{id:int}", "GET /get-invoice-customer-ledger",
                    SaleChallan, SaleEdits, Branches),
                Create("sales.manage", "POST /create-sale", ProductSearch, "GET /get-available-serials",
                    "GET /get-customer-by-phone", Customers, Branches),
                // Money and stock are committed when a sale is made; only its remark can change.
                // Goods coming back go through Sales Return (/sales-returns).
                Step("Update Remarks", "update", "sales.manage", "PUT /update-sale-remarks/{id:int}"),
                // Changing a completed invoice (lines, prices, discount, tax): stock, payments and
                // cross-ledger settlement are adjusted with it. Admin only until an admin grants it.
                Step("Edit Invoice", "edit", "admin", EditSale, ProductSearch, "GET /get-available-serials"),
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
                Update("warranty.manage", "PUT /update-warranty-claim/{id:int}"),
                Delete("warranty.manage", "DELETE /delete-warranty-claim/{id:int}"),
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
                Update("transfers.manage", "PUT /update-stock-transfer/{id:int}", ProductSearch, Branches,
                    "GET /get-stock-by-variant-and-branch", "GET /get-branch-stock/{branchId:int}/variant/{productVariantId:int}"),
                Delete("transfers.manage", "DELETE /delete-stock-transfer/{id:int}"),
            ],
            ["/reports"] =
            [
                View("reports.view"),
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

        // Applies PageAdditions (see there). Runs after SeedAsync.
        public static async Task AddPagesAsync(
            AppDbContext db,
            IReadOnlySet<string> knownRoutes,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            const int active = (int)EntityStatus.Active;

            foreach (var page in PageAdditions)
            {
                if (await db.SeedHistory.AnyAsync(s => s.Name == page.SeedName, cancellationToken)) continue;

                var menu = await db.AppMenus
                    .Include(m => m.Actions)
                    .FirstOrDefaultAsync(m => m.IsActive == active && m.FrontendRoute == page.Route, cancellationToken);

                if (menu == null)
                {
                    // Put it right after its sibling (e.g. under Sales, after "Sales").
                    var sibling = await db.AppMenus
                        .FirstOrDefaultAsync(m => m.IsActive == active && m.FrontendRoute == page.SiblingRoute, cancellationToken);
                    if (sibling == null)
                    {
                        logger.LogWarning("Menu page seed {SeedName}: sibling page {Sibling} not found, menu not added",
                            page.SeedName, page.SiblingRoute);
                        continue;
                    }

                    var order = page.InsertBefore ? sibling.MenuOrder : sibling.MenuOrder + 1;
                    var later = await db.AppMenus
                        .Where(m => m.SubmoduleId == sibling.SubmoduleId && m.ParentId == sibling.ParentId
                                    && m.MenuOrder >= order)
                        .ToListAsync(cancellationToken);
                    foreach (var m in later) m.MenuOrder++;

                    menu = new AppMenu
                    {
                        Name = page.Name,
                        FrontendRoute = page.Route,
                        Permission = page.Permission,
                        IsLeaf = true,
                        MenuOrder = order,
                        SubmoduleId = sibling.SubmoduleId,
                        ParentId = sibling.ParentId,
                        CreatedAt = DateTime.UtcNow,
                    };
                    db.AppMenus.Add(menu);
                }

                var created = new List<AppMenuAction>();
                if (!menu.Actions.Any(a => a.IsActive == active))
                {
                    foreach (var def in page.Actions)
                    {
                        var action = new AppMenuAction
                        {
                            Menu = menu,
                            Name = def.Name,
                            ComponentCode = def.Code,
                            ApiRoutes = def.Routes.Where(knownRoutes.Contains).Distinct().ToList(),
                            CreatedAt = DateTime.UtcNow,
                        };
                        db.AppMenuActions.Add(action);
                        created.Add(action);
                    }
                }

                var salesman = await db.Roles.FirstOrDefaultAsync(r => r.Name == SalesmanRoleName, cancellationToken);
                if (salesman != null)
                {
                    foreach (var action in created.Where(a => page.SalesmanCodes.Contains(a.ComponentCode)))
                        db.RoleMenuActions.Add(new RoleMenuAction { RoleId = salesman.Id, MenuAction = action });
                }

                if (page.ShareWithRolesOn is { Length: > 0 } sharedPages)
                {
                    // On a new database the actions came from Defaults, so look at the menu's own.
                    var pageActions = created.Count > 0
                        ? created
                        : menu.Actions.Where(a => a.IsActive == active).ToList();
                    var toGrant = pageActions.Where(a => page.SalesmanCodes.Length == 0
                                                         ? a.ComponentCode == "view"
                                                         : page.SalesmanCodes.Contains(a.ComponentCode))
                                             .ToList();

                    var roleIds = await db.RoleMenuActions
                        .Where(g => g.IsActive == active
                                    && g.MenuAction.IsActive == active
                                    && g.MenuAction.Menu.IsActive == active
                                    && sharedPages.Contains(g.MenuAction.Menu.FrontendRoute))
                        .Select(g => g.RoleId)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                    foreach (var action in toGrant)
                    {
                        var already = action.Id == 0
                            ? []
                            : await db.RoleMenuActions
                                .Where(g => g.MenuActionId == action.Id)
                                .Select(g => g.RoleId)
                                .ToListAsync(cancellationToken);
                        // Grants added above in this same run (the Salesman ones) are not saved yet;
                        // adding them again would break the unique (RoleId, MenuActionId) index.
                        var pending = db.RoleMenuActions.Local
                            .Where(g => g.MenuAction == action)
                            .Select(g => g.RoleId);
                        foreach (var roleId in roleIds.Except(already).Except(pending).ToList())
                            db.RoleMenuActions.Add(new RoleMenuAction { RoleId = roleId, MenuAction = action });
                    }
                }

                db.SeedHistory.Add(new SeedHistory { Name = page.SeedName, CreatedAt = DateTime.UtcNow });
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Menu page seed {SeedName}: {Count} action(s) added on {Route}",
                    page.SeedName, created.Count, page.Route);
            }
        }

        // Applies ActionAdditions (see there). Runs after SeedAsync.
        public static async Task AddActionsAsync(
            AppDbContext db,
            IReadOnlySet<string> knownRoutes,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            const int active = (int)EntityStatus.Active;

            foreach (var (seedName, menuRoute, name, code, apiRoutes) in ActionAdditions)
            {
                if (await db.SeedHistory.AnyAsync(s => s.Name == seedName, cancellationToken)) continue;

                var menu = await db.AppMenus
                    .Include(m => m.Actions)
                    .FirstOrDefaultAsync(m => m.IsActive == active && m.FrontendRoute == menuRoute, cancellationToken);
                if (menu == null)
                {
                    logger.LogWarning("Menu action seed {SeedName}: page {Route} not found, action not added", seedName, menuRoute);
                    continue;
                }

                // A new database already got it from Defaults; only an existing page needs it created.
                if (!menu.Actions.Any(a => a.IsActive == active && a.ComponentCode == code))
                {
                    db.AppMenuActions.Add(new AppMenuAction
                    {
                        Menu = menu,
                        Name = name,
                        ComponentCode = code,
                        ApiRoutes = apiRoutes.Where(knownRoutes.Contains).Distinct().ToList(),
                        CreatedAt = DateTime.UtcNow,
                    });
                }

                db.SeedHistory.Add(new SeedHistory { Name = seedName, CreatedAt = DateTime.UtcNow });
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Menu action seed {SeedName}: action {Code} ensured on {Route}", seedName, code, menuRoute);
            }
        }

        // Applies ActionRemovals (see there). Runs after SeedAsync.
        public static async Task RemoveActionsAsync(
            AppDbContext db,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            const int active = (int)EntityStatus.Active;

            foreach (var (seedName, menuRoute, actionCode) in ActionRemovals)
            {
                if (await db.SeedHistory.AnyAsync(s => s.Name == seedName, cancellationToken)) continue;

                var actions = await db.AppMenuActions
                    .Where(a => a.IsActive == active
                                && a.ComponentCode == actionCode
                                && a.Menu.FrontendRoute == menuRoute)
                    .ToListAsync(cancellationToken);
                foreach (var action in actions)
                {
                    action.Delete();
                    action.UpDatedAt = DateTime.UtcNow;
                }

                db.SeedHistory.Add(new SeedHistory { Name = seedName, CreatedAt = DateTime.UtcNow });
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Menu action seed {SeedName}: {Count} action(s) removed from {MenuRoute}",
                    seedName, actions.Count, menuRoute);
            }
        }

        // Applies RouteAdditions: appends each batch's API routes to the existing action of its
        // page. Runs after SeedAsync, so on a new database the routes are already present (from
        // Defaults) and the batch only records itself.
        public static async Task AddRoutesAsync(
            AppDbContext db,
            IReadOnlySet<string> knownRoutes,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            const int active = (int)EntityStatus.Active;

            foreach (var (seedName, menuRoute, actionCode, apiRoutes) in RouteAdditions)
            {
                if (await db.SeedHistory.AnyAsync(s => s.Name == seedName, cancellationToken)) continue;

                var actions = await db.AppMenuActions
                    .Where(a => a.IsActive == active
                                && a.ComponentCode == actionCode
                                && a.Menu.IsActive == active
                                && a.Menu.FrontendRoute == menuRoute)
                    .ToListAsync(cancellationToken);

                var routes = apiRoutes.Where(knownRoutes.Contains).ToList();
                foreach (var action in actions)
                {
                    var missing = routes.Where(r => !action.ApiRoutes.Contains(r)).ToList();
                    if (missing.Count == 0) continue;
                    // A new list so EF sees the text[] column change.
                    action.ApiRoutes = [.. action.ApiRoutes, .. missing];
                    action.UpDatedAt = DateTime.UtcNow;
                }

                db.SeedHistory.Add(new SeedHistory { Name = seedName, CreatedAt = DateTime.UtcNow });
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Menu action seed {SeedName}: routes added to {Count} action(s) on {MenuRoute}",
                    seedName, actions.Count, menuRoute);
            }
        }
    }
}
