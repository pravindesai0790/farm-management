using System.Diagnostics;
using System.Text.Json;

namespace FarmManagement.API.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                traceId);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = "An unexpected error occurred",
                traceId
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
