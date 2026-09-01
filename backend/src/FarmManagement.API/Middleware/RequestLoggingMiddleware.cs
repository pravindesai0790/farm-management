using System.Diagnostics;

namespace FarmManagement.API.Middleware;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = TraceIdSupport.GetOrCreate(context);
        var startedAt = Stopwatch.GetTimestamp();
        int? exceptionStatusCode = null;

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            exceptionStatusCode = ApiErrorResponseFactory.GetStatusCode(exception);
            throw;
        }
        finally
        {
            var duration = Stopwatch.GetElapsedTime(startedAt);
            logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {DurationMilliseconds} ms. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                exceptionStatusCode ?? context.Response.StatusCode,
                duration.TotalMilliseconds,
                traceId);
        }
    }
}
