using DevRecall.Domain.Common.Errors;

namespace DevRecall.Domain.Identity;

public static class UserErrors
{
    public static readonly DomainError EmailAlreadyExists =
        new(
            "IDENTITY_EMAIL_ALREADY_EXISTS",
            "A user with this email already exists.");

    public static readonly DomainError InvalidCredentials =
        new(
            "IDENTITY_INVALID_CREDENTIALS",
            "The email or password is incorrect.");
}
