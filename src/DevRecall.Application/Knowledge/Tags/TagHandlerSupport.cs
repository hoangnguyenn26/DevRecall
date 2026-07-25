using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge.Tags;

namespace DevRecall.Application.Knowledge.Tags;

internal static class TagHandlerSupport
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

    public static void ValidateName(string name)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["Tag name is required."];
        }
        else
        {
            var normalizedName = string.Join(
                ' ',
                name.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            if (normalizedName.Length > TagName.MaxLength)
            {
                errors["name"] =
                    [$"Tag name cannot exceed {TagName.MaxLength} characters."];
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
