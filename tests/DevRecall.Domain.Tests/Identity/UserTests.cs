using DevRecall.Domain.Identity;
using FluentAssertions;

namespace DevRecall.Domain.Tests.Identity;

public sealed class UserTests
{
    [Fact]
    public void Create_ShouldCreateActiveUser()
    {
        var now = DateTimeOffset.UtcNow;

        var user = User.Create(
            Guid.NewGuid(),
            "hoang@example.com",
            "Hoang Nguyen",
            "hashed-password",
            now);

        user.Email.Should().Be("hoang@example.com");
        user.NormalizedEmail.Should().Be("HOANG@EXAMPLE.COM");
        user.DisplayName.Should().Be("Hoang Nguyen");
        user.PasswordHash.Should().Be("hashed-password");
        user.Status.Should().Be(UserStatus.Active);
        user.CreatedAtUtc.Should().Be(now);
        user.UpdatedAtUtc.Should().Be(now);
    }

    [Fact]
    public void Create_ShouldTrimAndNormalizeEmailAndDisplayName()
    {
        var user = User.Create(
            Guid.NewGuid(),
            "  Hoang@example.com  ",
            "  Hoang  ",
            "hashed-password",
            DateTimeOffset.UtcNow);

        user.Email.Should().Be("Hoang@example.com");
        user.NormalizedEmail.Should().Be("HOANG@EXAMPLE.COM");
        user.DisplayName.Should().Be("Hoang");
    }

    [Fact]
    public void Create_WithEmptyId_ShouldThrowArgumentException()
    {
        var action = () => CreateUser(id: Guid.Empty);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("id");
    }

    [Fact]
    public void Create_WithEmptyEmail_ShouldThrowArgumentException()
    {
        var action = () => CreateUser(email: " ");

        action.Should().Throw<ArgumentException>()
            .WithParameterName("email");
    }

    [Fact]
    public void Create_WithEmptyDisplayName_ShouldThrowArgumentException()
    {
        var action = () => CreateUser(displayName: " ");

        action.Should().Throw<ArgumentException>()
            .WithParameterName("displayName");
    }

    [Fact]
    public void Create_WithEmptyPasswordHash_ShouldThrowArgumentException()
    {
        var action = () => CreateUser(passwordHash: " ");

        action.Should().Throw<ArgumentException>()
            .WithParameterName("passwordHash");
    }

    private static User CreateUser(
        Guid? id = null,
        string email = "hoang@example.com",
        string displayName = "Hoang",
        string passwordHash = "hashed-password")
    {
        return User.Create(
            id ?? Guid.NewGuid(),
            email,
            displayName,
            passwordHash,
            DateTimeOffset.UtcNow);
    }
}
