using DevRecall.Api.Authentication;
using DevRecall.Application.Identity.Login;
using DevRecall.Application.Identity.Register;
using DevRecall.Contracts.Auth;
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

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        RegisterUserHandler handler,
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
}
