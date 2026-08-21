namespace Inventory_Management_System.Shared.Extensions.CorsExtension
{
    public static class CorsExtension
    {
        public static IServiceCollection AddCorsExtension(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>();

            // Without this check WithOrigins(null) throws an unclear ArgumentNullException at startup,
            // which is what happens when the environment has no appsettings.<Environment>.json.
            if (allowedOrigins is null || allowedOrigins.Length == 0)
                throw new InvalidOperationException(
                    "AllowedOrigins is missing or empty. Set it in appsettings.<Environment>.json " +
                    "or via the AllowedOrigins__0 environment variable.");

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder.WithOrigins(allowedOrigins)
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                });
            });
            return services;
        }
    }
}
