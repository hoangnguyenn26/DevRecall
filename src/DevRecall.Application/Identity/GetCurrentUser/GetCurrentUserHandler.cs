using DevRecall.Application.Common.Exceptions;

namespace DevRecall.Application.Identity.GetCurrentUser;

public sealed class GetCurrentUserHandler(
    ICurrentUser currentUser,
    IUserRepository userRepository)
{
    public async Task<GetCurrentUserResult> HandleAsync(
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new UnauthorizedException(
                "IDENTITY_UNAUTHENTICATED",
                "Authentication is required.");
        }

        var user = await userRepository.GetByIdAsync(
            currentUser.UserId.Value,
            cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException(
                "IDENTITY_INVALID_SESSION",
                "The authenticated session is invalid.");
        }

        return new GetCurrentUserResult(user.Id, user.Email, user.DisplayName);
    }
}
