namespace DevRecall.Application.Common.Exceptions;

public sealed class ValidationException : AppException
{
    public ValidationException(
        IReadOnlyDictionary<string, string[]> errors)
        : base(
            "VALIDATION_FAILED",
            "One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
