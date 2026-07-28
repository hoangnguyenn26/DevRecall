using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Application.Study;

internal static class StudySessionSupport
{
    public static Guid GetUserId(ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new UnauthorizedException(
                "IDENTITY_UNAUTHENTICATED", "Authentication is required.");
        }

        return currentUser.UserId.Value;
    }

    public static void ValidatePlan(
        string? title, int plannedDurationMinutes, string? notes)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(title))
        {
            errors["title"] = ["Study session title is required."];
        }
        else if (title.Trim().Length > StudySessionText.TitleMaxLength)
        {
            errors["title"] =
                [$"Title cannot exceed {StudySessionText.TitleMaxLength} characters."];
        }

        if (plannedDurationMinutes is < 0
            or > StudySessionText.MaximumPlannedDurationMinutes)
        {
            errors["plannedDurationMinutes"] =
                [$"Planned duration must be between 0 and {StudySessionText.MaximumPlannedDurationMinutes} minutes."];
        }

        if (notes?.Trim().Length > StudySessionText.SessionNotesMaxLength)
        {
            errors["notes"] =
                [$"Notes cannot exceed {StudySessionText.SessionNotesMaxLength} characters."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    public static void ValidateItemNotes(string? notes)
    {
        if (notes?.Trim().Length > StudySessionText.ItemNotesMaxLength)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["notes"] =
                        [$"Notes cannot exceed {StudySessionText.ItemNotesMaxLength} characters."]
                });
        }
    }

    public static void ValidateExpectedVersion(int expectedVersion)
    {
        if (expectedVersion <= 0)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["expectedVersion"] =
                        ["Expected version must be greater than zero."]
                });
        }
    }

    public static ConflictException MapDomainConflict(
        StudySessionDomainException exception) =>
        new(exception.Error.Code, exception.Error.Message);

    public static async Task SaveWithConcurrencyMappingAsync(
        IStudySessionRepository repository,
        CancellationToken cancellationToken)
    {
        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                StudySessionErrors.Conflict.Code,
                StudySessionErrors.Conflict.Message);
        }
    }

    public static void EnsureCanEditPlan(StudySession session)
    {
        if (session.Status == StudySessionStatus.Planned)
        {
            return;
        }

        throw ConflictForSessionStatus(session.Status);
    }

    public static ConflictException ConflictForSessionStatus(
        StudySessionStatus status) => status switch
        {
            StudySessionStatus.InProgress => new ConflictException(
                StudySessionErrors.SessionAlreadyStarted.Code,
                StudySessionErrors.SessionAlreadyStarted.Message),
            StudySessionStatus.Completed => new ConflictException(
                StudySessionErrors.SessionCompleted.Code,
                StudySessionErrors.SessionCompleted.Message),
            StudySessionStatus.Cancelled => new ConflictException(
                StudySessionErrors.SessionCancelled.Code,
                StudySessionErrors.SessionCancelled.Message),
            _ => new ConflictException(
                StudySessionErrors.SessionInvalidState.Code,
                StudySessionErrors.SessionInvalidState.Message)
        };

    public static ConflictException ItemMutationConflict(
        StudySession session)
    {
        if (session.Status != StudySessionStatus.InProgress)
        {
            return session.Status == StudySessionStatus.Planned
                ? new ConflictException(
                    StudySessionErrors.SessionNotStarted.Code,
                    StudySessionErrors.SessionNotStarted.Message)
                : ConflictForSessionStatus(session.Status);
        }

        return new ConflictException(
            StudySessionErrors.ItemInvalidState.Code,
            StudySessionErrors.ItemInvalidState.Message);
    }
}

internal static class StudyResourceTypeParser
{
    public static StudyResourceType Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Error("Resource type is required.");
        }

        var normalized = value.Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
        if (!Enum.TryParse<StudyResourceType>(
                normalized, true, out var resourceType)
            || !Enum.IsDefined(resourceType))
        {
            throw Error(
                "Resource type must be KnowledgeNode, InterviewQuestion, DsaProblem, or ReviewItem.");
        }

        return resourceType;
    }

    private static ValidationException Error(string message) =>
        new(new Dictionary<string, string[]>
        {
            ["resourceType"] = [message]
        });
}

internal static class StudySessionStatusParser
{
    public static StudySessionStatus Parse(string value)
    {
        var normalized = value.Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
        if (!Enum.TryParse<StudySessionStatus>(
                normalized, true, out var status)
            || !Enum.IsDefined(status))
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["status"] =
                        ["Status must be Planned, InProgress, Completed, or Cancelled."]
                });
        }

        return status;
    }
}
