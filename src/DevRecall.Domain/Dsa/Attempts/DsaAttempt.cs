namespace DevRecall.Domain.Dsa.Attempts;

public sealed class DsaAttempt
{
    private DsaAttempt()
    {
    }

    private DsaAttempt(
        Guid id, Guid dsaProblemId, int attemptNumber,
        DsaAttemptResult result, string? language, string? solutionCode,
        string? approach, string? timeComplexity, string? spaceComplexity,
        int durationMinutes, string? notes, DateTimeOffset attemptedAtUtc,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        DsaProblemId = dsaProblemId;
        AttemptNumber = attemptNumber;
        Result = result;
        Language = language;
        SolutionCode = solutionCode;
        Approach = approach;
        TimeComplexity = timeComplexity;
        SpaceComplexity = spaceComplexity;
        DurationMinutes = durationMinutes;
        Notes = notes;
        AttemptedAtUtc = attemptedAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid DsaProblemId { get; private set; }
    public int AttemptNumber { get; private set; }
    public DsaAttemptResult Result { get; private set; }
    public string? Language { get; private set; }
    public string? SolutionCode { get; private set; }
    public string? Approach { get; private set; }
    public string? TimeComplexity { get; private set; }
    public string? SpaceComplexity { get; private set; }
    public int DurationMinutes { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset AttemptedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static DsaAttempt Create(
        Guid id, Guid dsaProblemId, int attemptNumber,
        DsaAttemptResult result, string? language, string? solutionCode,
        string? approach, string? timeComplexity, string? spaceComplexity,
        int durationMinutes, string? notes, DateTimeOffset attemptedAtUtc,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "DSA attempt id cannot be empty.", nameof(id));
        }

        if (dsaProblemId == Guid.Empty)
        {
            throw new ArgumentException(
                "DSA problem id cannot be empty.", nameof(dsaProblemId));
        }

        if (attemptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attemptNumber),
                DsaAttemptErrors.InvalidAttemptNumber.Message);
        }

        EnsureValidResult(result);
        EnsureValidDuration(durationMinutes);
        EnsureUtc(attemptedAtUtc, nameof(attemptedAtUtc));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        return new DsaAttempt(
            id, dsaProblemId, attemptNumber, result,
            DsaAttemptText.NormalizeOptionalSingleLine(
                language, nameof(language), DsaAttemptText.LanguageMaxLength),
            DsaAttemptText.NormalizeOptionalMultiline(
                solutionCode, nameof(solutionCode),
                DsaAttemptText.SolutionCodeMaxLength),
            DsaAttemptText.NormalizeOptionalMultiline(
                approach, nameof(approach), DsaAttemptText.ApproachMaxLength),
            DsaAttemptText.NormalizeOptionalSingleLine(
                timeComplexity, nameof(timeComplexity),
                DsaAttemptText.ComplexityMaxLength),
            DsaAttemptText.NormalizeOptionalSingleLine(
                spaceComplexity, nameof(spaceComplexity),
                DsaAttemptText.ComplexityMaxLength),
            durationMinutes,
            DsaAttemptText.NormalizeOptionalMultiline(
                notes, nameof(notes), DsaAttemptText.NotesMaxLength),
            attemptedAtUtc, createdAtUtc);
    }

    private static void EnsureValidResult(DsaAttemptResult result)
    {
        if (!Enum.IsDefined(result))
        {
            throw new ArgumentOutOfRangeException(
                nameof(result), DsaAttemptErrors.InvalidResult.Message);
        }
    }

    private static void EnsureValidDuration(int durationMinutes)
    {
        if (durationMinutes < 0
            || durationMinutes > DsaAttemptText.MaximumDurationMinutes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationMinutes),
                DsaAttemptErrors.InvalidDuration.Message);
        }
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
