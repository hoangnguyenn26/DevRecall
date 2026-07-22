namespace DevRecall.Application.Common.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(
        string errorCode,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
