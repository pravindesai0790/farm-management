namespace FarmManagement.API.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = TraceIdSupport.GetOrCreate(context);

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            // Keep exception messages and stack traces out of logs because they may
            // contain credentials or provider-specific connection details.
            logger.LogError(
                "Unhandled {ExceptionType} while processing {Method} {Path}. TraceId: {TraceId}",
                exception.GetType().Name,
                context.Request.Method,
                context.Request.Path,
                traceId);

            if (context.Response.HasStarted)
            {
                throw;
            }

            var errorResponse = ApiErrorResponseFactory.FromException(exception, traceId);
            context.Response.Clear();
            context.Response.StatusCode = errorResponse.StatusCode;
            context.Response.ContentType = "application/json";
            TraceIdSupport.SetResponseHeader(context, traceId);

            await context.Response.WriteAsJsonAsync(
                errorResponse,
                context.RequestAborted);
        }
    }
}
