using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Interview;
using DevRecall.Contracts.Interview.Answers;
using DevRecall.Contracts.Interview.Practice;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevRecall.Api.Tests.Interview;

[Collection(AuthApiTestSuite.Name)]
public sealed class InterviewPracticeEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task PracticeRead_ShouldBeOwnerScopedAndReturnPublishedReference()
    {
        using var owner = await CreateClientAsync();
        var question = await CreateQuestionAsync(owner);
        var draft = await PostAsync<InterviewAnswerVersionResponse>(owner,
            $"/api/v1/interview-questions/{question.Id}/answer-versions",
            new CreateInterviewAnswerDraftRequest("Reference answer."));
        using var publish = await owner.PostAsync(
            $"/api/v1/interview-questions/{question.Id}/answer-versions/{draft.Id}/publish", null);
        publish.EnsureSuccessStatusCode();

        var practice = await owner.GetFromJsonAsync<GetInterviewPracticeResponse>(
            $"/api/v1/interview-questions/{question.Id}/practice");
        practice!.ReferenceAnswer!.Content.Should().Be("Reference answer.");

        using var other = await CreateClientAsync();
        using var forbidden = await other.GetAsync($"/api/v1/interview-questions/{question.Id}/practice");
        forbidden.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var forbiddenHistory = await other.GetAsync($"/api/v1/interview-questions/{question.Id}/attempts");
        forbiddenHistory.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Complete_ShouldRejectNumericRatingAndBeIdempotent()
    {
        using var client = await CreateClientAsync();
        var question = await CreateQuestionAsync(client);
        var submissionId = Guid.NewGuid();
        var started = DateTimeOffset.UtcNow.AddMinutes(-2);
        using var invalid = await client.PostAsJsonAsync(
            $"/api/v1/interview-questions/{question.Id}/attempts",
            new CompleteInterviewPracticeRequest("My answer", "1", [], started, Guid.NewGuid()));
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var request = new CompleteInterviewPracticeRequest("My answer", "Good", [], started, submissionId);
        var first = await PostAsync<CompleteInterviewPracticeResponse>(client,
            $"/api/v1/interview-questions/{question.Id}/attempts", request);
        var retry = await PostAsync<CompleteInterviewPracticeResponse>(client,
            $"/api/v1/interview-questions/{question.Id}/attempts", request);
        retry.AttemptId.Should().Be(first.AttemptId);
        retry.CompletedAtUtc.Should().Be(first.CompletedAtUtc);
        var history = await client.GetFromJsonAsync<GetInterviewPracticeHistoryResponse>(
            $"/api/v1/interview-questions/{question.Id}/attempts");
        history!.PageSize.Should().Be(10);
        history.Items.Should().ContainSingle(item => item.AttemptId == first.AttemptId);
        var detail = await client.GetFromJsonAsync<GetInterviewPracticeAttemptResponse>(
            $"/api/v1/interview-questions/{question.Id}/attempts/{first.AttemptId}");
        detail!.QuestionSnapshot.Should().Be("Explain IQueryable.");
        detail.AnswerSnapshot.Should().Be("My answer");
        detail.SelfRating.Should().Be("Good");

        using var other = await CreateClientAsync();
        using var hidden = await other.GetAsync(
            $"/api/v1/interview-questions/{question.Id}/attempts/{first.AttemptId}");
        hidden.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await hidden.Content.ReadAsStringAsync()).Should().Contain("INTERVIEW_ATTEMPT_NOT_FOUND");

        using var unbounded = await client.GetAsync(
            $"/api/v1/interview-questions/{question.Id}/attempts?pageSize=51");
        unbounded.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<HttpClient> CreateClientAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
        var email = $"practice-{Guid.NewGuid():N}@example.com";
        (await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "Practice User", "Example123!"))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Example123!"))).EnsureSuccessStatusCode();
        return client;
    }

    private static Task<CreateInterviewQuestionResponse> CreateQuestionAsync(HttpClient client) =>
        PostAsync<CreateInterviewQuestionResponse>(client, "/api/v1/interview-questions",
            new CreateInterviewQuestionRequest("IQueryable", "Explain IQueryable.", "LINQ", "Medium", null));

    private static async Task<T> PostAsync<T>(HttpClient client, string path, object body)
    {
        using var response = await client.PostAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
