using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Interview;
using DevRecall.Contracts.Interview.FollowUps;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevRecall.Api.Tests.Interview;

[Collection(AuthApiTestSuite.Name)]
public sealed class InterviewFollowUpEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task Lifecycle_ShouldCreateUpdateReorderArchiveAndNormalize()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var question = await CreateQuestionAsync(client);
        var first = await CreateFollowUpAsync(client, question.Id, "First");
        var second = await CreateFollowUpAsync(client, question.Id, "Second");
        var third = await CreateFollowUpAsync(client, question.Id, "Third");

        using var updateResponse = await client.PutAsJsonAsync(
            FollowUpPath(question.Id, second.Id),
            new UpdateInterviewFollowUpRequest("  Updated   second  "));
        var updated = await updateResponse.Content
            .ReadFromJsonAsync<InterviewFollowUpResponse>();
        using var reorderResponse = await client.PutAsJsonAsync(
            $"{FollowUpPath(question.Id, third.Id)}/order",
            new ChangeInterviewFollowUpOrderRequest(0));
        var reordered = await GetFollowUpsAsync(client, question.Id);
        using var archiveResponse = await client.PostAsync(
            $"{FollowUpPath(question.Id, second.Id)}/archive", null);
        using var secondArchiveResponse = await client.PostAsync(
            $"{FollowUpPath(question.Id, second.Id)}/archive", null);
        var active = await GetFollowUpsAsync(client, question.Id);

        first.SortOrder.Should().Be(0);
        second.SortOrder.Should().Be(1);
        third.SortOrder.Should().Be(2);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updated!.Prompt.Should().Be("Updated second");
        reorderResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        reordered.Select(item => item.Id).Should()
            .Equal(third.Id, first.Id, second.Id);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        secondArchiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        active.Select(item => item.Id).Should().Equal(third.Id, first.Id);
        active.Select(item => item.SortOrder).Should().Equal(0, 1);
    }

    [Fact]
    public async Task UpdateAndReorderNoOp_ShouldPreserveTimestamp()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var question = await CreateQuestionAsync(client);
        var followUp = await CreateFollowUpAsync(
            client, question.Id, "What happens?");

        using var updateResponse = await client.PutAsJsonAsync(
            FollowUpPath(question.Id, followUp.Id),
            new UpdateInterviewFollowUpRequest("  What   happens?  "));
        var updated = await updateResponse.Content
            .ReadFromJsonAsync<InterviewFollowUpResponse>();
        using var reorderResponse = await client.PutAsJsonAsync(
            $"{FollowUpPath(question.Id, followUp.Id)}/order",
            new ChangeInterviewFollowUpOrderRequest(0));
        var listed = await GetFollowUpsAsync(client, question.Id);

        updated!.UpdatedAtUtc.Should().Be(
            TruncateToMicroseconds(followUp.UpdatedAtUtc));
        reorderResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        listed.Single().UpdatedAtUtc.Should().Be(updated.UpdatedAtUtc);
    }

    [Fact]
    public async Task InvalidOrderAndPrompt_ShouldReturnValidationErrors()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var question = await CreateQuestionAsync(client);
        var followUp = await CreateFollowUpAsync(client, question.Id, "Prompt");

        using var promptResponse = await client.PostAsJsonAsync(
            $"/api/v1/interview-questions/{question.Id}/follow-ups",
            new CreateInterviewFollowUpRequest(" "));
        using var orderResponse = await client.PutAsJsonAsync(
            $"{FollowUpPath(question.Id, followUp.Id)}/order",
            new ChangeInterviewFollowUpOrderRequest(1));

        promptResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        orderResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Mutations_ForArchivedQuestion_ShouldReturnConflict()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var question = await CreateQuestionAsync(client);
        var followUp = await CreateFollowUpAsync(client, question.Id, "Prompt");
        using var archiveQuestionResponse = await client.PostAsync(
            $"/api/v1/interview-questions/{question.Id}/archive", null);
        archiveQuestionResponse.EnsureSuccessStatusCode();

        using var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/interview-questions/{question.Id}/follow-ups",
            new CreateInterviewFollowUpRequest("New"));
        using var updateResponse = await client.PutAsJsonAsync(
            FollowUpPath(question.Id, followUp.Id),
            new UpdateInterviewFollowUpRequest("Updated"));

        await AssertErrorAsync(
            createResponse, HttpStatusCode.Conflict,
            "INTERVIEW_QUESTION_ARCHIVED");
        await AssertErrorAsync(
            updateResponse, HttpStatusCode.Conflict,
            "INTERVIEW_QUESTION_ARCHIVED");
    }

    [Fact]
    public async Task CrossUserAndWrongQuestionFollowUp_ShouldReturnNotFound()
    {
        using var owner = await CreateAuthenticatedClientAsync();
        var firstQuestion = await CreateQuestionAsync(owner, "First");
        var secondQuestion = await CreateQuestionAsync(owner, "Second");
        var followUp = await CreateFollowUpAsync(
            owner, secondQuestion.Id, "Prompt");
        using var otherUser = await CreateAuthenticatedClientAsync();

        using var crossUserResponse = await otherUser.GetAsync(
            $"/api/v1/interview-questions/{firstQuestion.Id}/follow-ups");
        using var wrongCombinationResponse = await owner.PutAsJsonAsync(
            FollowUpPath(firstQuestion.Id, followUp.Id),
            new UpdateInterviewFollowUpRequest("Updated"));

        await AssertErrorAsync(
            crossUserResponse, HttpStatusCode.NotFound,
            "INTERVIEW_QUESTION_NOT_FOUND");
        await AssertErrorAsync(
            wrongCombinationResponse, HttpStatusCode.NotFound,
            "INTERVIEW_FOLLOW_UP_NOT_FOUND");
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/interview-questions/{Guid.NewGuid()}/follow-ups",
            new CreateInterviewFollowUpRequest("Prompt"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var email = $"follow-up-{Guid.NewGuid():N}@example.com";
        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Follow-up User", "Example123!"));
        registerResponse.EnsureSuccessStatusCode();
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(email, "Example123!"));
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
                title, $"{title} text?", "C#", "Medium", null));
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<CreateInterviewQuestionResponse>())!;
    }

    private static async Task<InterviewFollowUpResponse> CreateFollowUpAsync(
        HttpClient client, Guid questionId, string prompt)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/interview-questions/{questionId}/follow-ups",
            new CreateInterviewFollowUpRequest(prompt));
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<InterviewFollowUpResponse>())!;
    }

    private static async Task<IReadOnlyList<InterviewFollowUpResponse>>
        GetFollowUpsAsync(HttpClient client, Guid questionId)
    {
        using var response = await client.GetAsync(
            $"/api/v1/interview-questions/{questionId}/follow-ups");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<
            IReadOnlyList<InterviewFollowUpResponse>>())!;
    }

    private static string FollowUpPath(Guid questionId, Guid followUpId) =>
        $"/api/v1/interview-questions/{questionId}/follow-ups/{followUpId}";

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
