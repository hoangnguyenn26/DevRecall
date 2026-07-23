using DevRecall.Application.Identity.Register;
using DevRecall.Contracts.Auth;

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
}
