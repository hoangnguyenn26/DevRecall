using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Identity.GetCurrentUser;
using DevRecall.Domain.Identity;
using FluentAssertions;

namespace DevRecall.Application.Tests.Identity.GetCurrentUser;

public sealed class GetCurrentUserHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithAuthenticatedExistingUser_ShouldReturnUser()
    {
        var user = CreateUser();
        var currentUser = new FakeCurrentUser
        {
            IsAuthenticated = true,
            UserId = user.Id
        };
        var handler = new GetCurrentUserHandler(
            currentUser,
            new FakeUserRepository { User = user });

        var result = await handler.HandleAsync(CancellationToken.None);

        result.Should().Be(new GetCurrentUserResult(
            user.Id,
            user.Email,
            user.DisplayName));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task HandleAsync_WithoutAuthenticatedUserId_ShouldThrowUnauthorizedException(
        bool isAuthenticated,
        bool hasUserId)
    {
        var currentUser = new FakeCurrentUser
        {
            IsAuthenticated = isAuthenticated,
            UserId = hasUserId ? Guid.NewGuid() : null
        };
        var handler = new GetCurrentUserHandler(
            currentUser,
            new FakeUserRepository());

        var action = () => handler.HandleAsync(CancellationToken.None);

        var exception = await action.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.ErrorCode.Should().Be("IDENTITY_UNAUTHENTICATED");
    }

    [Fact]
    public async Task HandleAsync_WithMissingUser_ShouldThrowUnauthorizedException()
    {
        var currentUser = new FakeCurrentUser
        {
            IsAuthenticated = true,
            UserId = Guid.NewGuid()
        };
        var handler = new GetCurrentUserHandler(
            currentUser,
            new FakeUserRepository());

        var action = () => handler.HandleAsync(CancellationToken.None);

        var exception = await action.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.ErrorCode.Should().Be("IDENTITY_INVALID_SESSION");
    }

    private static User CreateUser()
    {
        return User.Create(
            Guid.NewGuid(),
            "hoang@example.com",
            "Hoang Nguyen",
            "password-hash",
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated { get; init; }

        public Guid? UserId { get; init; }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? User { get; init; }

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
            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(User?.Id == id ? User : null);
        }

        public void Add(User user)
        {
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
