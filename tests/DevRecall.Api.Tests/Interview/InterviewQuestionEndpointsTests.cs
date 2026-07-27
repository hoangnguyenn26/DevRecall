using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Common;
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

    [Fact]
    public async Task ListAndDetail_ShouldUseCombinedFiltersAndReturnFullQuestion()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var expected = await CreateQuestionAsync(
            client, "IEnumerable vs IQueryable", "LINQ", "Medium");
        await CreateQuestionAsync(client, "Deferred execution", "LINQ", "Easy");
        await CreateQuestionAsync(
            client, "Middleware pipeline", "ASP.NET Core", "Medium");

        using var listResponse = await client.GetAsync(
            "/api/v1/interview-questions?topic=linq&difficulty=medium");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<
            PagedResponse<InterviewQuestionListItemResponse>>();
        using var detailResponse = await client.GetAsync(
            $"/api/v1/interview-questions/{expected.Id}");
        var detail = await detailResponse.Content
            .ReadFromJsonAsync<InterviewQuestionDetailResponse>();

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        list!.TotalCount.Should().Be(1);
        list.Items.Should().ContainSingle(item => item.Id == expected.Id);
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        detail!.Question.Should().Be("What is IEnumerable vs IQueryable?");
        detail.Notes.Should().Be("Interview notes.");
        detail.Status.Should().Be("Active");
    }

    [Fact]
    public async Task List_ShouldReturnOnlyCurrentUsersQuestions()
    {
        var firstSession = await CreateAuthenticatedClientAsync();
        using var firstClient = firstSession.Client;
        await CreateQuestionAsync(firstClient, "First user question", "LINQ", "Easy");
        var secondSession = await CreateAuthenticatedClientAsync();
        using var secondClient = secondSession.Client;
        await CreateQuestionAsync(
            secondClient, "Second user question", "LINQ", "Medium");

        using var response = await secondClient.GetAsync(
            "/api/v1/interview-questions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<
            PagedResponse<InterviewQuestionListItemResponse>>();

        result!.Items.Select(item => item.Title)
            .Should().Equal("Second user question");
    }

    [Fact]
    public async Task List_ShouldReturnCorrectPaginationMetadata()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        await CreateQuestionAsync(client, "Question A", "LINQ", "Easy");
        await CreateQuestionAsync(client, "Question B", "LINQ", "Medium");
        await CreateQuestionAsync(client, "Question C", "LINQ", "Hard");

        using var response = await client.GetAsync(
            "/api/v1/interview-questions?page=2&pageSize=2");
        var result = await response.Content.ReadFromJsonAsync<
            PagedResponse<InterviewQuestionListItemResponse>>();

        result!.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(2);
        result.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Detail_ForAnotherUsersQuestion_ShouldReturnNotFound()
    {
        var firstSession = await CreateAuthenticatedClientAsync();
        using var firstClient = firstSession.Client;
        var question = await CreateQuestionAsync(
            firstClient, "Owned question", "LINQ", "Medium");
        var secondSession = await CreateAuthenticatedClientAsync();
        using var secondClient = secondSession.Client;

        using var response = await secondClient.GetAsync(
            $"/api/v1/interview-questions/{question.Id}");
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("INTERVIEW_QUESTION_NOT_FOUND");
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?difficulty=Intermediate")]
    public async Task List_WithInvalidQuery_ShouldReturnValidationError(
        string query)
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;

        using var response = await client.GetAsync(
            $"/api/v1/interview-questions{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

    private static async Task<CreateInterviewQuestionResponse>
        CreateQuestionAsync(
            HttpClient client,
            string title,
            string topic,
            string difficulty)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/interview-questions",
            new CreateInterviewQuestionRequest(
                title,
                $"What is {title}?",
                topic,
                difficulty,
                "Interview notes."));
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<CreateInterviewQuestionResponse>())!;
    }
}
