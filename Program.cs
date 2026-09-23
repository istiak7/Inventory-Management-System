using FluentValidation;
using Inventory_Management_System.Database;
using Inventory_Management_System.Middleware;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.CorsExtension;
using Inventory_Management_System.Shared.Extensions.DependencyExtensions;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddTransient<ExceptionHandlingMiddleware>();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Swagger services
builder.Services.AddSwaggerGen();


//Database Registration
var dbConnectionString = builder.Configuration.GetConnectionString("DbConnectionString");

// Fail at startup instead of on the first request, where a missing connection string
// surfaces as a confusing Npgsql "host is required" error deep inside a handler.
if (string.IsNullOrWhiteSpace(dbConnectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:DbConnectionString is missing. Set it in appsettings.<Environment>.json " +
        "or via the ConnectionStrings__DbConnectionString environment variable.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dbConnectionString));

//MediatR Registration
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

//FluentValidation Registration
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

//Validation Behavior PipelineRegistration
builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(ValidationBehavior<,>));

// Staff may only write records of their own branch (see IBranchScopedRequest).
builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(Inventory_Management_System.Shared.CurrentUser.BranchAccessBehavior<,>));

// Days in reports, the dashboard and date filters follow the shop's time zone.
BusinessClock.Configure(builder.Configuration["BusinessTimeZone"] ?? "Asia/Dhaka");

//JWT Settings Registration
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

// Fail fast on a missing/short signing key instead of printing it: the previous debug line
// wrote the raw secret to stdout, so it landed in every console and log sink.
if (string.IsNullOrWhiteSpace(jwtSettings?.SecretKey) || jwtSettings.SecretKey.Length < 32)
    throw new InvalidOperationException(
        "JwtSettings:SecretKey is missing or shorter than 32 characters. Set it via user-secrets locally, " +
        "or via the JwtSettings__SecretKey environment variable on the server.");

builder.AddJWTAuthentication();

// Authorization: management endpoints (users, roles) are for admins only.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddServices();
builder.Services.AddRepositories();
builder.Services.AddCorsExtension(builder.Configuration);

// Behind nginx every request comes from the nginx container. These headers carry the real
// client IP, so the login limit below counts per person, not for everyone together.
// The backend is not reachable from outside (only through nginx), so the headers can be trusted.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Slows down password guessing: at most 10 login / refresh attempts per minute per IP address.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(Inventory_Management_System.Features.Users.Login.LoginEndpoint.AuthRateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(new Result
        {
            IsSuccess = false,
            StatusCode = StatusCodes.Status429TooManyRequests,
            Status = "Error",
            Message = "Too many attempts. Please wait a minute and try again."
        }, cancellationToken);
    };
});

var app = builder.Build();
app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
// Swagger lists every endpoint, so it is only shown on developer machines, never on the server.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Enable Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API");
    });
}


app.UseCors("CorsPolicy");

// Map endpoints dynamically
var endpoints = Assembly.GetExecutingAssembly().GetTypes()
    .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

foreach (var endpoint in endpoints)
{
    var instance = Activator.CreateInstance(endpoint) as IEndpoint;
    instance?.MapEndpoint(app);
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

// Apply migrations and seed permissions, roles and the first admin user.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db, app.Configuration, app.Environment);
}

app.Run();
