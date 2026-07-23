using System.Security.Claims;
using DevRecall.Application.Identity;

namespace DevRecall.Api.Authentication;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor)
    : ICurrentUser
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
