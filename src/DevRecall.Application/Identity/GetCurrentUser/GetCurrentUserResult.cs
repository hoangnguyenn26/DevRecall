namespace DevRecall.Application.Identity.GetCurrentUser;

public sealed record GetCurrentUserResult(
    Guid Id,
    string Email,
    string DisplayName);
