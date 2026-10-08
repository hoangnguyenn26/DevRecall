using DevRecall.Application.LearningContent;
using DevRecall.Application.LearningProfiles;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;

namespace DevRecall.Application.Discover;

public sealed record DiscoverTopic(Guid Id, string Slug, string Name);
public sealed record DiscoverCandidate(Guid Id, string Slug, string Title, string Summary,
    ContentDifficulty Difficulty, int EstimatedMinutes, DateTimeOffset PublishedAtUtc,
    IReadOnlyList<Technology> Technologies, IReadOnlyList<LearningProfileGoal> Goals,
    IReadOnlyList<DiscoverTopic> Topics);
// Observed topics must already be explicitly mapped to canonical Content Topic IDs.
public sealed record DiscoverInputs(LearningProfileSignals Declared, IReadOnlyList<DiscoverTopic> WeakTopics,
    IReadOnlyList<DiscoverCandidate> Candidates)
{
    public IReadOnlyList<DiscoverResourceCandidate> Resources { get; init; } = [];
}
public sealed record LearningRecommendationScore(int WeakTopic, int Goal, int Technology,
    int Difficulty, int Time, bool HasSemanticMatch, IReadOnlyList<DiscoverReason> Reasons)
{
    public int Total => WeakTopic + Goal + Technology + Difficulty + Time;
}

public static class LearningRecommendationPolicy
{
    public const int CandidateLimit = 100;
    public const int RecommendedLimit = 4;
    public const int SecondarySectionLimit = 3;
    public const int WeakTopicWeight = 40;
    public const int GoalWeight = 30;
    public const int PrimaryTechnologyWeight = 20;
    public const int SecondaryTechnologyWeight = 10;
    public const int TimeFitWeight = 5;
    public const int NearTimeFitWeight = 2;

    public static LearningRecommendationScore Score(DiscoverCandidate candidate, LearningProfileSignals profile,
        IReadOnlyList<DiscoverTopic> weakTopics)
    {
        var weakIds = weakTopics.Select(topic => topic.Id).ToHashSet();
        var weak = candidate.Topics.Where(topic => weakIds.Contains(topic.Id)).OrderBy(topic => topic.Slug, StringComparer.Ordinal).ToArray();
        var goals = candidate.Goals.Where(profile.Goals.Contains).Distinct().Order().ToArray();
        var primary = candidate.Technologies.Where(profile.PrimaryTechnologies.Contains).Distinct().Order().ToArray();
        var secondary = candidate.Technologies.Where(profile.OtherTechnologies.Contains).Distinct().Order().ToArray();
        var technology = primary.Length > 0 ? PrimaryTechnologyWeight : secondary.Length > 0 ? SecondaryTechnologyWeight : 0;
        var semanticMatch = weak.Length > 0 || goals.Length > 0 || technology > 0;
        if (!semanticMatch) return new(0, 0, 0, 0, 0, false, []);
        var difficulty = DifficultyFit(profile.ExperienceLevel, candidate.Difficulty);
        var budget = profile.AvailableMinutesPerDay;
        var time = budget is > 0 ? candidate.EstimatedMinutes <= budget.Value ? TimeFitWeight
            : candidate.EstimatedMinutes * 2 <= budget.Value * 3 ? NearTimeFitWeight : 0 : 0;
        var reasons = new List<DiscoverReason>();
        reasons.AddRange(weak.Select(topic => new DiscoverReason("WeakTopicMatch", Value: topic.Slug, Label: topic.Name)));
        reasons.AddRange(goals.Select(goal => new DiscoverReason("GoalMatch", LearningProfileMetadata.GoalValue(goal))));
        var technologies = primary.Length > 0 ? primary : secondary;
        reasons.AddRange(technologies.Select(value =>
        {
            var metadata = LearningProfileMetadata.TechnologyValue(value, primary.Length > 0);
            return new DiscoverReason(primary.Length > 0 ? "PrimaryTechnologyMatch" : "SecondaryTechnologyMatch",
                Value: metadata.Value, Label: metadata.Label);
        }));
        // Self-reported experience adjusts rank, but is not a measured-skill explanation.
        if (time == TimeFitWeight) reasons.Add(new("TimeFit", AvailableMinutes: budget));
        return new(weak.Length > 0 ? WeakTopicWeight : 0, goals.Length > 0 ? GoalWeight : 0,
            technology, difficulty, time, true, reasons.Take(2).ToArray());
    }

