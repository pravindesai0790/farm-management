using System.Diagnostics;

namespace FarmManagement.API.Middleware;

internal static class TraceIdSupport
{
    public const string HeaderName = "X-Trace-Id";

    private const string ItemKey = "FarmManagement.TraceId";

    public static string GetOrCreate(HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var existingTraceId) &&
            existingTraceId is string traceId &&
            !string.IsNullOrWhiteSpace(traceId))
        {
            SetResponseHeader(context, traceId);
            return traceId;
        }

        var activityTraceId = Activity.Current?.TraceId.ToString();
        traceId = !string.IsNullOrWhiteSpace(activityTraceId) &&
                  !activityTraceId.All(static character => character == '0')
            ? activityTraceId
            : context.TraceIdentifier;

        if (string.IsNullOrWhiteSpace(traceId))
        {
            traceId = Guid.NewGuid().ToString("N");
            context.TraceIdentifier = traceId;
        }

        context.Items[ItemKey] = traceId;
        SetResponseHeader(context, traceId);
        return traceId;
    }

    public static void SetResponseHeader(HttpContext context, string traceId)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.Headers[HeaderName] = traceId;
        }
    }
}
