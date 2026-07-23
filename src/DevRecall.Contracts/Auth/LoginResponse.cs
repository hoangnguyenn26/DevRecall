namespace DevRecall.Contracts.Auth;

public sealed record LoginResponse(Guid Id, string Email, string DisplayName);
