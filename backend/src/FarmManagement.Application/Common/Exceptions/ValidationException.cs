namespace FarmManagement.Application.Common.Exceptions;

public sealed class ValidationException : Exception
{
    public ValidationException(
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]>? Errors { get; }
}
