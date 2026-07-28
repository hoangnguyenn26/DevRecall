using DevRecall.Domain.Dsa.Attempts;
using FluentAssertions;

namespace DevRecall.Domain.Tests.Dsa.Attempts;

public sealed class DsaAttemptTests
{
    [Fact]
    public void Create_ShouldPreserveMultilineFormatting()
    {
        var code =
            """
            public int[] TwoSum(int[] nums, int target)
            {
                return [];
            }
            """;
        var approach = "First line.\n\nSecond line.";

        var attempt = CreateAttempt(
            solutionCode: code, approach: approach);

        attempt.SolutionCode.Should().Be(code.Trim());
        attempt.Approach.Should().Be(approach);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidAttemptNumber_ShouldThrow(int attemptNumber)
    {
        var action = () => CreateAttempt(attemptNumber: attemptNumber);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage(
                $"*{DsaAttemptErrors.InvalidAttemptNumber.Message}*");
    }

    [Fact]
    public void Create_WithInvalidResult_ShouldThrow()
    {
        var action = () => CreateAttempt(result: (DsaAttemptResult)99);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage($"*{DsaAttemptErrors.InvalidResult.Message}*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(DsaAttemptText.MaximumDurationMinutes + 1)]
    public void Create_WithInvalidDuration_ShouldThrow(int durationMinutes)
    {
        var action = () => CreateAttempt(durationMinutes: durationMinutes);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage($"*{DsaAttemptErrors.InvalidDuration.Message}*");
    }

    [Fact]
    public void Create_WithWhitespaceOptionalValues_ShouldNormalizeToNull()
    {
        var attempt = CreateAttempt(
            language: " ", solutionCode: " ", approach: " ",
            timeComplexity: " ", spaceComplexity: " ", notes: " ");

        attempt.Language.Should().BeNull();
        attempt.SolutionCode.Should().BeNull();
        attempt.Approach.Should().BeNull();
        attempt.TimeComplexity.Should().BeNull();
        attempt.SpaceComplexity.Should().BeNull();
        attempt.Notes.Should().BeNull();
    }

    [Fact]
    public void Create_WithNonUtcTimestamp_ShouldThrow()
    {
        var nonUtc = DateTimeOffset.Now;
        if (nonUtc.Offset == TimeSpan.Zero)
        {
            nonUtc = nonUtc.ToOffset(TimeSpan.FromHours(7));
        }

        var action = () => CreateAttempt(attemptedAtUtc: nonUtc);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*attemptedAtUtc must be in UTC.*");
    }

    private static DsaAttempt CreateAttempt(
        int attemptNumber = 1,
        DsaAttemptResult result = DsaAttemptResult.Failed,
        string? language = "C#",
        string? solutionCode = null,
        string? approach = null,
        string? timeComplexity = null,
        string? spaceComplexity = null,
        int durationMinutes = 30,
        string? notes = null,
        DateTimeOffset? attemptedAtUtc = null) =>
        DsaAttempt.Create(
            Guid.NewGuid(), Guid.NewGuid(), attemptNumber, result, language,
            solutionCode, approach, timeComplexity, spaceComplexity,
            durationMinutes, notes, attemptedAtUtc ?? DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
}
