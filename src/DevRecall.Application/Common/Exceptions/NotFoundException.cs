namespace DevRecall.Application.Common.Exceptions;

public sealed class NotFoundException : AppException
{
    public NotFoundException(
        string errorCode,
        string message)
        : base(errorCode, message)
    {
    }
}
