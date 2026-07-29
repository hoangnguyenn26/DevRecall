using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Analytics;
using DevRecall.Contracts.Auth;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Api.Tests.Analytics;

[Collection(AuthApiTestSuite.Name)]
public sealed class DsaPerformanceEndpointsTests(AuthApiFactory factory)
{
    private static readonly DateTimeOffset From =
        new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = From.AddDays(7);

    [Fact]
    public async Task Get_ShouldRequireAuthentication()
    {
        using var response = await CreateClient().GetAsync(
            "/api/v1/analytics/dsa-performance");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_EmptyState_ShouldReturnZeros()
    {
        var auth = await AuthenticateAsync();
        using var response = await auth.Client.GetAsync(BuildUrl());
        var result = await response.Content
            .ReadFromJsonAsync<DsaPerformanceResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().Be(new DsaPerformanceResponse(
            From, To, 0, 0, 0, 0, 0, 0, 0m, 0m));
    }

    [Fact]
    public async Task Get_ShouldAggregateDistinctOwnedProblemsInRange()
    {
        var owner = await AuthenticateAsync();
        var other = await AuthenticateAsync();
        await SeedAsync(owner.User.Id, other.User.Id);

        using var response = await owner.Client.GetAsync(BuildUrl());
        var result = await response.Content
            .ReadFromJsonAsync<DsaPerformanceResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.TotalAttempts.Should().Be(5);
        result.SolvedAttempts.Should().Be(2);
        result.PartiallySolvedAttempts.Should().Be(1);
        result.FailedAttempts.Should().Be(1);
        result.SkippedAttempts.Should().Be(1);
        result.ProblemsPracticed.Should().Be(2);
        result.SolvedRate.Should().Be(40m);
        result.AverageDurationMinutes.Should().Be(30m);
    }

    private async Task SeedAsync(Guid ownerId, Guid otherId)
    {
        var first = CreateProblem(ownerId, "First");
        var second = CreateProblem(ownerId, "Second");
        var other = CreateProblem(otherId, "Other");
        var attempts = new[]
        {
            CreateAttempt(first.Id, 1, DsaAttemptResult.Solved, 10, From),
            CreateAttempt(first.Id, 2, DsaAttemptResult.Failed, 20, From.AddDays(1)),
            CreateAttempt(first.Id, 3, DsaAttemptResult.PartiallySolved, 30, From.AddDays(2)),
            CreateAttempt(second.Id, 1, DsaAttemptResult.Solved, 40, To.AddSeconds(-1)),
            CreateAttempt(second.Id, 2, DsaAttemptResult.Skipped, 50, From.AddDays(3)),
            CreateAttempt(second.Id, 3, DsaAttemptResult.Solved, 60, To),
            CreateAttempt(other.Id, 1, DsaAttemptResult.Solved, 70, From)
        };
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        db.DsaProblems.AddRange(first, second, other);
        db.DsaAttempts.AddRange(attempts);
        await db.SaveChangesAsync();
    }

    private static DsaProblem CreateProblem(Guid userId, string title) =>
        DsaProblem.Create(
            Guid.NewGuid(), userId, title, "Description",
            DsaProblemDifficulty.Easy, null, null, ["Array"], From);

    private static DsaAttempt CreateAttempt(
        Guid problemId, int number, DsaAttemptResult result,
        int duration, DateTimeOffset attemptedAt) =>
        DsaAttempt.Create(
            Guid.NewGuid(), problemId, number, result, "C#", null, null,
            null, null, duration, null, attemptedAt, attemptedAt);

    private async Task<(HttpClient Client, RegisterResponse User)>
        AuthenticateAsync()
    {
        var client = CreateClient();
        var email = $"dsa-performance-{Guid.NewGuid():N}@example.com";
        using var register = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "DSA Analytics", "Example123!"));
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
        "/api/v1/analytics/dsa-performance"
        + $"?fromUtc={Uri.EscapeDataString(From.ToString("O"))}"
        + $"&toUtc={Uri.EscapeDataString(To.ToString("O"))}";
}
