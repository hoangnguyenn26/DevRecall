namespace DevRecall.Contracts.Dsa.Attempts;

public sealed class GetDsaAttemptsRequest
{
    public string? Result { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}
