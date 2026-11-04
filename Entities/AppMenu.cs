namespace Inventory_Management_System.Entities
{
    // The app's navigation, kept in the database so an admin can change it from System Setup
    // (like the accounting portal): Module -> Submodule -> Menu tree -> Menu Actions.
    // Named App* because "Module" clashes with System.Reflection.Module.

    // A whole area of the app with its own menus, e.g. "Inventory", "Admin Panel".
    public class AppModule : BaseEntity
    {
        public required string Name { get; set; }
        // Lucide icon name for the launcher tile, e.g. "Boxes". Empty = a default icon.
        public string Icon { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        public ICollection<AppSubmodule> Submodules { get; set; } = [];
    }

    // A part of a module, e.g. Admin Panel -> "Administration".
    public class AppSubmodule : BaseEntity
    {
        public int ModuleId { get; set; }
        public required string Name { get; set; }
        public int DisplayOrder { get; set; }

        public AppModule Module { get; set; } = null!;
        public ICollection<AppMenu> Menus { get; set; } = [];
    }

    // One menu entry. A folder (IsLeaf = false) holds other menus; a page (IsLeaf = true)
    // opens FrontendRoute. Menus form a tree through ParentId.
    public class AppMenu : BaseEntity
    {
        public int SubmoduleId { get; set; }
        public int? ParentId { get; set; }
        public required string Name { get; set; }
        // Route of the page, e.g. "/products". Empty for folders.
        public string FrontendRoute { get; set; } = string.Empty;
        public int MenuOrder { get; set; }
        public bool IsLeaf { get; set; }
        public string Icon { get; set; } = string.Empty;
        // Permission needed to see the menu, e.g. "products.view"; "admin" = admins only;
        // empty = every logged-in user.
        public string Permission { get; set; } = string.Empty;

        public AppSubmodule Submodule { get; set; } = null!;
        public AppMenu? Parent { get; set; }
        public ICollection<AppMenu> Children { get; set; } = [];
        public ICollection<AppMenuAction> Actions { get; set; } = [];
    }

    // Something a user can do on a menu's page, e.g. "View", "Add", "Delete", with the API
    // routes that action uses.
    public class AppMenuAction : BaseEntity
    {
        public int MenuId { get; set; }
        public required string Name { get; set; }
        // Short code the frontend uses to show / hide a control, e.g. "0001".
        public required string ComponentCode { get; set; }
        // API routes this action calls, e.g. "GET /get-all-products".
        public List<string> ApiRoutes { get; set; } = [];

        public AppMenu Menu { get; set; } = null!;
    }
}
