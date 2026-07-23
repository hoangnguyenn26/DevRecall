using System.Net.Mail;
using DevRecall.Application.Common.Exceptions;
using DevRecall.Domain.Identity;

namespace DevRecall.Application.Identity.Register;

public sealed class RegisterUserHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
{
    public async Task<RegisterUserResult> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        Validate(command);

        var normalizedEmail = UserEmail.Normalize(command.Email);
        var alreadyExists = await userRepository.ExistsByNormalizedEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (alreadyExists)
        {
            throw new ConflictException(
                UserErrors.EmailAlreadyExists.Code,
                UserErrors.EmailAlreadyExists.Message);
        }

        var passwordHash = passwordHasher.Hash(command.Password);
        var user = User.Create(Guid.NewGuid(), command.Email,
            command.DisplayName, passwordHash, DateTimeOffset.UtcNow);

        userRepository.Add(user);

        await userRepository.SaveChangesAsync(cancellationToken);

        return new RegisterUserResult(
            user.Id,
            user.Email,
            user.DisplayName);
    }

    private static void Validate(RegisterUserCommand command)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            errors["email"] = ["Email is required."];
        }
        else if (!MailAddress.TryCreate(command.Email, out _))
        {
            errors["email"] = ["Email format is invalid."];
        }

        if (string.IsNullOrWhiteSpace(command.DisplayName))
        {
            errors["displayName"] = ["Display name is required."];
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            errors["password"] = ["Password is required."];
        }
        else if (command.Password.Length < 8)
        {
            errors["password"] = ["Password must contain at least 8 characters."];
        }
        else if (command.Password.Length > 128)
        {
            errors["password"] = ["Password must not exceed 128 characters."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
