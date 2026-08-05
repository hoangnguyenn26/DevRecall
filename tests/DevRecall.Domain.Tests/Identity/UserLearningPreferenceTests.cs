using DevRecall.Domain.Identity;
using FluentAssertions;

namespace DevRecall.Domain.Tests.Identity;

public sealed class UserLearningPreferenceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 5, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Complete_ShouldStorePreferencesAndStartAtVersionOne()
    {
        var preference = Create();

        preference.Goal.Should().Be(LearningGoal.PrepareForInterviews);
        preference.DailyCommitmentMinutes.Should().Be(45);
        preference.WeeklyTargetDays.Should().Be(5);
        preference.CompletionType.Should().Be(OnboardingCompletionType.Completed);
        preference.Version.Should().Be(1);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(181)]
    public void Complete_WithInvalidDailyCommitment_ShouldFail(int minutes)
    {
        var action = () => UserLearningPreference.Complete(
            Guid.NewGuid(), LearningGoal.PrepareForInterviews, minutes, 5,
            [LearningFocusArea.DotNet], Now);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("dailyCommitmentMinutes");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void Complete_WithInvalidWeeklyTarget_ShouldFail(int days)
    {
        var action = () => UserLearningPreference.Complete(
            Guid.NewGuid(), LearningGoal.PrepareForInterviews, 30, days,
            [LearningFocusArea.DotNet], Now);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("weeklyTargetDays");
    }

    [Fact]
    public void Complete_ShouldNormalizeDuplicateFocusAreas()
    {
        var preference = UserLearningPreference.Complete(
            Guid.NewGuid(), LearningGoal.StrengthenDotNetSkills, 30, 5,
            [LearningFocusArea.CSharp, LearningFocusArea.CSharp], Now);

        preference.FocusAreas.Should().ContainSingle();
    }

    [Fact]
    public void Complete_WithMoreThanFourFocusAreas_ShouldFail()
    {
        var action = () => UserLearningPreference.Complete(
            Guid.NewGuid(), LearningGoal.PrepareForInterviews, 30, 5,
            [LearningFocusArea.CSharp, LearningFocusArea.DotNet,
                LearningFocusArea.SystemDesign, LearningFocusArea.Databases,
                LearningFocusArea.InterviewCommunication], Now);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("focusAreas");
    }

    [Fact]
    public void CompleteSamePayload_ShouldNotIncreaseVersion()
    {
        var preference = Create();

        var changed = preference.CompleteOnboarding(
            LearningGoal.PrepareForInterviews, 45, 5,
            [LearningFocusArea.DotNet, LearningFocusArea.Databases],
            Now.AddMinutes(1));

        changed.Should().BeFalse();
        preference.Version.Should().Be(1);
    }

    [Fact]
    public void CompleteDifferentPayload_ShouldIncreaseVersion()
    {
        var preference = Create();

        var changed = preference.CompleteOnboarding(
            LearningGoal.PracticeAlgorithms, 60, 7,
            [LearningFocusArea.AlgorithmsAndDataStructures],
            Now.AddMinutes(1));

        changed.Should().BeTrue();
        preference.Version.Should().Be(2);
    }

    [Fact]
    public void Skip_ShouldApplyDomainDefaults()
    {
        var preference = UserLearningPreference.Skip(Guid.NewGuid(), Now);

        preference.Goal.Should().Be(LearningPreferenceDefaults.Goal);
        preference.DailyCommitmentMinutes.Should().Be(30);
        preference.WeeklyTargetDays.Should().Be(5);
        preference.FocusAreas.Should().BeEmpty();
        preference.CompletionType.Should().Be(OnboardingCompletionType.Skipped);
    }

    private static UserLearningPreference Create() =>
        UserLearningPreference.Complete(
            Guid.NewGuid(), LearningGoal.PrepareForInterviews, 45, 5,
            [LearningFocusArea.Databases, LearningFocusArea.DotNet], Now);
}