    public static int DifficultyFit(ExperienceLevel? experience, ContentDifficulty difficulty) => (experience, difficulty) switch
    {
        (ExperienceLevel.Beginner, ContentDifficulty.Beginner) => 10,
        (ExperienceLevel.Beginner, ContentDifficulty.Intermediate) => 3,
        (ExperienceLevel.Junior, ContentDifficulty.Beginner) => 10,
        (ExperienceLevel.Junior, ContentDifficulty.Intermediate) => 8,
        (ExperienceLevel.Junior, ContentDifficulty.Advanced) => 1,
        (ExperienceLevel.MidLevel, ContentDifficulty.Beginner) => 5,
        (ExperienceLevel.MidLevel, ContentDifficulty.Intermediate) => 10,
        (ExperienceLevel.MidLevel, ContentDifficulty.Advanced) => 6,
        (ExperienceLevel.Senior, ContentDifficulty.Beginner) => 2,
        (ExperienceLevel.Senior, ContentDifficulty.Intermediate) => 8,
        (ExperienceLevel.Senior, ContentDifficulty.Advanced) => 10,
        _ => 0
    };

    public static DiscoverResult Build(DiscoverInputs inputs)
    {
        var scored = inputs.Candidates.Select(candidate => (Candidate: candidate,
            Score: Score(candidate, inputs.Declared, inputs.WeakTopics))).ToArray();
        var recommended = Rank(scored).Take(RecommendedLimit).ToArray();
        var claimed = recommended.Select(item => item.Candidate.Id).ToHashSet();
        var secondary = scored.OrderByDescending(item => item.Candidate.PublishedAtUtc).ThenBy(item => item.Candidate.Id).ToArray();
        var weak = secondary.Where(item => item.Score.WeakTopic > 0 && !claimed.Contains(item.Candidate.Id))
            .Take(SecondarySectionLimit).ToArray();
        claimed.UnionWith(weak.Select(item => item.Candidate.Id));
        var goals = secondary.Where(item => item.Score.Goal > 0 && !claimed.Contains(item.Candidate.Id))
            .Take(SecondarySectionLimit).ToArray();
        return new(inputs.Declared.IsConfigured, goals.Select(item => Map(item.Candidate,
            item.Candidate.Goals.Where(inputs.Declared.Goals.Contains).Distinct().Order().Take(2)
                .Select(goal => new DiscoverReason("GoalMatch", LearningProfileMetadata.GoalValue(goal))).ToArray())).ToArray(),
            weak.Select(item => Map(item.Candidate, item.Score.Reasons)).ToArray(),
            recommended.Select(item => Map(item.Candidate, item.Score.Reasons)).ToArray());
    }

    public static DiscoverLesson? GetTopRecommendedLesson(DiscoverInputs inputs)
    {
        var top = Rank(inputs.Candidates.Select(candidate => (Candidate: candidate,
            Score: Score(candidate, inputs.Declared, inputs.WeakTopics)))).FirstOrDefault();
        return top.Candidate is null ? null : Map(top.Candidate, top.Score.Reasons);
    }

    private static IOrderedEnumerable<(DiscoverCandidate Candidate, LearningRecommendationScore Score)> Rank(
        IEnumerable<(DiscoverCandidate Candidate, LearningRecommendationScore Score)> scored) =>
        scored.Where(item => item.Score.HasSemanticMatch && item.Score.Total > 0)
            .OrderByDescending(item => item.Score.Total).ThenByDescending(item => item.Candidate.PublishedAtUtc)
            .ThenBy(item => item.Candidate.Id);

    private static DiscoverLesson Map(DiscoverCandidate item, IReadOnlyList<DiscoverReason> reasons) => new(
        item.Slug, item.Title, item.Summary, item.Difficulty.ToString(), item.EstimatedMinutes,
        item.Technologies.Order().Select(value => LearningProfileMetadata.TechnologyValue(value, false))
            .Select(value => new LearningContentTechnologyItem(value.Value, value.Label)).ToArray(),
        item.Topics.OrderBy(topic => topic.Name).Select(topic => new LearningContentTopicItem(topic.Slug, topic.Name)).ToArray(), reasons);
}
