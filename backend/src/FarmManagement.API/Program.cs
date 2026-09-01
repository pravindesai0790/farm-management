using FarmManagement.API.Middleware;
using FarmManagement.Application.Interfaces;
using FarmManagement.Application.Services;
using FarmManagement.Infrastructure;
using FarmManagement.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not configured.");
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? ["http://localhost:4200"];

    options.AddPolicy("Development", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString,
        name: "postgresql",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: ["ready"]);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ISystemService, SystemService>();

var app = builder.Build();

app.Logger.LogInformation("Farm Management API is starting.");

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
    app.MapOpenApi();
}

app.MapHealthChecks("/health");
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    await app.Services.ApplyDatabaseMigrationsAsync(app.Logger);
}

app.Logger.LogInformation("Farm Management API started.");
await app.RunAsync();
