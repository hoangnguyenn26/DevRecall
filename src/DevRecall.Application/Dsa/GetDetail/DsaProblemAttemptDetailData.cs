using DevRecall.Application.Dsa.Attempts;

namespace DevRecall.Application.Dsa.GetDetail;

public sealed record DsaProblemAttemptDetailData(
    DsaAttemptSummaryReadModel Summary,
    DsaAttemptOverviewReadItem? LatestAttempt,
    DsaAttemptOverviewReadItem? LatestSuccessfulAttempt,
    IReadOnlyList<DsaAttemptOverviewReadItem> RecentAttempts);
