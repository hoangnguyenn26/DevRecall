using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;

namespace DevRecall.Application.Dsa;

internal static class DsaHandlerSupport
{
    public static Guid GetCurrentUserId(ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new UnauthorizedException(
                "IDENTITY_UNAUTHENTICATED",
                "Authentication is required.");
        }

        return currentUser.UserId.Value;
    }
}
