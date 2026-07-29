using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Analytics;
using DevRecall.Contracts.Auth;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Domain.Reviews;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Api.Tests.Analytics;

[Collection(AuthApiTestSuite.Name)]
public sealed class ProgressOverviewEndpointsTests(AuthApiFactory factory)
{
    private static readonly DateTimeOffset From =
        new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = From.AddDays(7);

    [Fact]
    public async Task Get_ShouldRequireAuthentication()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(
            "/api/v1/analytics/progress-overview");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldRejectAnInvalidRange()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var client = auth.Client;

        using var response = await client.GetAsync(BuildUrl(To, From));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ShouldReturnZeroMetricsForAnEmptyRange()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var client = auth.Client;

        using var response = await client.GetAsync(BuildUrl(From, To));
        var result = await response.Content
            .ReadFromJsonAsync<ProgressOverviewResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().Be(new ProgressOverviewResponse(
            From, To, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
    }

    [Fact]
    public async Task Get_ShouldUseTheDefaultPreviousSevenDays()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var client = auth.Client;
        var before = DateTimeOffset.UtcNow;

        using var response = await client.GetAsync(
            "/api/v1/analytics/progress-overview");
        var after = DateTimeOffset.UtcNow;
        var result = await response.Content
            .ReadFromJsonAsync<ProgressOverviewResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.ToUtc.Should().BeOnOrAfter(before);
        result.ToUtc.Should().BeOnOrBefore(after);
        (result.ToUtc - result.FromUtc).Should().Be(TimeSpan.FromDays(7));
    }

    [Fact]
    public async Task Get_ShouldAggregateSessionsAndItemsWithinHalfOpenRange()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var client = auth.Client;
        await SeedStudyActivityAsync(auth.User.Id);

        using var response = await client.GetAsync(BuildUrl(From, To));
        var result = await response.Content
            .ReadFromJsonAsync<ProgressOverviewResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.StudyMinutes.Should().Be(60);
        result.CompletedSessions.Should().Be(1);
        result.CancelledSessions.Should().Be(1);
        result.StudyItemsCompleted.Should().Be(1);
        result.StudyItemsSkipped.Should().Be(1);
        result.InterviewItemsCompleted.Should().Be(1);
        result.KnowledgeItemsCompleted.Should().Be(0);
        result.ActiveStudyDays.Should().Be(1);
    }

    [Fact]
    public async Task Get_ShouldJoinChildActivityToTheOwningUserAndCountUtcDays()
    {
        var owner = await CreateAuthenticatedClientAsync();
        using var client = owner.Client;
        var other = await CreateAuthenticatedClientAsync();
        using var otherClient = other.Client;
        await SeedReviewAndDsaActivityAsync(owner.User.Id, other.User.Id);

        using var response = await client.GetAsync(BuildUrl(From, To));
        var result = await response.Content
            .ReadFromJsonAsync<ProgressOverviewResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.ReviewsCompleted.Should().Be(2);
        result.DsaAttempts.Should().Be(1);
        result.ActiveStudyDays.Should().Be(2);
    }

    private async Task SeedStudyActivityAsync(Guid userId)
    {
        var completed = StudySession.Create(
            Guid.NewGuid(), userId, "Completed", 60, null, From);
        var completedItem = completed.AddItem(
            Guid.NewGuid(), StudyResourceType.InterviewQuestion,
            Guid.NewGuid(), null, From);
        var skippedItem = completed.AddItem(
            Guid.NewGuid(), StudyResourceType.KnowledgeNode,
            Guid.NewGuid(), null, From);
        completed.Start(From.AddDays(1));
        completed.CompleteItem(
            completedItem.Id, null, From.AddDays(1).AddMinutes(20));
        completed.SkipItem(
            skippedItem.Id, null, From.AddDays(1).AddMinutes(30));
        completed.Complete(From.AddDays(1).AddMinutes(60));

        var cancelled = StudySession.Create(
            Guid.NewGuid(), userId, "Cancelled", 30, null, From);
        cancelled.Cancel(From.AddDays(2));

        var excluded = StudySession.Create(
            Guid.NewGuid(), userId, "At upper boundary", 15, null, From);
        excluded.Start(To.AddMinutes(-15));
        excluded.Complete(To);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<DevRecallDbContext>();
        context.StudySessions.AddRange(completed, cancelled, excluded);
        await context.SaveChangesAsync();
    }

    private async Task SeedReviewAndDsaActivityAsync(
        Guid ownerId, Guid otherUserId)
    {
        var ownerReview = CreateReview(ownerId, From.AddDays(1));
        var ownerReviewSameDay = CreateReview(ownerId, From.AddDays(1).AddHours(2));
        var otherReview = CreateReview(otherUserId, From.AddDays(2));
        var ownerProblem = CreateProblem(ownerId, "Owner problem");
        var otherProblem = CreateProblem(otherUserId, "Other problem");
        var ownerAttempt = CreateAttempt(
            ownerProblem.Id, From.AddDays(3));
        var otherAttempt = CreateAttempt(
            otherProblem.Id, From.AddDays(4));

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<DevRecallDbContext>();
        context.ReviewItems.AddRange(
            ownerReview.Item, ownerReviewSameDay.Item, otherReview.Item);
        context.ReviewHistories.AddRange(
            ownerReview.History, ownerReviewSameDay.History,
            otherReview.History);
        context.DsaProblems.AddRange(ownerProblem, otherProblem);
        context.DsaAttempts.AddRange(ownerAttempt, otherAttempt);
        await context.SaveChangesAsync();
    }

    private static (ReviewItem Item, ReviewHistory History) CreateReview(
        Guid userId, DateTimeOffset reviewedAt)
    {
        var item = ReviewItem.Create(
            Guid.NewGuid(), userId, ReviewResourceType.DsaProblem,
            Guid.NewGuid(), reviewedAt, reviewedAt.AddDays(-1));
        var schedule = item.Evaluate(
            ReviewEvaluation.Good, 0, reviewedAt);
        return (
            item,
            ReviewHistory.Create(
                Guid.NewGuid(), item.Id, ReviewEvaluation.Good,
                schedule, reviewedAt));
    }

    private static DsaProblem CreateProblem(Guid userId, string title) =>
        DsaProblem.Create(
            Guid.NewGuid(), userId, title, "Description",
            DsaProblemDifficulty.Easy, null, null, ["Array"], From);

    private static DsaAttempt CreateAttempt(
        Guid problemId, DateTimeOffset attemptedAt) =>
        DsaAttempt.Create(
            Guid.NewGuid(), problemId, 1, DsaAttemptResult.Solved,
            "C#", "return;", "Approach", "O(1)", "O(1)", 10, null,
            attemptedAt, attemptedAt);

    private async Task<(HttpClient Client, RegisterResponse User)>
        CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var email = $"analytics-{Guid.NewGuid():N}@example.com";
        using var register = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Analytics User", "Example123!"));
        var user = await register.Content
            .ReadFromJsonAsync<RegisterResponse>();
        register.EnsureSuccessStatusCode();
        using var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Example123!"));
        login.EnsureSuccessStatusCode();
        return (client, user!);
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static string BuildUrl(
        DateTimeOffset from, DateTimeOffset to) =>
        "/api/v1/analytics/progress-overview"
        + $"?fromUtc={Uri.EscapeDataString(from.ToString("O"))}"
        + $"&toUtc={Uri.EscapeDataString(to.ToString("O"))}";
}
