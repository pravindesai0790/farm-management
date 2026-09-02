using FarmManagement.API.Middleware;
using FarmManagement.API.Configuration;
using FarmManagement.API.Extensions;
using FarmManagement.Application.Interfaces;
using FarmManagement.Application.Interfaces.Authentication;
using FarmManagement.Application.Interfaces.Farms;
using FarmManagement.Application.Interfaces.Crops;
using FarmManagement.Application.Interfaces.Plantations;
using FarmManagement.Application.Interfaces.CropCycles;
using FarmManagement.Application.Interfaces.Users;
using FarmManagement.Application.Interfaces.Roles;
using FarmManagement.Application.Interfaces.Organizations;
using FarmManagement.Application.Services;
using FarmManagement.Infrastructure;
using FarmManagement.Infrastructure.Authentication;
using FarmManagement.Infrastructure.Persistence;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is not null)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => error.Exception is not null || string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The value is invalid."
                        : error.ErrorMessage)
                    .ToArray());
        var traceId = TraceIdSupport.GetOrCreate(context.HttpContext);

        return new ObjectResult(new ApiErrorResponse(
            StatusCodes.Status400BadRequest,
            "Validation failed",
            traceId,
            errors))
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
    };
});
builder.Services.AddOpenApi();
builder.Services.AddOptions<RefreshTokenCookieOptions>()
    .Bind(builder.Configuration.GetSection(RefreshTokenCookieOptions.SectionName));
var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();
jwtOptions.Validate();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddPermissionAuthorization();
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? ["http://localhost:4200"];

    options.AddPolicy("Development", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString,
        name: "postgresql",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: ["ready"]);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ISystemService, SystemService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserAdministrationService, UserAdministrationService>();
builder.Services.AddScoped<IRoleAdministrationService, RoleAdministrationService>();
builder.Services.AddScoped<IFarmService, FarmService>();
builder.Services.AddScoped<IFarmAreaService, FarmAreaService>();
builder.Services.AddScoped<ICropService, CropService>();
builder.Services.AddScoped<ICropLifecycleTemplateService, CropLifecycleTemplateService>();
builder.Services.AddScoped<IPlantationService, PlantationService>();
builder.Services.AddScoped<ICropCycleService, CropCycleService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();

var app = builder.Build();

app.Logger.LogInformation("Farm Management API is starting.");

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    var statusCode = httpContext.Response.StatusCode;

    if (!httpContext.Request.Path.StartsWithSegments("/api") || statusCode < 400)
    {
        return;
    }

    var traceId = TraceIdSupport.GetOrCreate(httpContext);
    httpContext.Response.ContentType = "application/json";
    await httpContext.Response.WriteAsJsonAsync(
        new ApiErrorResponse(
            statusCode,
            statusCode switch
            {
                StatusCodes.Status401Unauthorized => "Unauthorized",
                StatusCodes.Status403Forbidden => "Forbidden",
                StatusCodes.Status404NotFound => "Resource not found",
                _ => "The request could not be completed."
            },
            traceId),
        httpContext.RequestAborted);
});

if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    await app.Services.ApplyDatabaseMigrationsAsync(app.Logger);
}

app.Logger.LogInformation("Farm Management API started.");
await app.RunAsync();
