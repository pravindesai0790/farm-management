using FarmManagement.Application.Common.Exceptions;

namespace FarmManagement.API.Middleware;

internal static class ApiErrorResponseFactory
{
    public static ApiErrorResponse FromException(Exception exception, string traceId)
    {
        var (statusCode, message, errors) = MapException(exception);
        return new ApiErrorResponse(statusCode, message, traceId, errors);
    }

    public static int GetStatusCode(Exception exception) => MapException(exception).StatusCode;

    private static (int StatusCode, string Message, IReadOnlyDictionary<string, string[]>? Errors)
        MapException(Exception exception) => exception switch
        {
            ValidationException validationException =>
                (StatusCodes.Status400BadRequest, validationException.Message, validationException.Errors),
            UnauthorizedAccessException unauthorizedException =>
                (StatusCodes.Status401Unauthorized, unauthorizedException.Message, null),
            ForbiddenException forbiddenException =>
                (StatusCodes.Status403Forbidden, forbiddenException.Message, null),
            ResourceNotFoundException notFoundException =>
                (StatusCodes.Status404NotFound, notFoundException.Message, null),
            ConflictException conflictException =>
                (StatusCodes.Status409Conflict, conflictException.Message, null),
            _ =>
                (StatusCodes.Status500InternalServerError, "An unexpected error occurred", null)
        };
}
