namespace Inventory_Management_System.Shared.CurrentUser
{
    public static class RequirePermissionExtensions
    {
        // The user must be logged in, and is checked by MenuActionAuthorizationHandler: when the
        // route is on a menu action, the role's menu actions decide; otherwise `permission` is
        // the fallback rule (kept so routes nobody has configured yet behave as before).
        public static RouteHandlerBuilder RequirePermission(
            this RouteHandlerBuilder builder,
            string permission)
        {
            builder.RequireAuthorization(MenuActionRequirement.Policy(permission));
            return builder;
        }
    }
}
