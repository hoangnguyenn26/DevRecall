using DevRecall.Domain.Common.Errors;

namespace DevRecall.Domain.Dsa.Attempts;

public static class DsaAttemptErrors
{
    public static readonly DomainError NotFound = new(
        "DSA_ATTEMPT_NOT_FOUND",
        "The DSA attempt was not found.");

    public static readonly DomainError InvalidAttemptNumber = new(
        "DSA_ATTEMPT_INVALID_NUMBER",
        "The DSA attempt number must be greater than zero.");

    public static readonly DomainError InvalidResult = new(
        "DSA_ATTEMPT_INVALID_RESULT",
        "The DSA attempt result is invalid.");

    public static readonly DomainError InvalidDuration = new(
        "DSA_ATTEMPT_INVALID_DURATION",
        "The DSA attempt duration is invalid.");

    public static readonly DomainError VersionConflict = new(
        "DSA_ATTEMPT_NUMBER_CONFLICT",
        "The DSA attempt could not be created because the attempt history changed.");
}
