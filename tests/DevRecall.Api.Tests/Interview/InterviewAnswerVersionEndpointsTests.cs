using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Application.Interview.Answers;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Interview;
using DevRecall.Contracts.Interview.Answers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Api.Tests.Interview;

[Collection(AuthApiTestSuite.Name)]
public sealed class InterviewAnswerVersionEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task DraftWorkflow_ShouldCreateUpdateAndPreserveNoOpTimestamp()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var question = await CreateQuestionAsync(client);

        using var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/interview-questions/{question.Id}/answer-versions",
            new CreateInterviewAnswerDraftRequest("  Initial answer.  "));
        var created = await createResponse.Content
            .ReadFromJsonAsync<InterviewAnswerVersionResponse>();
        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/interview-questions/{question.Id}/answer-versions/{created!.Id}",
            new UpdateInterviewAnswerDraftRequest("Improved answer."));
        var updated = await updateResponse.Content
            .ReadFromJsonAsync<InterviewAnswerVersionResponse>();
        using var noOpResponse = await client.PutAsJsonAsync(
            $"/api/v1/interview-questions/{question.Id}/answer-versions/{created.Id}",
            new UpdateInterviewAnswerDraftRequest("  Improved answer.  "));
        var noOp = await noOpResponse.Content
            .ReadFromJsonAsync<InterviewAnswerVersionResponse>();

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().Be(
            $"/api/v1/interview-questions/{question.Id}/answer-versions/{created.Id}");
        created.VersionNumber.Should().Be(1);
        created.Content.Should().Be("Initial answer.");
        created.Status.Should().Be("Draft");
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updated!.Content.Should().Be("Improved answer.");
        updated.UpdatedAtUtc.Should().BeAfter(created.UpdatedAtUtc);
        noOpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        noOp!.UpdatedAtUtc.Should().Be(TruncateToMicroseconds(
            updated.UpdatedAtUtc));
    }

    [Fact]
    public async Task CreateSecondDraft_ShouldReturnDraftAlreadyExists()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var question = await CreateQuestionAsync(client);
        await CreateDraftAsync(client, question.Id);

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/interview-questions/{question.Id}/answer-versions",
            new CreateInterviewAnswerDraftRequest("Second draft."));

        await AssertErrorAsync(
            response, HttpStatusCode.Conflict,
            "INTERVIEW_ANSWER_DRAFT_ALREADY_EXISTS");
    }

    [Fact]
    public async Task CreateDraft_ForArchivedQuestion_ShouldReturnConflict()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var question = await CreateQuestionAsync(client);
        using var archiveResponse = await client.PostAsync(
            $"/api/v1/interview-questions/{question.Id}/archive", null);
        archiveResponse.EnsureSuccessStatusCode();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/interview-questions/{question.Id}/answer-versions",
            new CreateInterviewAnswerDraftRequest("Answer."));

        await AssertErrorAsync(
            response, HttpStatusCode.Conflict, "INTERVIEW_QUESTION_ARCHIVED");
    }

    [Fact]
    public async Task CreateAndUpdateDraft_CrossUser_ShouldReturnQuestionNotFound()
    {
        using var owner = await CreateAuthenticatedClientAsync();
        var question = await CreateQuestionAsync(owner);
        var answer = await CreateDraftAsync(owner, question.Id);
        using var otherUser = await CreateAuthenticatedClientAsync();

        using var createResponse = await otherUser.PostAsJsonAsync(
            $"/api/v1/interview-questions/{question.Id}/answer-versions",
            new CreateInterviewAnswerDraftRequest("Foreign answer."));
        using var updateResponse = await otherUser.PutAsJsonAsync(
            $"/api/v1/interview-questions/{question.Id}/answer-versions/{answer.Id}",
            new UpdateInterviewAnswerDraftRequest("Foreign update."));

        await AssertErrorAsync(
            createResponse, HttpStatusCode.NotFound,
            "INTERVIEW_QUESTION_NOT_FOUND");
        await AssertErrorAsync(
            updateResponse, HttpStatusCode.NotFound,
            "INTERVIEW_QUESTION_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateDraft_WithWrongQuestionOrEmptyContent_ShouldFail()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var firstQuestion = await CreateQuestionAsync(client, "First");
        var secondQuestion = await CreateQuestionAsync(client, "Second");
        var answer = await CreateDraftAsync(client, secondQuestion.Id);

        using var wrongQuestionResponse = await client.PutAsJsonAsync(
            $"/api/v1/interview-questions/{firstQuestion.Id}/answer-versions/{answer.Id}",
            new UpdateInterviewAnswerDraftRequest("Update."));
        using var invalidResponse = await client.PutAsJsonAsync(
            $"/api/v1/interview-questions/{secondQuestion.Id}/answer-versions/{answer.Id}",
            new UpdateInterviewAnswerDraftRequest(" "));

        await AssertErrorAsync(
            wrongQuestionResponse, HttpStatusCode.NotFound,
            "INTERVIEW_ANSWER_VERSION_NOT_FOUND");
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDraft_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/interview-questions/{Guid.NewGuid()}/answer-versions",
            new CreateInterviewAnswerDraftRequest("Answer."));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublishWorkflow_ShouldKeepHistoryAndSelectNewestPublished()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var question = await CreateQuestionAsync(client);
        var first = await CreateDraftAsync(client, question.Id);
        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/interview-questions/{question.Id}/answer-versions/{first.Id}",
            new UpdateInterviewAnswerDraftRequest("Stable v1."));
        updateResponse.EnsureSuccessStatusCode();

        var publishedFirst = await PublishAsync(client, question.Id, first.Id);
        using var immutableResponse = await client.PutAsJsonAsync(
            $"/api/v1/interview-questions/{question.Id}/answer-versions/{first.Id}",
            new UpdateInterviewAnswerDraftRequest("Forbidden."));
        var repeatedPublish = await PublishAsync(client, question.Id, first.Id);
        var second = await CreateDraftAsync(client, question.Id);
        var publishedSecond = await PublishAsync(
            client, question.Id, second.Id);

        publishedFirst.Status.Should().Be("Published");
        publishedFirst.PublishedAtUtc.Should().NotBeNull();
        await AssertErrorAsync(
            immutableResponse, HttpStatusCode.Conflict,
            "INTERVIEW_ANSWER_VERSION_PUBLISHED");
        repeatedPublish.PublishedAtUtc.Should().Be(
            TruncateToMicroseconds(publishedFirst.PublishedAtUtc!.Value));
        repeatedPublish.UpdatedAtUtc.Should().Be(
            TruncateToMicroseconds(publishedFirst.UpdatedAtUtc));
        second.VersionNumber.Should().Be(2);
        publishedSecond.Status.Should().Be("Published");

        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider
            .GetRequiredService<IInterviewAnswerVersionRepository>();
        var current = await repository.GetCurrentPublishedAsync(
            question.Id, CancellationToken.None);
        current!.Id.Should().Be(second.Id);
        current.VersionNumber.Should().Be(2);
    }

    [Fact]
    public async Task Publish_ForArchivedQuestion_ShouldReturnConflict()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var question = await CreateQuestionAsync(client);
        var answer = await CreateDraftAsync(client, question.Id);
        using var archiveResponse = await client.PostAsync(
            $"/api/v1/interview-questions/{question.Id}/archive", null);
        archiveResponse.EnsureSuccessStatusCode();

        using var response = await client.PostAsync(
            $"/api/v1/interview-questions/{question.Id}/answer-versions/{answer.Id}/publish",
            null);

        await AssertErrorAsync(
            response, HttpStatusCode.Conflict, "INTERVIEW_QUESTION_ARCHIVED");
    }

    [Fact]
    public async Task Publish_WithWrongQuestionVersion_ShouldReturnNotFound()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var firstQuestion = await CreateQuestionAsync(client, "First");
        var secondQuestion = await CreateQuestionAsync(client, "Second");
        var answer = await CreateDraftAsync(client, secondQuestion.Id);

        using var response = await client.PostAsync(
            $"/api/v1/interview-questions/{firstQuestion.Id}/answer-versions/{answer.Id}/publish",
            null);

        await AssertErrorAsync(
            response, HttpStatusCode.NotFound,
            "INTERVIEW_ANSWER_VERSION_NOT_FOUND");
    }

    [Fact]
    public async Task Publish_CrossUser_ShouldReturnQuestionNotFound()
    {
        using var owner = await CreateAuthenticatedClientAsync();
        var question = await CreateQuestionAsync(owner);
        var answer = await CreateDraftAsync(owner, question.Id);
        using var otherUser = await CreateAuthenticatedClientAsync();

        using var response = await otherUser.PostAsync(
            $"/api/v1/interview-questions/{question.Id}/answer-versions/{answer.Id}/publish",
            null);

        await AssertErrorAsync(
            response, HttpStatusCode.NotFound,
            "INTERVIEW_QUESTION_NOT_FOUND");
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var email = $"answer-{Guid.NewGuid():N}@example.com";
        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Answer User", "Example123!"));
        registerResponse.EnsureSuccessStatusCode();
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Example123!"));
        loginResponse.EnsureSuccessStatusCode();
        return client;
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static async Task<CreateInterviewQuestionResponse>
        CreateQuestionAsync(HttpClient client, string title = "Question")
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/interview-questions",
            new CreateInterviewQuestionRequest(
                title, $"{title} text?", "LINQ", "Medium", null));
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<CreateInterviewQuestionResponse>())!;
    }

    private static async Task<InterviewAnswerVersionResponse> CreateDraftAsync(
        HttpClient client,
        Guid questionId)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/interview-questions/{questionId}/answer-versions",
            new CreateInterviewAnswerDraftRequest("Draft answer."));
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<InterviewAnswerVersionResponse>())!;
    }

    private static async Task<InterviewAnswerVersionResponse> PublishAsync(
        HttpClient client,
        Guid questionId,
        Guid versionId)
    {
        using var response = await client.PostAsync(
            $"/api/v1/interview-questions/{questionId}/answer-versions/{versionId}/publish",
            null);
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<InterviewAnswerVersionResponse>())!;
    }

    private static async Task AssertErrorAsync(
        HttpResponseMessage response,
        HttpStatusCode statusCode,
        string errorCode)
    {
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        response.StatusCode.Should().Be(statusCode);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be(errorCode);
    }

    private static DateTimeOffset TruncateToMicroseconds(
        DateTimeOffset value) =>
        new(value.Ticks - value.Ticks % 10, value.Offset);
}
