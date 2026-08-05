namespace DevRecall.Application.Common.Exceptions;

public sealed class ConcurrencyException(
    string errorCode, string message, Exception? innerException = null)
    : AppException(errorCode, message, innerException);
