namespace DevRecall.Contracts.Auth;

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string DisplayName);

public sealed record CsrfTokenResponse(string RequestToken, string HeaderName);
