namespace DevRecall.Application.Identity.Register;

public sealed record RegisterUserResult(
    Guid Id,
    string Email,
    string DisplayName);
