namespace DevRecall.Application.Dsa.GetDetail;

public interface IDsaProblemAttemptDetailReader
{
    Task<DsaProblemAttemptDetailData> ReadAsync(
        Guid dsaProblemId, int recentAttemptCount,
        CancellationToken cancellationToken);
}
