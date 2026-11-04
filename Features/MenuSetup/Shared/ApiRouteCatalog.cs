namespace Inventory_Management_System.Features.MenuSetup.Shared
{
    // Every route the API serves, as "METHOD /route" (e.g. "GET /get-all-products"), read from
    // the running app so the list can never drift from the real endpoints.
    public static class ApiRouteCatalog
    {
        public static IReadOnlyList<string> List(EndpointDataSource endpoints) =>
            endpoints.Endpoints
                .OfType<RouteEndpoint>()
                .SelectMany(e =>
                {
                    var pattern = "/" + (e.RoutePattern.RawText ?? string.Empty).TrimStart('/');
                    var methods = e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? ["ANY"];
                    return methods.Select(method => $"{method} {pattern}");
                })
                .Where(r => !r.Contains("/swagger") && !r.Contains("/openapi"))
                // Login, logout, /me and the menu belong to every logged-in user; they cannot be
                // given to (or taken from) a role, so they are not offered.
                .Where(r => !MenuActionAccess.AlwaysAllowedRoutes.Contains(r))
                .Distinct()
                .OrderBy(r => r.Split(' ', 2)[1])
                .ThenBy(r => r)
                .ToList();
    }
}
