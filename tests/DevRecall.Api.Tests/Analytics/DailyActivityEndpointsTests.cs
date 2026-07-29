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
public sealed class DailyActivityEndpointsTests(AuthApiFactory factory)
{
    private static readonly DateTimeOffset From =
        new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To =
        new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Get_ShouldRequireAuthentication()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(
            "/api/v1/analytics/daily-activity");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_EmptyRange_ShouldReturnEveryUtcDateWithZeros()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var client = auth.Client;

        using var response = await client.GetAsync(BuildUrl());
        var result = await response.Content
            .ReadFromJsonAsync<DailyActivityResponse>();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            await response.Content.ReadAsStringAsync());
        result!.Days.Select(day => day.Date).Should().Equal(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 2),
            new DateOnly(2026, 8, 3));
        result.Days.Should().OnlyContain(day =>
            day.StudyMinutes == 0
            && day.CompletedSessions == 0
            && day.CompletedStudyItems == 0
            && day.Reviews == 0
            && day.DsaAttempts == 0);
    }

    [Fact]
    public async Task Get_ShouldAggregateByUtcDateAndEnforceRangeAndOwnership()
    {
        var owner = await CreateAuthenticatedClientAsync();
        using var client = owner.Client;
        var other = await CreateAuthenticatedClientAsync();
        using var otherClient = other.Client;
        await SeedActivityAsync(owner.User.Id, other.User.Id);

        using var response = await client.GetAsync(BuildUrl());
        var result = await response.Content
            .ReadFromJsonAsync<DailyActivityResponse>();

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            await response.Content.ReadAsStringAsync());
        result!.Days.Should().BeInAscendingOrder(day => day.Date);
        result.Days[0].Should().Be(new DailyActivityDayResponse(
            new DateOnly(2026, 8, 1), 30, 1, 1, 2, 1));
        result.Days[1].Should().Be(new DailyActivityDayResponse(
            new DateOnly(2026, 8, 2), 45, 1, 0, 0, 0));
        result.Days[2].Should().Be(new DailyActivityDayResponse(
            new DateOnly(2026, 8, 3), 0, 0, 0, 0, 0));
    }

    private async Task SeedActivityAsync(Guid ownerId, Guid otherUserId)
    {
        var first = CreateCompletedSession(
            ownerId, From, From.AddMinutes(30),
            StudyResourceType.KnowledgeNode);
        var second = CreateCompletedSession(
            ownerId, From.AddDays(1), From.AddDays(1).AddMinutes(45));
        var before = CreateCompletedSession(
            ownerId, From.AddHours(-1), From.AddMinutes(-1));
        var atUpperBoundary = CreateCompletedSession(
            ownerId, To.AddMinutes(-30), To);
        var other = CreateCompletedSession(
            otherUserId, From, From.AddMinutes(90),
            StudyResourceType.InterviewQuestion);
        var ownerReviews = new[]
        {
            CreateReview(ownerId, From.AddHours(1)),
            CreateReview(ownerId, From.AddHours(2))
        };
        var otherReview = CreateReview(otherUserId, From.AddHours(3));
        var ownerProblem = CreateProblem(ownerId, "Owner problem");
        var otherProblem = CreateProblem(otherUserId, "Other problem");
        var ownerAttempt = CreateAttempt(ownerProblem.Id, From.AddHours(4));
        var otherAttempt = CreateAttempt(otherProblem.Id, From.AddHours(5));

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<DevRecallDbContext>();
        context.StudySessions.AddRange(
            first, second, before, atUpperBoundary, other);
        context.ReviewItems.AddRange(
            ownerReviews[0].Item, ownerReviews[1].Item, otherReview.Item);
        context.ReviewHistories.AddRange(
            ownerReviews[0].History, ownerReviews[1].History,
            otherReview.History);
        context.DsaProblems.AddRange(ownerProblem, otherProblem);
        context.DsaAttempts.AddRange(ownerAttempt, otherAttempt);
        await context.SaveChangesAsync();
    }

    private static StudySession CreateCompletedSession(
        Guid userId, DateTimeOffset startedAt, DateTimeOffset completedAt,
        StudyResourceType? itemType = null)
    {
        var session = StudySession.Create(
            Guid.NewGuid(), userId, "Analytics session", 30, null,
            startedAt.AddMinutes(-1));
        StudySessionItem? item = null;
        if (itemType is not null)
        {
            item = session.AddItem(
                Guid.NewGuid(), itemType.Value, Guid.NewGuid(), null,
                startedAt.AddMinutes(-1));
        }

        session.Start(startedAt);
        if (item is not null)
        {
            session.CompleteItem(item.Id, null, startedAt);
        }

        session.Complete(completedAt);
        return session;
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
        var email = $"daily-{Guid.NewGuid():N}@example.com";
        using var register = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Daily User", "Example123!"));
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

    private static string BuildUrl() =>
        "/api/v1/analytics/daily-activity"
        + $"?fromUtc={Uri.EscapeDataString(From.ToString("O"))}"
        + $"&toUtc={Uri.EscapeDataString(To.ToString("O"))}";
}
