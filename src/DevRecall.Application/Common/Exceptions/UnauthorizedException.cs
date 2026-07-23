namespace DevRecall.Application.Common.Exceptions;

public sealed class UnauthorizedException(
    string errorCode,
    string message)
    : AppException(errorCode, message);
