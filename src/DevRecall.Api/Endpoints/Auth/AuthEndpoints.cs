using DevRecall.Api.Authentication;
using DevRecall.Api.Authorization;
using DevRecall.Application.Identity.GetCurrentUser;
using DevRecall.Application.Identity.Login;
using DevRecall.Application.Identity.Register;
using DevRecall.Contracts.Auth;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace DevRecall.Api.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .RequireRateLimiting(RateLimitingPolicies.Authentication);
        group.MapPost("/login", LoginAsync)
            .RequireRateLimiting(RateLimitingPolicies.Authentication);
        group.MapGet("/csrf-token", (IAntiforgery antiforgery, HttpContext context) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new CsrfTokenResponse(
                tokens.RequestToken!, tokens.HeaderName ?? "X-CSRF-TOKEN"));
        }).WithName("GetCsrfToken").Produces<CsrfTokenResponse>();
        group.MapGet("/me", GetCurrentUserAsync)
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);
        group.MapPost(
                "/logout",
                (Func<HttpContext, Task<IResult>>)LogoutAsync)
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        RegisterUserHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RegisterUserCommand(
                request.Email,
                request.DisplayName,
                request.Password),
            cancellationToken);
        var response = new RegisterResponse(
            result.Id,
            result.Email,
            result.DisplayName);
        var principal = ClaimsPrincipalFactory.Create(
            result.Id,
            result.Email,
            result.DisplayName);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        return Results.Created(
            $"/api/v1/users/{result.Id}",
            response);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        LoginUserHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new LoginUserCommand(request.Email, request.Password),
            cancellationToken);
        var principal = ClaimsPrincipalFactory.Create(
            result.Id,
            result.Email,
            result.DisplayName);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        return Results.Ok(new LoginResponse(
            result.Id,
            result.Email,
            result.DisplayName));
    }

    private static async Task<IResult> GetCurrentUserAsync(
        GetCurrentUserHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        return Results.Ok(new CurrentUserResponse(
            result.Id,
            result.Email,
            result.DisplayName));
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.NoContent();
    }
}
