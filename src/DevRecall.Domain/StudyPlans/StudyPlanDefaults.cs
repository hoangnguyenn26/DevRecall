namespace DevRecall.Domain.StudyPlans;

public static class StudyPlanDefaults
{
    public const int MaximumTitleLength = 200;
    public const int MaximumItems = 20;
    public const int MinimumItemDurationMinutes = 5;
    public const int MaximumItemDurationMinutes = 180;
    public const int MaximumTotalDurationMinutes = 480;
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(7);
}
