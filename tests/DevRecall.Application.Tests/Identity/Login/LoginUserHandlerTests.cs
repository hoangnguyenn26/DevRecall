using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Identity.Login;
using DevRecall.Domain.Identity;
using FluentAssertions;

namespace DevRecall.Application.Tests.Identity.Login;

public sealed class LoginUserHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithCorrectCredentials_ShouldReturnUser()
    {
        var user = CreateUser();
        var repository = new FakeUserRepository { User = user };
        var handler = new LoginUserHandler(repository, new FakePasswordHasher());

        var result = await handler.HandleAsync(
            new LoginUserCommand("  HOANG@example.com  ", "Example123!"),
            CancellationToken.None);

        repository.LookupNormalizedEmail.Should().Be("HOANG@EXAMPLE.COM");
        result.Should().Be(new LoginUserResult(user.Id, user.Email, user.DisplayName));
    }

    [Fact]
    public async Task HandleAsync_WithUnknownEmail_ShouldThrowUnauthorizedException()
    {
        var handler = new LoginUserHandler(
            new FakeUserRepository(),
            new FakePasswordHasher());

        var action = () => handler.HandleAsync(ValidCommand(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.ErrorCode.Should().Be("IDENTITY_INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task HandleAsync_WithWrongPassword_ShouldThrowUnauthorizedException()
    {
        var repository = new FakeUserRepository { User = CreateUser() };
        var handler = new LoginUserHandler(repository, new FakePasswordHasher());

        var action = () => handler.HandleAsync(
            ValidCommand() with { Password = "WrongPassword" },
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.ErrorCode.Should().Be("IDENTITY_INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task HandleAsync_WithDisabledUser_ShouldThrowForbiddenException()
    {
        var user = CreateUser();
        typeof(User).GetProperty(nameof(User.Status))!
            .SetValue(user, UserStatus.Disabled);
        var repository = new FakeUserRepository { User = user };
        var handler = new LoginUserHandler(repository, new FakePasswordHasher());

        var action = () => handler.HandleAsync(ValidCommand(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ForbiddenException>();
        exception.Which.ErrorCode.Should().Be("IDENTITY_USER_DISABLED");
    }

    [Fact]
    public async Task HandleAsync_WithInvalidRequest_ShouldReturnAllValidationErrors()
    {
        var handler = new LoginUserHandler(
            new FakeUserRepository(),
            new FakePasswordHasher());

        var action = () => handler.HandleAsync(
            new LoginUserCommand("", ""),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Keys.Should().BeEquivalentTo("email", "password");
    }

    private static LoginUserCommand ValidCommand()
    {
        return new LoginUserCommand("hoang@example.com", "Example123!");
    }

    private static User CreateUser()
    {
        return User.Create(
            Guid.NewGuid(),
            "hoang@example.com",
            "Hoang Nguyen",
            "HASHED:Example123!",
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? User { get; init; }

        public string? LookupNormalizedEmail { get; private set; }

        public Task<bool> ExistsByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<User?> GetByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            LookupNormalizedEmail = normalizedEmail;
            return Task.FromResult(User);
        }

        public Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(User);
        }

        public void Add(User user)
        {
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return $"HASHED:{password}";
        }

        public bool Verify(string passwordHash, string providedPassword)
        {
            return passwordHash == $"HASHED:{providedPassword}";
        }
    }
}
