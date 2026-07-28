using DevRecall.Domain.Common.Errors;

namespace DevRecall.Domain.Study;

public sealed class StudySessionDomainException(DomainError error)
    : InvalidOperationException(error.Message)
{
    public DomainError Error { get; } = error;
}
