namespace DevRecall.Application.Common.Exceptions;

public sealed class BadRequestException(string errorCode, string message)
    : AppException(errorCode, message);
