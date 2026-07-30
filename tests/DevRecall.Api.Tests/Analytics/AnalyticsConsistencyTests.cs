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
public sealed class AnalyticsConsistencyTests(AuthApiFactory factory)
{
    private static readonly DateTimeOffset From =
        new(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = From.AddDays(4);

    [Fact]
    public async Task Endpoints_ShouldReportConsistentOwnerScopedMetrics()
    {
        var owner = await AuthenticateAsync();
        using var client = owner.Client;
        var other = await AuthenticateAsync();
        using var otherClient = other.Client;
        await SeedAsync(owner.User.Id, other.User.Id);

        var overview = await GetAsync<ProgressOverviewResponse>(
            client, "progress-overview");
        var daily = await GetAsync<DailyActivityResponse>(
            client, "daily-activity");
        var modules = await GetAsync<ModuleBreakdownResponse>(
            client, "module-breakdown");
        var reviews = await GetAsync<ReviewPerformanceResponse>(
            client, "review-performance");
        var dsa = await GetAsync<DsaPerformanceResponse>(
            client, "dsa-performance");

        overview.StudyMinutes.Should().Be(
            daily.Days.Sum(day => day.StudyMinutes));
        overview.CompletedSessions.Should().Be(
            daily.Days.Sum(day => day.CompletedSessions));
        overview.StudyItemsCompleted.Should().Be(
            daily.Days.Sum(day => day.CompletedStudyItems));
        overview.StudyItemsCompleted.Should().Be(
            modules.TotalCompletedItems);
        overview.ReviewsCompleted.Should().Be(reviews.TotalReviews);
        overview.ReviewsCompleted.Should().Be(
            daily.Days.Sum(day => day.Reviews));
        overview.DsaAttempts.Should().Be(dsa.TotalAttempts);
        overview.DsaAttempts.Should().Be(
            daily.Days.Sum(day => day.DsaAttempts));
        overview.StudyMinutes.Should().Be(30);
        overview.ReviewsCompleted.Should().Be(1);
        overview.DsaAttempts.Should().Be(1);
        daily.Days.Should().BeInAscendingOrder(day => day.Date);
        daily.Days.Select(day => day.Date).Should().OnlyHaveUniqueItems();
        daily.Days.Should().HaveCount(4);
    }

    private async Task SeedAsync(Guid ownerId, Guid otherId)
    {
        var ownerSession = CreateSession(ownerId, 30);
        var otherSession = CreateSession(otherId, 90);
        var ownerReview = CreateReview(ownerId);
        var otherReview = CreateReview(otherId);
        var ownerProblem = CreateProblem(ownerId, "Owner");
        var otherProblem = CreateProblem(otherId, "Other");
        var ownerAttempt = CreateAttempt(ownerProblem.Id);
        var otherAttempt = CreateAttempt(otherProblem.Id);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        db.StudySessions.AddRange(ownerSession, otherSession);
        db.ReviewItems.AddRange(ownerReview.Item, otherReview.Item);
        db.ReviewHistories.AddRange(
            ownerReview.History, otherReview.History);
        db.DsaProblems.AddRange(ownerProblem, otherProblem);
        db.DsaAttempts.AddRange(ownerAttempt, otherAttempt);
        await db.SaveChangesAsync();
    }

    private static StudySession CreateSession(Guid userId, int minutes)
    {
        var session = StudySession.Create(
            Guid.NewGuid(), userId, "Consistency", minutes, null, From);
        var item = session.AddItem(
            Guid.NewGuid(), StudyResourceType.KnowledgeNode,
            Guid.NewGuid(), null, From);
        session.Start(From);
        session.CompleteItem(item.Id, null, From.AddMinutes(1));
        session.Complete(From.AddMinutes(minutes));
        return session;
    }

    private static (ReviewItem Item, ReviewHistory History) CreateReview(
        Guid userId)
    {
        var item = ReviewItem.Create(
            Guid.NewGuid(), userId, ReviewResourceType.DsaProblem,
            Guid.NewGuid(), From, From);
        var schedule = item.Evaluate(
            ReviewEvaluation.Good, 0, From.AddDays(1));
        return (item, ReviewHistory.Create(
            Guid.NewGuid(), item.Id, ReviewEvaluation.Good,
            schedule, From.AddDays(1)));
    }

    private static DsaProblem CreateProblem(Guid userId, string title) =>
        DsaProblem.Create(
            Guid.NewGuid(), userId, title, "Description",
            DsaProblemDifficulty.Easy, null, null, ["Array"], From);

    private static DsaAttempt CreateAttempt(Guid problemId) =>
        DsaAttempt.Create(
            Guid.NewGuid(), problemId, 1, DsaAttemptResult.Solved,
            "C#", null, null, null, null, 20, null,
            From.AddDays(2), From.AddDays(2));

    private static async Task<T> GetAsync<T>(
        HttpClient client, string route)
    {
        using var response = await client.GetAsync(
            $"/api/v1/analytics/{route}{RangeQuery()}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<(HttpClient Client, RegisterResponse User)>
        AuthenticateAsync()
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false,
                HandleCookies = true
            });
        var email = $"analytics-regression-{Guid.NewGuid():N}@example.com";
        using var register = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Analytics Regression", "Example123!"));
        var user = await register.Content.ReadFromJsonAsync<RegisterResponse>();
        register.EnsureSuccessStatusCode();
        using var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Example123!"));
        login.EnsureSuccessStatusCode();
        return (client, user!);
    }

    private static string RangeQuery() =>
        $"?fromUtc={Uri.EscapeDataString(From.ToString("O"))}"
        + $"&toUtc={Uri.EscapeDataString(To.ToString("O"))}";
}
