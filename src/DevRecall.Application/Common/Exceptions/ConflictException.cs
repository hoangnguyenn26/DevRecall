namespace DevRecall.Application.Common.Exceptions;

public sealed class ConflictException : AppException
{
    public ConflictException(
        string errorCode,
        string message)
        : base(errorCode, message)
    {
    }
}
