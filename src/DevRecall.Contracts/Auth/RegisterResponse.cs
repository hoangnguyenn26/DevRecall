namespace DevRecall.Contracts.Auth;

public sealed record RegisterResponse(
    Guid Id,
    string Email,
    string DisplayName);
