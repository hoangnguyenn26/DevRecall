using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Analytics;
using DevRecall.Contracts.Auth;
using DevRecall.Domain.Reviews;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Api.Tests.Analytics;

[Collection(AuthApiTestSuite.Name)]
public sealed class ReviewPerformanceEndpointsTests(AuthApiFactory factory)
{
    private static readonly DateTimeOffset From =
        new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = From.AddDays(7);

    [Fact]
    public async Task Get_ShouldRequireAuthentication()
    {
        using var response = await CreateClient().GetAsync(
            "/api/v1/analytics/review-performance");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_EmptyState_ShouldReturnZeros()
    {
        var auth = await AuthenticateAsync();
        using var response = await auth.Client.GetAsync(BuildUrl());
        var result = await response.Content
            .ReadFromJsonAsync<ReviewPerformanceResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().Be(new ReviewPerformanceResponse(
            From, To, 0, 0, 0, 0, 0, 0m, 0m, 0m));
    }

    [Fact]
    public async Task Get_ShouldAggregateRangeAndOwnerHistories()
    {
        var owner = await AuthenticateAsync();
        var other = await AuthenticateAsync();
        await SeedAsync(owner.User.Id, other.User.Id);

        using var response = await owner.Client.GetAsync(BuildUrl());
        var result = await response.Content
            .ReadFromJsonAsync<ReviewPerformanceResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.TotalReviews.Should().Be(4);
        result.AgainCount.Should().Be(1);
        result.HardCount.Should().Be(1);
        result.GoodCount.Should().Be(1);
        result.EasyCount.Should().Be(1);
        result.SuccessRate.Should().Be(50m);
        (result.AgainCount + result.HardCount
            + result.GoodCount + result.EasyCount).Should()
            .Be(result.TotalReviews);
    }

    private async Task SeedAsync(Guid ownerId, Guid otherId)
    {
        var rows = new[]
        {
            Create(ownerId, ReviewEvaluation.Again, From),
            Create(ownerId, ReviewEvaluation.Hard, From.AddDays(1)),
            Create(ownerId, ReviewEvaluation.Good, To.AddSeconds(-1)),
            Create(ownerId, ReviewEvaluation.Easy, From.AddDays(2)),
            Create(ownerId, ReviewEvaluation.Good, To),
            Create(otherId, ReviewEvaluation.Easy, From)
        };
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        db.ReviewItems.AddRange(rows.Select(row => row.Item));
        db.ReviewHistories.AddRange(rows.Select(row => row.History));
        await db.SaveChangesAsync();
    }

    private static (ReviewItem Item, ReviewHistory History) Create(
        Guid userId, ReviewEvaluation evaluation, DateTimeOffset reviewedAt)
    {
        var item = ReviewItem.Create(
            Guid.NewGuid(), userId, ReviewResourceType.DsaProblem,
            Guid.NewGuid(), reviewedAt, reviewedAt.AddDays(-1));
        var schedule = item.Evaluate(evaluation, 0, reviewedAt);
        return (item, ReviewHistory.Create(
            Guid.NewGuid(), item.Id, evaluation, schedule, reviewedAt));
    }

    private async Task<(HttpClient Client, RegisterResponse User)>
        AuthenticateAsync()
    {
        var client = CreateClient();
        var email = $"review-performance-{Guid.NewGuid():N}@example.com";
        using var register = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Review Analytics", "Example123!"));
        var user = await register.Content.ReadFromJsonAsync<RegisterResponse>();
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
        "/api/v1/analytics/review-performance"
        + $"?fromUtc={Uri.EscapeDataString(From.ToString("O"))}"
        + $"&toUtc={Uri.EscapeDataString(To.ToString("O"))}";
}
