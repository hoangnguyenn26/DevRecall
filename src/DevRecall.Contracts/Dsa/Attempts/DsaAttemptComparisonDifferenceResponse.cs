namespace DevRecall.Contracts.Dsa.Attempts;

public sealed record DsaAttemptComparisonDifferenceResponse(
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
