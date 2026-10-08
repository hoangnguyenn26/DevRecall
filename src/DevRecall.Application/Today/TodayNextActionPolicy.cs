using DevRecall.Domain.Recommendations;
using DevRecall.Domain.StudyPlans;
using DevRecall.Application.LearningContent;
using DevRecall.Application.Discover;

namespace DevRecall.Application.Today;

public enum TodayActionType
{
    ContinueStudySession = 1,
    StartStudyPlan = 2,
    ContinueStudyPlan = 3,
    StartReview = 4,
    GenerateStudyPlan = 5,
    OpenRecommendation = 6,
    CreateKnowledge = 7,
    ContinueLearning = 8,
    LearnRecommendedContent = 9,
    BrowseLearning = 10
}

public sealed record TodayActionContext(
    ActiveStudySessionCandidate? ActiveSession,
    TodayStudyPlanReadModel? StudyPlan,
    int ReviewsDue,
    TodayRecommendationReadModel? TopRecommendation,
    int ActiveRecommendationCount)
{
    public ContinueLearningContentItem? ContinueLesson { get; init; }
    public DiscoverLesson? RecommendedLesson { get; init; }
    public bool HasActionablePlan { get; init; } = true;
}
public sealed record TodayNextActionContextData(
    Guid? ResourceId, string? ResourceType, string? ResourceTitle,
    int? PlannedDurationMinutes, int? RemainingCount, string? Priority);
public sealed record TodayNextAction(
    TodayActionType Type, string Title, string Description, string ActionLabel,
    string TargetPath, string Icon, TodayNextActionContextData? Context);

public interface ITodayNextActionPolicy
{
    TodayNextAction SelectAction(TodayActionContext context);
}

public sealed class TodayNextActionPolicy : ITodayNextActionPolicy
{
    public TodayNextAction SelectAction(TodayActionContext context)
    {
        if (context.ReviewsDue > 0)
            return Create(TodayActionType.StartReview, $"{context.ReviewsDue} {(context.ReviewsDue == 1 ? "card is" : "cards are")} due",
                "Keep your review schedule moving while these cards are due.", "Start review", "refresh", null,
                new(null, null, null, null, context.ReviewsDue, null));

        if (context.ActiveSession is { } session)
        {
            return Create(
                TodayActionType.ContinueStudySession,
                session.Title,
                "You already started this study session.",
                "Continue session", "timer", session.Id,
                new(session.Id, null, session.Title, session.RemainingMinutes,
                    session.RemainingItemCount, null));
        }

        if (context.ContinueLesson is { } lesson)
            return new(TodayActionType.ContinueLearning, lesson.Title, "Continue the lesson you started.",
                "Continue lesson", $"/app/learn/{Uri.EscapeDataString(lesson.Slug)}?returnTo=/app", "book-open",
                new(null, "LearningContent", lesson.Title, lesson.EstimatedMinutes, null, null));

        if (context.HasActionablePlan && context.StudyPlan is { Status: StudyPlanStatus.Ready } readyPlan)
        {
            return CreatePlanAction(
                TodayActionType.StartStudyPlan, readyPlan,
                readyPlan.Title, "Open study plan", "list-checks");
        }

        if (context.HasActionablePlan && context.StudyPlan is { Status: StudyPlanStatus.Draft } draftPlan)
        {
            return CreatePlanAction(
                TodayActionType.ContinueStudyPlan, draftPlan,
                draftPlan.Title, "Continue planning", "pencil");
        }

        if (context.TopRecommendation is
            { Priority: RecommendationPriority.Critical or RecommendationPriority.High } recommendation)
        {
            return Create(
                TodayActionType.OpenRecommendation,
                recommendation.ResourceTitle,
                recommendation.ReasonSummary,
                "View recommendation", "sparkles", recommendation.RecommendationId,
                new(recommendation.ResourceId, recommendation.ResourceType.ToString(),
                    recommendation.ResourceTitle, null, null,
                    recommendation.Priority.ToString()));
        }

        if (context.ActiveRecommendationCount > 0)
        {
            return Create(
                TodayActionType.GenerateStudyPlan,
                "Turn recommendations into a study plan",
                $"{context.ActiveRecommendationCount} active recommendations are ready to organize.",
                "Generate study plan", "list-plus", null, null);
        }

        if (context.RecommendedLesson is { } suggested)
            return new(TodayActionType.LearnRecommendedContent, suggested.Title,
                RecommendationReason(suggested), "Open lesson",
                $"/app/learn/{Uri.EscapeDataString(suggested.Slug)}?returnTo=/app", "book-open",
                new(null, "LearningContent", suggested.Title, suggested.EstimatedMinutes, null, null));

        return Create(TodayActionType.BrowseLearning, "You're clear for now",
            "Explore Discover when you want something new to learn.", "Explore Discover", "book-open", null, null);
    }

    private static TodayNextAction CreatePlanAction(
        TodayActionType type, TodayStudyPlanReadModel plan,
        string title, string label, string icon) =>
        Create(
            type, title,
            "You planned this work for study. Open the plan to prepare or start a session.",
            label, icon, plan.StudyPlanId,
            new(plan.StudyPlanId, "StudyPlan", plan.Title,
                plan.TotalPlannedDurationMinutes, plan.ItemCount, null));

    private static TodayNextAction Create(
        TodayActionType type, string title, string description,
        string actionLabel, string icon, Guid? entityId,
        TodayNextActionContextData? context) =>
        new(type, title, description, actionLabel,
            GetTargetPath(type, entityId), icon, context);

    private static string GetTargetPath(TodayActionType type, Guid? entityId) =>
        type switch
        {
            TodayActionType.ContinueStudySession =>
                $"/app/study-sessions/{RequireId(entityId)}",
            TodayActionType.StartStudyPlan or TodayActionType.ContinueStudyPlan =>
                $"/app/study-plans/{RequireId(entityId)}",
            TodayActionType.StartReview => "/app/review",
            TodayActionType.GenerateStudyPlan => "/app/study-plans?action=generate",
            TodayActionType.OpenRecommendation =>
                $"/app/recommendations/{RequireId(entityId)}",
            TodayActionType.CreateKnowledge => "/app/knowledge?action=create",
            TodayActionType.BrowseLearning => "/app/discover",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

    private static string RecommendationReason(DiscoverLesson lesson)
    {
        var reasons = lesson.Reasons.Take(2).Select(reason => reason.Type switch
        {
            "GoalMatch" when reason.Goal is not null => $"Matches your goal: {reason.Goal.Label}",
            "PrimaryTechnologyMatch" or "SecondaryTechnologyMatch" when reason.Label is not null => $"Matches your focus: {reason.Label}",
            "WeakTopicMatch" when reason.Label is not null => $"Related to a weak topic: {reason.Label}",
            _ => null
        }).Where(reason => reason is not null).ToArray();
        return reasons.Length > 0 ? string.Join(" · ", reasons) : "A lesson matching your declared learning focus.";
    }

    private static Guid RequireId(Guid? id) =>
        id is { } value && value != Guid.Empty
            ? value
            : throw new InvalidOperationException(
                "The selected Today action requires a resource id.");
}
