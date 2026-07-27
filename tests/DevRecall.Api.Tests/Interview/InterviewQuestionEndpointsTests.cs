using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Interview;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Api.Tests.Interview;

[Collection(AuthApiTestSuite.Name)]
public sealed class InterviewQuestionEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task Create_ShouldNormalizeAndPersistForCurrentUser()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;

        using var response = await client.PostAsJsonAsync(
            "/api/v1/interview-questions",
            new CreateInterviewQuestionRequest(
                "  IEnumerable   vs IQueryable ",
                " What is the difference? ",
                " LINQ ",
                "medium",
                " Focus on execution location. "));
        var result = await response.Content
            .ReadFromJsonAsync<CreateInterviewQuestionResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should()
            .Be($"/api/v1/interview-questions/{result!.Id}");
        result.Title.Should().Be("IEnumerable vs IQueryable");
        result.Topic.Should().Be("LINQ");
        result.Difficulty.Should().Be("Medium");
        result.Status.Should().Be("Active");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        var ownerId = await context.InterviewQuestions
            .Where(question => question.Id == result.Id)
            .Select(question => question.UserId)
            .SingleAsync();
        ownerId.Should().Be(session.User.Id);
    }

    [Fact]
    public async Task Create_WithInvalidDifficulty_ShouldReturnValidationError()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;

        using var response = await client.PostAsJsonAsync(
            "/api/v1/interview-questions",
            ValidRequest(difficulty: "Intermediate"));
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        document.RootElement.GetProperty("errors")
            .GetProperty("difficulty")[0].GetString()
            .Should().Be("Difficulty must be Easy, Medium, or Hard.");
    }

    [Fact]
    public async Task Create_WithMissingRequiredFields_ShouldReturnValidationErrors()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;

        using var response = await client.PostAsJsonAsync(
            "/api/v1/interview-questions",
            new CreateInterviewQuestionRequest(
                " ", "", null!, "Medium", null));
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        var errors = document.RootElement.GetProperty("errors");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        errors.TryGetProperty("title", out _).Should().BeTrue();
        errors.TryGetProperty("question", out _).Should().BeTrue();
        errors.TryGetProperty("topic", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/interview-questions",
            ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(HttpClient Client, RegisterResponse User)>
        CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var email = $"interview-{Guid.NewGuid():N}@example.com";
        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Interview User", "Example123!"));
        var user = await registerResponse.Content
            .ReadFromJsonAsync<RegisterResponse>();
        registerResponse.EnsureSuccessStatusCode();
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Example123!"));
        loginResponse.EnsureSuccessStatusCode();
        return (client, user!);
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static CreateInterviewQuestionRequest ValidRequest(
        string difficulty = "Medium") =>
        new(
            "Dependency Injection",
            "What is dependency injection?",
            "Software Design",
            difficulty,
            null);
}
