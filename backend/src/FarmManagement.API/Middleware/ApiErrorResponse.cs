using System.Text.Json.Serialization;

namespace FarmManagement.API.Middleware;

/// <summary>
/// The response envelope returned for API errors.
/// </summary>
public sealed class ApiErrorResponse
{
    public ApiErrorResponse(
        int statusCode,
        string message,
        string traceId,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        StatusCode = statusCode;
        Message = message;
        TraceId = traceId;
        Errors = errors;
    }

    public bool Success => false;

    public int StatusCode { get; }

    public string Message { get; }

    public string TraceId { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string[]>? Errors { get; }
}
