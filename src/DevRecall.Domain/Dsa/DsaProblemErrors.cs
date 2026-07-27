using DevRecall.Domain.Common.Errors;

namespace DevRecall.Domain.Dsa;

public static class DsaProblemErrors
{
    public static readonly DomainError NotFound = new(
        "DSA_PROBLEM_NOT_FOUND",
        "The DSA problem was not found.");

    public static readonly DomainError Archived = new(
        "DSA_PROBLEM_ARCHIVED",
        "An archived DSA problem cannot be modified.");

    public static readonly DomainError InvalidDifficulty = new(
        "DSA_PROBLEM_INVALID_DIFFICULTY",
        "The DSA problem difficulty is invalid.");

    public static readonly DomainError InvalidExternalUrl = new(
        "DSA_PROBLEM_INVALID_EXTERNAL_URL",
        "The DSA problem external URL is invalid.");
}
