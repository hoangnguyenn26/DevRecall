namespace DevRecall.Domain.Common.Errors;

public sealed record DomainError(
    string Code,
    string Message);
