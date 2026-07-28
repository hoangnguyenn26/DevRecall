namespace DevRecall.Application.Dsa.Attempts.Compare;

public sealed record DsaAttemptComparisonDifference(
    int AttemptNumberDifference,
    int DurationDifferenceMinutes,
    string ResultTransition,
    bool ResultChanged,
    bool LanguageChanged,
    bool SolutionCodeChanged,
    bool ApproachChanged,
    bool TimeComplexityChanged,
    bool SpaceComplexityChanged,
    bool NotesChanged);
