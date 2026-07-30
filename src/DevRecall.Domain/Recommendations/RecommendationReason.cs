using DevRecall.Domain.WeakTopics;

namespace DevRecall.Domain.Recommendations;

public sealed record RecommendationReason
{
    private RecommendationReason()
    {
    }

    private RecommendationReason(
        decimal weaknessScore, WeaknessLevel weaknessLevel, int signalCount,
        DateTimeOffset weaknessCalculatedAtUtc)
    {
        WeaknessScore = weaknessScore;
        WeaknessLevel = weaknessLevel;
        SignalCount = signalCount;
        WeaknessCalculatedAtUtc = weaknessCalculatedAtUtc;
    }

    public decimal WeaknessScore { get; private set; }
    public WeaknessLevel WeaknessLevel { get; private set; }
    public int SignalCount { get; private set; }
    public DateTimeOffset WeaknessCalculatedAtUtc { get; private set; }

    public static RecommendationReason Create(
        decimal weaknessScore, WeaknessLevel weaknessLevel, int signalCount,
        DateTimeOffset weaknessCalculatedAtUtc)
    {
        if (weaknessScore <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weaknessScore), "Weakness score must be greater than zero.");
        }

        if (weaknessLevel == WeaknessLevel.None || !Enum.IsDefined(weaknessLevel))
        {
            throw new ArgumentException(
                "Weakness level must represent an active weakness.",
                nameof(weaknessLevel));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(signalCount);

        EnsureUtc(weaknessCalculatedAtUtc, nameof(weaknessCalculatedAtUtc));
        return new(
            weaknessScore, weaknessLevel, signalCount, weaknessCalculatedAtUtc);
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                $"{parameterName} must be in UTC.", parameterName);
        }
    }
}
