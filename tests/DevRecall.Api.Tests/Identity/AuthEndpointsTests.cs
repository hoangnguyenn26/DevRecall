using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevRecall.Api.Tests.Identity;

[Collection(AuthApiTestSuite.Name)]
public sealed class AuthEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task Register_ShouldReturnCreatedAndAuthenticateTheNewUser()
    {
        using var client = CreateClient();
        var email = CreateUniqueEmail();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Hoang Nguyen", "Example123!"));
        var result = await response.Content
            .ReadFromJsonAsync<RegisterResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);

        using var meResponse = await client.GetAsync("/api/v1/auth/me");
        var currentUser = await meResponse.Content
            .ReadFromJsonAsync<CurrentUserResponse>();
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        currentUser!.Email.Should().Be(email);
    }

    [Fact]
    public async Task Register_ShouldReturnConflictForDuplicateEmail()
    {
        using var client = CreateClient();
        var email = CreateUniqueEmail();
        var request = new RegisterRequest(
            email,
            "Hoang Nguyen",
            "Example123!");

        using var firstResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            request);
        using var duplicateResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            request with { Email = email.ToUpperInvariant() });

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_ShouldReturnValidationProblemForInvalidRequest()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("", "", "123"));
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorizedForInvalidCredentials()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("missing@example.com", "WrongPassword"));
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("IDENTITY_INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task InvalidLogin_ShouldEchoCorrelationIdInHeaderAndProblemDetails()
    {
        const string correlationId = "auth-review-001";
        using var client = CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/auth/login")
        {
            Content = JsonContent.Create(
                new LoginRequest("missing@example.com", "WrongPassword"))
        };
        request.Headers.Add("X-Correlation-ID", correlationId);

        using var response = await client.SendAsync(request);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.GetValues("X-Correlation-ID")
            .Should().ContainSingle(correlationId);
        document.RootElement.GetProperty("traceId").GetString()
            .Should().Be(correlationId);
    }

    [Fact]
    public async Task Me_ShouldReturnUnauthorizedWithoutRedirectWhenAnonymous()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAndLogin_ShouldBeAccessibleAnonymously()
    {
        using var client = CreateClient();
        var email = CreateUniqueEmail();

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Hoang Nguyen", "Example123!"));
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Example123!"));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthFlow_ShouldRegisterGetMeAndLogout()
    {
        using var client = CreateClient();
        var email = CreateUniqueEmail();

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Hoang Nguyen", "Example123!"));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var meResponse = await client.GetAsync("/api/v1/auth/me");
        var currentUser = await meResponse.Content
            .ReadFromJsonAsync<CurrentUserResponse>();
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        currentUser.Should().NotBeNull();
        currentUser!.Email.Should().Be(email);

        using var logoutResponse = await client.PostAsync(
            "/api/v1/auth/logout",
            null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var meAfterLogout = await client.GetAsync("/api/v1/auth/me");
        meAfterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    private static string CreateUniqueEmail()
    {
        return $"user-{Guid.NewGuid():N}@example.com";
    }
}
