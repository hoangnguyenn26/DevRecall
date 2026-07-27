using DevRecall.Application.Common.Exceptions;
using DevRecall.Domain.Dsa;

namespace DevRecall.Application.Dsa;

internal static class DsaProblemDifficultyParser
{
    public static DsaProblemDifficulty Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Error("Difficulty is required.");
        }

        var parsed = Enum.TryParse<DsaProblemDifficulty>(
            value.Trim(), ignoreCase: true, out var difficulty);
        if (!parsed || !Enum.IsDefined(difficulty))
        {
            throw Error("Difficulty must be Easy, Medium, or Hard.");
        }

        return difficulty;
    }

    public static DsaProblemDifficulty? ParseOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Parse(value);

    private static ValidationException Error(string message) =>
        new(new Dictionary<string, string[]>
        {
            ["difficulty"] = [message]
        });
}
