namespace DevRecall.Contracts.Dsa.Attempts;

public sealed class CompareDsaAttemptsRequest
{
    public Guid LeftAttemptId { get; init; }
    public Guid RightAttemptId { get; init; }
}
