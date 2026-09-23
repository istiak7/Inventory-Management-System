using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.Health
{
    // GET /health: 200 when the API is up and can reach the database, 503 when it cannot.
    // For Docker / monitoring. It shows no data, so it needs no login.
    public class HealthEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/health", async (AppDbContext db, CancellationToken cancellationToken) =>
            {
                var databaseOk = await db.Database.CanConnectAsync(cancellationToken);
                return databaseOk
                    ? Results.Ok(new { status = "Healthy" })
                    : Results.Json(new { status = "Unhealthy", reason = "Database not reachable" }, statusCode: 503);
            }).WithTags("Health").AllowAnonymous();
        }
    }
}
