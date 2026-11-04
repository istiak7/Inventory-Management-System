using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Inventory_Management_System.Shared.Extensions.DependencyExtensions
{
    public static class JwtAuthentication
    {
        public static void AddJWTAuthentication(this WebApplicationBuilder builder)
        {
            var jwtSettings = new JwtSettings();

            builder.Configuration.Bind("JwtSettings", jwtSettings);
            builder.Services.AddSingleton(jwtSettings);

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.RequireHttpsMetadata = false;
                        options.SaveToken = true;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            // Only accept tokens this API made for itself.
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            // Default is 5 minutes of extra life after expiry; keep it short.
                            ClockSkew = TimeSpan.FromSeconds(30),
                            ValidIssuer = jwtSettings.Issuer,
                            ValidAudience = jwtSettings.Audience,
                            RequireExpirationTime = true,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                        };

                        // A signed token is not enough: the user must still be active and have the
                        // role and branch the token was made for. Otherwise 401, and the app renews
                        // the token (getting the current role) or, for a removed user, logs out.
                        options.Events = new JwtBearerEvents
                        {
                            OnTokenValidated = async context =>
                            {
                                var access = context.HttpContext.RequestServices
                                    .GetRequiredService<Inventory_Management_System.Shared.CurrentUser.MenuActionAccess>();
                                if (context.Principal is null
                                    || !await access.IsTokenCurrentAsync(context.Principal, context.HttpContext.RequestAborted))
                                {
                                    context.Fail("The user's role, branch or status has changed since this token was issued.");
                                }
                            },
                        };
                    });
        }
    }
}
