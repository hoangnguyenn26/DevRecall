namespace DevRecall.Domain.Identity;

public static class LearningPreferenceDefaults
{
    public const int MinimumDailyCommitmentMinutes = 10;
    public const int MaximumDailyCommitmentMinutes = 180;
    public const int MinimumWeeklyTargetDays = 1;
    public const int MaximumWeeklyTargetDays = 7;
    public const int MaximumFocusAreas = 4;
    public const int DailyCommitmentMinutes = 30;
    public const int WeeklyTargetDays = 5;
    public const LearningGoal Goal = LearningGoal.BuildConsistentStudyHabit;
}
