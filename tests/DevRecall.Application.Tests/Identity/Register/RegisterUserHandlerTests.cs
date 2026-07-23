using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Identity.Register;
using DevRecall.Domain.Identity;
using FluentAssertions;

namespace DevRecall.Application.Tests.Identity.Register;

public sealed class RegisterUserHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldCreateUser()
    {
        var repository = new FakeUserRepository();
        var handler = new RegisterUserHandler(
            repository,
            new FakePasswordHasher());

        var result = await handler.HandleAsync(
            new RegisterUserCommand(
                "  Hoang@example.com  ",
                "  Hoang Nguyen  ",
                "Example123!"),
            CancellationToken.None);

        repository.LookupNormalizedEmail.Should().Be("HOANG@EXAMPLE.COM");
        repository.AddedUser.Should().NotBeNull();
        repository.AddedUser!.PasswordHash.Should().Be("HASHED:Example123!");
        repository.SaveChangesCalled.Should().BeTrue();
        result.Id.Should().Be(repository.AddedUser.Id);
        result.Email.Should().Be("Hoang@example.com");
        result.DisplayName.Should().Be("Hoang Nguyen");
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateEmail_ShouldThrowConflictException()
    {
        var repository = new FakeUserRepository
        {
            Exists = true
        };
        var handler = new RegisterUserHandler(
            repository,
            new FakePasswordHasher());

        var action = async () => await handler.HandleAsync(
            ValidCommand(),
            CancellationToken.None);

        var exception = await action.Should()
            .ThrowAsync<ConflictException>();
        exception.Which.ErrorCode.Should().Be("IDENTITY_EMAIL_ALREADY_EXISTS");
        repository.AddedUser.Should().BeNull();
        repository.SaveChangesCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WithEmptyFields_ShouldReturnAllValidationErrors()
    {
        var handler = CreateHandler();

        var action = async () => await handler.HandleAsync(
            new RegisterUserCommand("", "", ""),
            CancellationToken.None);

        var exception = await action.Should()
            .ThrowAsync<ValidationException>();
        exception.Which.Errors.Keys.Should()
            .BeEquivalentTo("email", "displayName", "password");
    }

    [Fact]
    public async Task HandleAsync_WithInvalidEmail_ShouldThrowValidationException()
    {
        var handler = CreateHandler();

        var action = async () => await handler.HandleAsync(
            ValidCommand() with { Email = "not-an-email" },
            CancellationToken.None);

        var exception = await action.Should()
            .ThrowAsync<ValidationException>();
        exception.Which.Errors["email"].Should()
            .ContainSingle("Email format is invalid.");
    }

    [Theory]
    [InlineData("short", "Password must contain at least 8 characters.")]
    [InlineData(
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        "Password must not exceed 128 characters.")]
    public async Task HandleAsync_WithInvalidPassword_ShouldThrowValidationException(
        string password,
        string expectedError)
    {
        var handler = CreateHandler();

        var action = async () => await handler.HandleAsync(
            ValidCommand() with { Password = password },
            CancellationToken.None);

        var exception = await action.Should()
            .ThrowAsync<ValidationException>();
        exception.Which.Errors["password"].Should()
            .ContainSingle(expectedError);
    }

    private static RegisterUserHandler CreateHandler()
    {
        return new RegisterUserHandler(
            new FakeUserRepository(),
            new FakePasswordHasher());
    }

    private static RegisterUserCommand ValidCommand()
    {
        return new RegisterUserCommand(
            "hoang@example.com",
            "Hoang",
            "Example123!");
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public bool Exists { get; init; }

        public string? LookupNormalizedEmail { get; private set; }

        public User? AddedUser { get; private set; }

        public bool SaveChangesCalled { get; private set; }

        public Task<bool> ExistsByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            LookupNormalizedEmail = normalizedEmail;
            return Task.FromResult(Exists);
        }

        public Task<User?> GetByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<User?>(null);
        }

        public void Add(User user)
        {
            AddedUser = user;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
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
