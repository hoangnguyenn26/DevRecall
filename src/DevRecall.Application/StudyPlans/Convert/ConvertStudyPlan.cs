using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.StudyPlans.Resources;
using DevRecall.Domain.Study;
using DevRecall.Domain.StudyPlans;

namespace DevRecall.Application.StudyPlans.Convert;

public sealed record ConvertStudyPlanCommand(
    Guid StudyPlanId, int ExpectedVersion);

public sealed record ConvertStudyPlanResult(
    Guid StudyPlanId, string StudyPlanStatus, Guid StudySessionId,
    string StudySessionStatus, string Title, int ItemCount,
    int TotalPlannedDurationMinutes, DateTimeOffset ConvertedAtUtc,
    int StudyPlanVersion, int StudySessionVersion);

public interface IStudyPlanConversionPersistence
{
    Task<StudyPlan?> GetPlanForUpdateAsync(
        Guid userId, Guid studyPlanId, CancellationToken cancellationToken);
    Task<StudySession?> GetStudySessionAsync(
        Guid userId, Guid studySessionId, CancellationToken cancellationToken);
    void AddStudySession(StudySession studySession);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class StudyPlanConversionConflictException(
    string message, Exception innerException) : Exception(message, innerException);

public sealed class ConvertStudyPlanHandler(
    IStudyPlanConversionPersistence persistence,
    IStudyPlanResourceSummaryReader resourceReader,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<ConvertStudyPlanResult> HandleAsync(
        ConvertStudyPlanCommand command, CancellationToken cancellationToken)
    {
        Validate(command);
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException(
                "AUTH_REQUIRED", "Authentication is required.");
        var plan = await persistence.GetPlanForUpdateAsync(
            userId, command.StudyPlanId, cancellationToken)
            ?? throw new NotFoundException(
                StudyPlanErrors.NotFound.Code, StudyPlanErrors.NotFound.Message);
        if (plan.Version != command.ExpectedVersion)
        {
            throw new ConflictException(
                StudyPlanErrors.Conflict.Code, StudyPlanErrors.Conflict.Message);
        }

        if (plan.Status == StudyPlanStatus.Converted)
        {
            return await MapExistingAsync(plan, userId, cancellationToken);
        }

        if (plan.Status != StudyPlanStatus.Ready)
        {
            throw new ConflictException(
                StudyPlanErrors.NotReady.Code, StudyPlanErrors.NotReady.Message);
        }

        if (plan.Items.Count == 0)
        {
            throw new ConflictException(
                StudyPlanErrors.EmptyPlan.Code, StudyPlanErrors.EmptyPlan.Message);
        }

        var references = plan.Items.Select(item =>
            new StudyPlanResourceReference(
                item.ResourceType, item.ResourceId)).Distinct().ToArray();
        var summaries = await resourceReader.ReadManyAsync(
            userId, references, cancellationToken);
        var available = summaries.Where(item => item.IsAvailable)
            .Select(item => (item.ResourceType, item.ResourceId)).ToHashSet();
        if (references.Any(reference =>
            !available.Contains((reference.ResourceType, reference.ResourceId))))
        {
            throw new ConflictException(
                StudyPlanErrors.ResourceUnavailable.Code,
                StudyPlanErrors.ResourceUnavailable.Message);
        }

        var convertedAtUtc = utcClock.UtcNow;
        var session = CreateSessionFromPlan(
            Guid.NewGuid(), userId, plan, convertedAtUtc);
        try
        {
            plan.MarkConverted(
                command.ExpectedVersion, session.Id, convertedAtUtc);
        }
        catch (StudyPlanDomainException exception)
        {
            throw new ConflictException(
                exception.Error.Code, exception.Error.Message);
        }

        persistence.AddStudySession(session);
        try
        {
            await persistence.SaveChangesAsync(cancellationToken);
        }
        catch (StudyPlanConversionConflictException)
        {
            throw new ConflictException(
                StudyPlanErrors.Conflict.Code, StudyPlanErrors.Conflict.Message);
        }

        return Map(plan, session);
    }

    private async Task<ConvertStudyPlanResult> MapExistingAsync(
        StudyPlan plan, Guid userId, CancellationToken cancellationToken)
    {
        var sessionId = plan.ConvertedStudySessionId
            ?? throw new ConflictException(
                StudyPlanErrors.AlreadyConverted.Code,
                StudyPlanErrors.AlreadyConverted.Message);
        var session = await persistence.GetStudySessionAsync(
            userId, sessionId, cancellationToken)
            ?? throw new ConflictException(
                StudyPlanErrors.AlreadyConverted.Code,
                StudyPlanErrors.AlreadyConverted.Message);
        return Map(plan, session);
    }

    private static StudySession CreateSessionFromPlan(
        Guid sessionId, Guid userId, StudyPlan plan,
        DateTimeOffset createdAtUtc)
    {
        var items = plan.Items.OrderBy(item => item.Position)
            .Select(item => new InitialStudySessionItem(
                Guid.NewGuid(),
                StudyPlanMappingPolicy.MapToStudyResourceType(
                    item.ResourceType),
                item.ResourceId)).ToArray();
        return StudySession.CreateFromPlan(
            sessionId, userId, plan.Title,
            plan.TotalPlannedDurationMinutes, items, createdAtUtc);
    }

    private static ConvertStudyPlanResult Map(
        StudyPlan plan, StudySession session) =>
        new(
            plan.Id, plan.Status.ToString(), session.Id,
            session.Status.ToString(), session.Title, session.Items.Count,
            session.PlannedDurationMinutes,
            plan.ConvertedAtUtc
                ?? throw new InvalidOperationException(
                    "Converted plan has no conversion timestamp."),
            plan.Version, session.Version);

    private static void Validate(ConvertStudyPlanCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (command.StudyPlanId == Guid.Empty)
        {
            errors["studyPlanId"] = ["Study plan id is required."];
        }

        if (command.ExpectedVersion <= 0)
        {
            errors["expectedVersion"] = ["Expected version must be positive."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
