using DevRecall.Application.Common.Exceptions;
using DevRecall.Domain.Dsa.Attempts;

namespace DevRecall.Application.Dsa.Attempts;

internal static class DsaAttemptResultParser
{
    public static DsaAttemptResult Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Error("Attempt result is required.");
        }

        var normalized = value.Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
        var parsed = Enum.TryParse<DsaAttemptResult>(
            normalized, ignoreCase: true, out var result);
        if (!parsed || !Enum.IsDefined(result))
        {
            throw Error(
                "Result must be Solved, PartiallySolved, Failed, or Skipped.");
        }

        return result;
    }

    private static ValidationException Error(string message) =>
        new(new Dictionary<string, string[]>
        {
            ["result"] = [message]
        });
}
