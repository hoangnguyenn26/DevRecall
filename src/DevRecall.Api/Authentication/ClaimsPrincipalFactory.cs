using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace DevRecall.Api.Authentication;

internal static class ClaimsPrincipalFactory
{
    public static ClaimsPrincipal Create(
        Guid userId,
        string email,
        string displayName)
    {
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, displayName)
        ];
        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }
}
