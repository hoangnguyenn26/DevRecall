namespace DevRecall.Application.Identity.Register;

public sealed record RegisterUserCommand(
    string Email,
    string DisplayName,
    string Password);
