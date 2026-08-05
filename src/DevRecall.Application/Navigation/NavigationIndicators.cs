using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;

namespace DevRecall.Application.Navigation;

public sealed record NavigationIndicatorsReadModel(
    int ReviewsDue, bool HasActiveStudyPlan, int CriticalWeakTopics, bool HasIncompleteOnboarding);

public interface INavigationIndicatorsReader
{
    Task<NavigationIndicatorsReadModel> ReadAsync(
        Guid userId, DateTimeOffset currentUtc, CancellationToken cancellationToken);
}

public sealed class GetNavigationIndicatorsHandler(
    INavigationIndicatorsReader reader, ICurrentUser currentUser, IUtcClock utcClock)
{
    public Task<NavigationIndicatorsReadModel> HandleAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException(
            "IDENTITY_UNAUTHENTICATED", "Authentication is required.");
        return reader.ReadAsync(userId, utcClock.UtcNow, cancellationToken);
    }
}
