namespace Inventory_Management_System.Shared.CurrentUser
{
    public static class RequirePermissionExtensions
    {
        public static RouteHandlerBuilder RequirePermission(
            this RouteHandlerBuilder builder,
            string permission)
        {
            builder.RequireAuthorization(); // a user must be jwt access token authenticated to reach this endpoint

            builder.AddEndpointFilter(async (context, next) =>
            {
                var user = context.HttpContext.User;

                var allowed = user.IsInRole("Admin")
                    || user.Claims.Any(c => c.Type == "permission" && c.Value == permission);

                if (!allowed)
                {
                    return Results.Json(
                        new Result
                        {
                            IsSuccess = false,
                            StatusCode = StatusCodes.Status403Forbidden,
                            Status = "Forbidden",
                            Message = $"You do not have permission to perform this action ({permission})."
                        },
                        statusCode: StatusCodes.Status403Forbidden);
                }

                return await next(context);
            });

            return builder;
        }
    }
}
