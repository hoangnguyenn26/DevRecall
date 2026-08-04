using DevRecall.Domain.Recommendations;
using DevRecall.Domain.StudyPlans;

namespace DevRecall.Application.Today;

public enum TodayActionType
{
    ContinueStudySession = 1,
    StartStudyPlan = 2,
    ContinueStudyPlan = 3,
    StartReview = 4,
    GenerateStudyPlan = 5,
    OpenRecommendation = 6,
    CreateKnowledge = 7
}

public sealed record TodayActionContext(
    ActiveStudySessionCandidate? ActiveSession,
    TodayStudyPlanReadModel? StudyPlan,
    int ReviewsDue,
    TodayRecommendationReadModel? TopRecommendation,
    int ActiveRecommendationCount);
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
        if (context.ActiveSession is { } session)
        {
            return Create(
                TodayActionType.ContinueStudySession,
                "Continue your study session",
                session.RemainingItemCount == 1
                    ? "1 item remains in your active session."
                    : $"{session.RemainingItemCount} items remain in your active session.",
                "Continue session", "timer", session.Id,
                new(session.Id, null, session.Title, session.RemainingMinutes,
                    session.RemainingItemCount, null));
        }

        if (context.StudyPlan is { Status: StudyPlanStatus.Ready } readyPlan)
        {
            return CreatePlanAction(
                TodayActionType.StartStudyPlan, readyPlan,
                "Your study plan is ready", "Start study plan", "list-checks");
        }

        if (context.ReviewsDue > 0)
        {
            var noun = context.ReviewsDue == 1 ? "review" : "reviews";
            return Create(
                TodayActionType.StartReview,
                $"{context.ReviewsDue} {noun} due today",
                "Complete scheduled reviews before they accumulate.",
                "Start review", "refresh", null,
                new(null, null, null, null, context.ReviewsDue, null));
        }

        if (context.StudyPlan is { Status: StudyPlanStatus.Draft } draftPlan)
        {
            return CreatePlanAction(
                TodayActionType.ContinueStudyPlan, draftPlan,
                "Finish your study plan", "Continue planning", "pencil");
        }

        if (context.TopRecommendation is
            { Priority: RecommendationPriority.Critical or RecommendationPriority.High } recommendation)
        {
            return Create(
                TodayActionType.OpenRecommendation,
                recommendation.ResourceTitle,
                $"A {recommendation.Priority.ToString().ToLowerInvariant()} priority recommendation is ready.",
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

        return Create(
            TodayActionType.CreateKnowledge,
            "Capture your first concept",
            "Start with something you recently learned. DevRecall will help you organize, review and practice it later.",
            "Create knowledge", "book-open", null, null);
    }

    private static TodayNextAction CreatePlanAction(
        TodayActionType type, TodayStudyPlanReadModel plan,
        string title, string label, string icon) =>
        Create(
            type, title,
            $"{plan.ItemCount} items · {plan.TotalPlannedDurationMinutes} minutes",
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
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

    private static Guid RequireId(Guid? id) =>
        id is { } value && value != Guid.Empty
            ? value
            : throw new InvalidOperationException(
                "The selected Today action requires a resource id.");
}
