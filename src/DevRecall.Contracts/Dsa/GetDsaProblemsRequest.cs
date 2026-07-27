namespace DevRecall.Contracts.Dsa;

public sealed class GetDsaProblemsRequest
{
    public string? Difficulty { get; init; }
    public string? Topic { get; init; }
    public string? Source { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}
