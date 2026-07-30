namespace DevRecall.Application.Common.Exceptions;

public sealed class UnprocessableEntityException(
    string errorCode, string message) : AppException(errorCode, message);
