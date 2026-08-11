using DevRecall.Domain.LearningProfiles;
using FluentAssertions;

namespace DevRecall.Domain.Tests.LearningProfiles;

public sealed class LearningProfileTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldBuildOwnedConfiguredProfile()
    {
        var userId = Guid.NewGuid();
        var profile = Create(userId);

        profile.UserId.Should().Be(userId);
        profile.Version.Should().Be(1);
        profile.Technologies.Should().HaveCount(2);
        profile.Goals.Should().ContainSingle();
        profile.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldRejectDuplicateTechnologiesAndGoals()
    {
        var duplicateTechnologies = () => LearningProfile.Create(Guid.NewGuid(), Guid.NewGuid(),
            TargetRole.BackendDeveloper, ExperienceLevel.Junior, 45,
            [(Technology.CSharp, true), (Technology.CSharp, false)],
            [LearningProfileGoal.PrepareForInterviews], Now);
        var duplicateGoals = () => LearningProfile.Create(Guid.NewGuid(), Guid.NewGuid(),
            TargetRole.BackendDeveloper, ExperienceLevel.Junior, 45,
            [(Technology.CSharp, true)],
            [LearningProfileGoal.PrepareForInterviews, LearningProfileGoal.PrepareForInterviews], Now);

        duplicateTechnologies.Should().Throw<ArgumentException>();
        duplicateGoals.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(4)]
    [InlineData(47)]
    [InlineData(481)]
    public void Create_ShouldRejectInvalidAvailableMinutes(int minutes)
    {
        var act = () => LearningProfile.Create(Guid.NewGuid(), Guid.NewGuid(),
            TargetRole.BackendDeveloper, ExperienceLevel.Junior, minutes,
            [(Technology.CSharp, true)], [LearningProfileGoal.PrepareForInterviews], Now);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Update_ShouldBeSetBasedAndNoOpWhenOnlyOrderChanges()
    {
        var profile = Create(Guid.NewGuid());
        var changed = profile.Update(TargetRole.BackendDeveloper, ExperienceLevel.Junior, 45,
            [(Technology.DotNet, false), (Technology.CSharp, true)],
            [LearningProfileGoal.PrepareForInterviews], Now.AddDays(1));

        changed.Should().BeFalse();
        profile.Version.Should().Be(1);
        profile.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Update_ShouldTreatPrimaryFlagAsMeaningfulAndLimitPrimaryTechnologies()
    {
        var profile = Create(Guid.NewGuid());
        var originalIds = profile.Technologies.ToDictionary(item => item.Technology, item => item.Id);
        profile.Update(TargetRole.BackendDeveloper, ExperienceLevel.Junior, 45,
            [(Technology.CSharp, false), (Technology.DotNet, false)],
            [LearningProfileGoal.PrepareForInterviews], Now.AddDays(1)).Should().BeTrue();
        profile.Version.Should().Be(2);
        profile.Technologies.Should().OnlyContain(item => item.Id == originalIds[item.Technology]);

        var tooManyPrimary = () => LearningProfile.Create(Guid.NewGuid(), Guid.NewGuid(),
            TargetRole.BackendDeveloper, ExperienceLevel.Junior, 45,
            [(Technology.CSharp, true), (Technology.DotNet, true), (Technology.AspNetCore, true),
                (Technology.EfCore, true), (Technology.PostgreSql, true), (Technology.Docker, true)],
            [LearningProfileGoal.PrepareForInterviews], Now);
        tooManyPrimary.Should().Throw<ArgumentException>();
    }

    private static LearningProfile Create(Guid userId) => LearningProfile.Create(Guid.NewGuid(), userId,
        TargetRole.BackendDeveloper, ExperienceLevel.Junior, 45,
        [(Technology.CSharp, true), (Technology.DotNet, false)],
        [LearningProfileGoal.PrepareForInterviews], Now);
}
