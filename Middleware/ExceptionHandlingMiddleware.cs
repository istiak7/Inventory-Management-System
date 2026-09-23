using FluentValidation;
using System.Text.Json;

namespace Inventory_Management_System.Middleware
{
    public class ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> _logger) : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (ValidationException ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                var errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
                var response = JsonSerializer.Serialize(new { Message = "Validation Failed", Errors = errors });

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(response);
            }
            catch (Exception ex)
            {
                // Logged with the request path so the cause can be found in "docker compose logs backend".
                // The client only gets a general message: no internal details leak out.
                _logger.LogError(ex, "Unhandled error on {Method} {Path}", context.Request.Method, context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("An unexpected error occurred.");
            }
        }
    }
}
