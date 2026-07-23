using DevRecall.Application.Common.Exceptions;
using DevRecall.Domain.Identity;

namespace DevRecall.Application.Identity.Login;

public sealed class LoginUserHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
{
    public async Task<LoginUserResult> HandleAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        Validate(command);

        var normalizedEmail = UserEmail.Normalize(command.Email);
        var user = await userRepository.GetByNormalizedEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (user is null)
        {
            ThrowInvalidCredentials();
        }

        if (!passwordHasher.Verify(user!.PasswordHash, command.Password))
        {
            ThrowInvalidCredentials();
        }

        if (user.Status != UserStatus.Active)
        {
            throw new ForbiddenException(
                "IDENTITY_USER_DISABLED",
                "The user account is disabled.");
        }

        return new LoginUserResult(user.Id, user.Email, user.DisplayName);
    }

    private static void Validate(LoginUserCommand command)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            errors["email"] = ["Email is required."];
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            errors["password"] = ["Password is required."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void ThrowInvalidCredentials()
    {
        throw new UnauthorizedException(
            UserErrors.InvalidCredentials.Code,
            UserErrors.InvalidCredentials.Message);
    }
}
