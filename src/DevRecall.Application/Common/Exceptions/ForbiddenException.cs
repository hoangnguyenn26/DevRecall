namespace DevRecall.Application.Common.Exceptions;

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(
        string errorCode,
        string message)
        : base(errorCode, message)
    {
    }
}
