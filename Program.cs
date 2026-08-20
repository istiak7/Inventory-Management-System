using FluentValidation;
using Inventory_Management_System.Database;
using Inventory_Management_System.Middleware;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Background;
using Inventory_Management_System.Shared.Extensions.CorsExtension;
using Inventory_Management_System.Shared.Extensions.DependencyExtensions;
using Inventory_Management_System.Shared.RMQ;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Quartz;
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
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DbConnectionString")));

//RabbitMQ Registration
builder.Services.Configure<RMQSettings>(builder.Configuration.GetSection("RMQSettings"));
builder.Services.AddSingleton<IRabbitMQConnection, RabbitMQConnection>();
builder.Services.AddSingleton<IMassageProducer, MessageProducer>();
//Quartz Registration
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("OutboxProcessorJob");
    q.AddJob<OutboxProcessorJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("OutboxProcessorJob-trigger")
        .WithCronSchedule("0 0/30 * * * ?")); // Run every 30 minutes
}
);
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);


//MediatR Registration
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

//FluentValidation Registration
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

//Validation Behavior PipelineRegistration
builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(ValidationBehavior<,>));

//JWT Settings Registration
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

// Fail fast on a missing/short signing key instead of printing it: the previous debug line
// wrote the raw secret to stdout, so it landed in every console and log sink.
if (string.IsNullOrWhiteSpace(jwtSettings?.SecretKey) || jwtSettings.SecretKey.Length < 32)
    throw new InvalidOperationException(
        "JwtSettings:SecretKey is missing or shorter than 32 characters. Configure it via user-secrets or an environment variable.");

builder.AddJWTAuthentication();
builder.Services.AddServices();
builder.Services.AddRepositories();
builder.Services.AddCorsExtension(builder.Configuration);

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
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

app.UseAuthorization();

app.MapControllers();

app.Run();
