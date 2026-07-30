namespace DevRecall.Domain.WeakTopics;

public static class WeakTopicScoringPolicy
{
    public static int GetWeight(WeaknessSignalType signalType) =>
        signalType switch
        {
            WeaknessSignalType.ReviewAgain => 4,
            WeaknessSignalType.ReviewHard => 2,
            WeaknessSignalType.ReviewGood => -2,
            WeaknessSignalType.ReviewEasy => -3,
            WeaknessSignalType.DsaFailed => 4,
            WeaknessSignalType.DsaPartiallySolved => 2,
            WeaknessSignalType.DsaSolved => -4,
            WeaknessSignalType.DsaSkipped => 3,
            WeaknessSignalType.StudyItemCompleted => -1,
            WeaknessSignalType.StudyItemSkipped => 2,
            _ => throw new ArgumentOutOfRangeException(
                nameof(signalType), signalType,
                "Unsupported weakness signal type.")
        };

    public static decimal GetRecencyMultiplier(
        DateTimeOffset occurredAtUtc, DateTimeOffset calculatedAtUtc)
    {
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        EnsureUtc(calculatedAtUtc, nameof(calculatedAtUtc));
        if (occurredAtUtc > calculatedAtUtc)
        {
            throw new ArgumentException(
                "Signal time cannot be later than calculation time.",
                nameof(occurredAtUtc));
        }

        var age = calculatedAtUtc - occurredAtUtc;
        if (age <= TimeSpan.FromDays(7))
        {
            return 1m;
        }

        if (age <= TimeSpan.FromDays(30))
        {
            return 0.60m;
        }

        return age <= TimeSpan.FromDays(90) ? 0.25m : 0m;
    }

    public static WeaknessLevel GetLevel(decimal score)
    {
        if (score <= 0m)
        {
            return WeaknessLevel.None;
        }

        if (score < 4m)
        {
            return WeaknessLevel.Low;
        }

        if (score < 8m)
        {
            return WeaknessLevel.Medium;
        }

        return score < 12m
            ? WeaknessLevel.High
            : WeaknessLevel.Critical;
    }

    public static WeaknessScoreBreakdown Calculate(
        IReadOnlyCollection<WeaknessSignal> signals,
        DateTimeOffset calculatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(signals);
        EnsureUtc(calculatedAtUtc, nameof(calculatedAtUtc));
        var contributions = signals.Select(signal =>
        {
            var weight = GetWeight(signal.Type);
            var multiplier = GetRecencyMultiplier(
                signal.OccurredAtUtc, calculatedAtUtc);
            return new WeaknessSignalContribution(
                signal.Type, weight, multiplier, weight * multiplier);
        }).Where(item => item.RecencyMultiplier > 0m).ToList();
        var rawScore = contributions.Sum(item => item.WeightedScore);
        var finalScore = Math.Round(
            Math.Max(0m, rawScore), 2, MidpointRounding.AwayFromZero);
        return new WeaknessScoreBreakdown(
            Math.Round(rawScore, 2, MidpointRounding.AwayFromZero),
            finalScore, GetLevel(finalScore), contributions.Count,
            calculatedAtUtc, contributions);
    }

    private static void EnsureUtc(
        DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                $"{parameterName} must be in UTC.", parameterName);
        }
    }
}
