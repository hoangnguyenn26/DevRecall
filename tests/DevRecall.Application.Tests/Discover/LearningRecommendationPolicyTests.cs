using DevRecall.Application.Discover;
using DevRecall.Application.LearningProfiles;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using FluentAssertions;

namespace DevRecall.Application.Tests.Discover;

public sealed class LearningRecommendationPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
    private static readonly DiscoverTopic Di = new(Guid.NewGuid(), "dependency-injection", "Dependency Injection");
    private static LearningProfileSignals Profile(Technology? primary = Technology.AspNetCore,
        Technology? secondary = null, LearningProfileGoal? goal = LearningProfileGoal.ImproveBackendFundamentals,
        ExperienceLevel? level = ExperienceLevel.Junior, int? minutes = 30) => new(null, level,
            primary is null ? new HashSet<Technology>() : new HashSet<Technology> { primary.Value },
            secondary is null ? new HashSet<Technology>() : new HashSet<Technology> { secondary.Value },
            goal is null ? new HashSet<LearningProfileGoal>() : new HashSet<LearningProfileGoal> { goal.Value }, minutes, true);
    private static DiscoverCandidate Candidate(string slug, Technology? technology = Technology.AspNetCore,
        LearningProfileGoal? goal = LearningProfileGoal.ImproveBackendFundamentals,
        ContentDifficulty difficulty = ContentDifficulty.Intermediate, int minutes = 15) => new(Guid.NewGuid(),
            slug, slug, "A focused lesson", difficulty, minutes, Now,
            technology is null ? [] : [technology.Value], goal is null ? [] : [goal.Value], [Di]);

    [Fact]
    public void SemanticGate_ShouldRejectTimeAndDifficultyOnlyAndNeverInferRelatedTechnologies()
    {
        var unrelated = Candidate("unrelated", Technology.AspNetCore, null, ContentDifficulty.Beginner);
        var score = LearningRecommendationPolicy.Score(unrelated, Profile(Technology.CSharp, goal: null), []);
        score.Total.Should().Be(0);
        score.HasSemanticMatch.Should().BeFalse();
        score.Reasons.Should().BeEmpty();
        LearningRecommendationPolicy.Build(new(Profile(Technology.CSharp, goal: null), [], [unrelated]))
            .Recommended.Should().BeEmpty();
    }

    [Fact]
    public void MissingProfile_ShouldAllowExplicitMappedWeakSignalButNotGuessDeclaredSignals()
    {
        var missing = Profile(null, goal: null, level: null, minutes: null) with { IsConfigured = false };
        var lesson = Candidate("weak-only", null, null);
        LearningRecommendationPolicy.Score(lesson, missing, []).Total.Should().Be(0);
        var weak = LearningRecommendationPolicy.Score(lesson, missing, [Di]);
        weak.Total.Should().Be(40);
        weak.Reasons.Should().ContainSingle(reason => reason.Type == "WeakTopicMatch" && reason.Label == Di.Name);
    }

    [Fact]
    public void Signals_ShouldBeCappedRegardlessOfMatchingTagCount()
    {
        var profile = Profile(secondary: Technology.EfCore) with
        {
            Goals = new HashSet<LearningProfileGoal> { LearningProfileGoal.ImproveBackendFundamentals, LearningProfileGoal.PrepareForInterviews },
            PrimaryTechnologies = new HashSet<Technology> { Technology.AspNetCore, Technology.DotNet }
        };
        var lesson = Candidate("many-tags") with
        {
            Goals = profile.Goals.ToArray(), Technologies = [Technology.AspNetCore, Technology.DotNet, Technology.EfCore],
            Topics = [Di, new(Guid.NewGuid(), "other", "Other")]
        };
        var score = LearningRecommendationPolicy.Score(lesson, profile, lesson.Topics);
        score.Goal.Should().Be(30);
        score.Technology.Should().Be(20);
        score.WeakTopic.Should().Be(40);
        score.Reasons.Should().HaveCount(2);
        score.Reasons.Should().OnlyContain(reason => reason.Type == "WeakTopicMatch");
    }

    [Fact]
    public void PrimaryTechnology_ShouldOutrankSecondaryAndGoalPlusWeakShouldOutrankTechnologyOnly()
    {
        var profile = Profile(secondary: Technology.EfCore, goal: null);
        var primary = LearningRecommendationPolicy.Score(Candidate("primary", goal: null), profile, []);
        var secondary = LearningRecommendationPolicy.Score(Candidate("secondary", Technology.EfCore, null), profile, []);
        primary.Total.Should().BeGreaterThan(secondary.Total);
        secondary.Reasons[0].Type.Should().Be("SecondaryTechnologyMatch");
        var weakGoal = LearningRecommendationPolicy.Score(Candidate("weak-goal", null), Profile(), [Di]);
        weakGoal.Total.Should().BeGreaterThan(primary.Total);
    }

    [Theory]
    [InlineData(ExperienceLevel.Beginner, ContentDifficulty.Advanced, 0)]
    [InlineData(ExperienceLevel.Junior, ContentDifficulty.Intermediate, 8)]
    [InlineData(ExperienceLevel.MidLevel, ContentDifficulty.Intermediate, 10)]
    [InlineData(ExperienceLevel.Senior, ContentDifficulty.Beginner, 2)]
    public void Difficulty_ShouldUseExplicitCompatibility(ExperienceLevel level, ContentDifficulty difficulty, int expected) =>
        LearningRecommendationPolicy.DifficultyFit(level, difficulty).Should().Be(expected);

    [Fact]
    public void TimeAndDifficulty_ShouldBeSoftAndNearFitShouldNotClaimToFitBudget()
    {
        var profile = Profile();
        var longLesson = LearningRecommendationPolicy.Score(Candidate("long", difficulty: ContentDifficulty.Advanced, minutes: 90), profile, []);
        longLesson.HasSemanticMatch.Should().BeTrue();
        longLesson.Time.Should().Be(0);
        var near = LearningRecommendationPolicy.Score(Candidate("near", goal: null, minutes: 45), profile, []);
        near.Time.Should().Be(2);
        near.Reasons.Should().NotContain(reason => reason.Type == "TimeFit");
        LearningRecommendationPolicy.DifficultyFit(null, ContentDifficulty.Beginner).Should().Be(0);
    }

    [Fact]
    public void Ranking_ShouldBeStableBoundedAndDedupeAcrossSections()
    {
        var candidates = Enumerable.Range(0, 10).Select(index => Candidate($"lesson-{index}") with
            { Id = Guid.Parse($"00000000-0000-0000-0000-{index + 1:D12}") }).Reverse().ToArray();
        var inputs = new DiscoverInputs(Profile(), [], candidates);
        var result = LearningRecommendationPolicy.Build(inputs);
        result.Recommended.Select(item => item.Slug).Should().Equal("lesson-0", "lesson-1", "lesson-2", "lesson-3");
        result.BasedOnGoals.Should().HaveCount(3);
        result.Recommended.Concat(result.BasedOnGoals).Select(item => item.Slug).Should().OnlyHaveUniqueItems();
        for (var repeat = 0; repeat < 5; repeat++)
            LearningRecommendationPolicy.Build(inputs).Should().BeEquivalentTo(result, options => options.WithStrictOrdering());
        var newer = candidates[0] with { PublishedAtUtc = Now.AddDays(1) };
        LearningRecommendationPolicy.Build(inputs with { Candidates = [.. candidates.Skip(1), newer] })
            .Recommended[0].Slug.Should().Be(newer.Slug);
    }

    [Fact]
    public void Personas_ShouldPrioritizeBackendAndEfFocusAndKeepNoProfileFallbackTruthful()
    {
        var di = Candidate("service-lifetimes");
        var ef = Candidate("tracking", Technology.EfCore);
        var dsa = Candidate("two-pointers", Technology.DataStructuresAlgorithms, LearningProfileGoal.ImproveDsa, ContentDifficulty.Beginner);
        var candidates = new[] { ef, dsa, di };
        LearningRecommendationPolicy.Build(new(Profile(), [], candidates)).Recommended[0].Slug.Should().Be(di.Slug);
        LearningRecommendationPolicy.Build(new(Profile(Technology.EfCore), [], candidates)).Recommended[0].Slug.Should().Be(ef.Slug);
        var interview = Candidate("interview-concepts", Technology.CSharp, LearningProfileGoal.PrepareForInterviews);
        LearningRecommendationPolicy.Build(new(Profile(Technology.CSharp, goal: LearningProfileGoal.PrepareForInterviews), [], [di, ef, interview]))
            .Recommended.Should().ContainSingle(item => item.Slug == interview.Slug);
        LearningRecommendationPolicy.Build(new(Profile(null, goal: null, level: null, minutes: null) with { IsConfigured = false }, [], candidates))
            .Recommended.Should().BeEmpty();
        var sparse = Profile(Technology.EfCore, goal: null, level: null, minutes: null) with { IsConfigured = false };
        var partial = LearningRecommendationPolicy.Build(new(sparse, [], candidates));
        partial.ProfileConfigured.Should().BeFalse();
        partial.Recommended.Should().ContainSingle(item => item.Slug == ef.Slug); // Never pad to four.
    }

    [Fact]
    public void Reasons_ShouldPreferSemanticSignalsAndOnlyClaimExactTimeFit()
    {
        var profile = Profile(goal: null);
        var score = LearningRecommendationPolicy.Score(Candidate("time-fit", goal: null), profile, []);
        score.Reasons.Select(reason => reason.Type).Should().Equal("PrimaryTechnologyMatch", "TimeFit");
        score.Reasons[1].AvailableMinutes.Should().Be(30);
        score.Difficulty.Should().Be(8); // Still a ranking adjustment, not a visible ability claim.
        score.Reasons.Should().NotContain(reason => reason.Type == "DifficultyFit");
        var unmapped = Di with { Id = Guid.NewGuid() };
        LearningRecommendationPolicy.Score(Candidate("not-mapped", null, null), profile, [unmapped])
            .HasSemanticMatch.Should().BeFalse(); // A matching name is not a canonical ID match.
    }

    [Fact]
    public void WeakSecondarySection_ShouldClaimBeforeGoalsWithoutRepeatingRecommended()
    {
        var candidates = Enumerable.Range(0, 12).Select(index => Candidate($"weak-{index}")).ToArray();
        var result = LearningRecommendationPolicy.Build(new(Profile(), [Di], candidates));
        result.Recommended.Should().HaveCount(4);
        result.BasedOnWeakTopics.Should().HaveCount(3);
        result.BasedOnGoals.Should().HaveCount(3);
        result.Recommended.Concat(result.BasedOnWeakTopics).Concat(result.BasedOnGoals)
            .Select(item => item.Slug).Should().OnlyHaveUniqueItems();
        result.BasedOnWeakTopics.Should().OnlyContain(item => item.Reasons.Any(reason => reason.Type == "WeakTopicMatch"));
    }
}
