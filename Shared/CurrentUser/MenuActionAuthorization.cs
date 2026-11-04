using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Inventory_Management_System.Shared.CurrentUser
{
    // The one authorization check every protected endpoint goes through.
    //
    //   1. Admin role            -> allowed (built-in full access).
    //   2. Route is on a menu action (System Setup -> Menu Action)
    //                            -> allowed only if the user's role has one of those actions.
    //   3. Route is on no action -> the endpoint's own fixed rule decides, as before:
    //                               AdminOnly = admins only, RequirePermission(x) = permission
    //                               claim x, plain RequireAuthorization() = any logged-in user.
    public sealed class MenuActionRequirement(string? fallbackPermission, bool adminOnly) : IAuthorizationRequirement
    {
        public string? FallbackPermission { get; } = fallbackPermission;
        public bool AdminOnly { get; } = adminOnly;

        public static AuthorizationPolicy Policy(string? fallbackPermission = null, bool adminOnly = false) =>
            new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new MenuActionRequirement(fallbackPermission, adminOnly))
                .Build();
    }

    public sealed class MenuActionAuthorizationHandler(MenuActionAccess _access)
        : AuthorizationHandler<MenuActionRequirement>
    {
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            MenuActionRequirement requirement)
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated != true) return; // answered with 401

            if (user.IsInRole(MenuActionAccess.AdminRoleName))
            {
                context.Succeed(requirement);
                return;
            }

            var httpContext = context.Resource as HttpContext;
            var routes = MenuActionAccess.RouteKeys(httpContext?.GetEndpoint()).ToList();

            if (routes.Any(MenuActionAccess.AlwaysAllowedRoutes.Contains))
            {
                context.Succeed(requirement);
                return;
            }

            var snapshot = await _access.GetAsync(httpContext?.RequestAborted ?? default);
            var managedBy = routes
                .Where(snapshot.ActionsByRoute.ContainsKey)
                .SelectMany(r => snapshot.ActionsByRoute[r])
                .ToHashSet();

            if (managedBy.Count > 0)
            {
                var roleId = int.TryParse(user.FindFirst("roleId")?.Value, out var id) ? id : (int?)null;
                if (snapshot.ActionsOf(roleId).Overlaps(managedBy))
                    context.Succeed(requirement);
                return;
            }

            // Not on any menu action: the endpoint's own fixed rule (backward compatible).
            if (requirement.AdminOnly) return;
            if (requirement.FallbackPermission is null
                || user.Claims.Any(c => c.Type == "permission" && c.Value == requirement.FallbackPermission))
            {
                context.Succeed(requirement);
            }
        }
    }

    // Answers a refused request with the usual Result envelope instead of an empty body, so the
    // frontend can show the message. 401 = not logged in (or token expired), 403 = no permission.
    public sealed class ResultAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _default = new();

        public async Task HandleAsync(
            RequestDelegate next,
            HttpContext context,
            AuthorizationPolicy policy,
            PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Forbidden)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new Result
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Status = "Forbidden",
                    Message = "You do not have permission to perform this action."
                });
                return;
            }

            await _default.HandleAsync(next, context, policy, authorizeResult);
        }
    }

    public static class MenuActionAuthorizationSetup
    {
        // Registers the check and the policies the endpoints already use.
        public static void AddMenuActionAuthorization(this IServiceCollection services)
        {
            services.AddSingleton<MenuActionAccess>();
            services.AddSingleton<IAuthorizationHandler, MenuActionAuthorizationHandler>();
            services.AddSingleton<IAuthorizationMiddlewareResultHandler, ResultAuthorizationResultHandler>();

            services.AddAuthorization(options =>
            {
                // RequireAuthorization() with no policy name.
                options.DefaultPolicy = MenuActionRequirement.Policy();
                // Management endpoints: admins only unless a menu action grants the route.
                options.AddPolicy("AdminOnly", MenuActionRequirement.Policy(adminOnly: true));
            });
        }

        public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(MenuActionAccess.AdminRoleName);
    }
}
