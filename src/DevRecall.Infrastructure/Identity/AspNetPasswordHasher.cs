using DevRecall.Application.Identity;
using Microsoft.AspNetCore.Identity;

namespace DevRecall.Infrastructure.Identity;

internal sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return _hasher.HashPassword(
            new object(),
            password);
    }
}
