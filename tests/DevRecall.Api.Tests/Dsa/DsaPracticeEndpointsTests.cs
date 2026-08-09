using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Dsa;
using DevRecall.Contracts.Dsa.Attempts;
using DevRecall.Contracts.Dsa.Practice;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevRecall.Api.Tests.Dsa;

[Collection(AuthApiTestSuite.Name)]
public sealed class DsaPracticeEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task PracticeRead_ShouldBeOwnerScopedAndExcludeAttemptData()
    {
        using var owner = await CreateClientAsync();
        var problem = await CreateProblemAsync(owner, "Two Sum");
        var practice = await owner.GetFromJsonAsync<GetDsaPracticeResponse>(
            $"/api/v1/dsa-problems/{problem.Id}/practice");
        practice!.Title.Should().Be("Two Sum");

        using var other = await CreateClientAsync();
        using var response = await other.GetAsync($"/api/v1/dsa-problems/{problem.Id}/practice");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PracticeSubmit_ShouldDeriveDurationAndBeIdempotent()
    {
        using var client = await CreateClientAsync();
        var problem = await CreateProblemAsync(client, "Two Sum");
        var submissionId = Guid.NewGuid();
        var started = DateTimeOffset.UtcNow.AddMinutes(-3);
        var request = new CreateDsaAttemptRequest(
            "Solved", "CSharp", "return result;", "Hash map", "O(n)", "O(n)",
            999, "Learned the pattern.", started, submissionId, started);
        var first = await PostAsync<DsaAttemptResponse>(client,
            $"/api/v1/dsa-problems/{problem.Id}/attempts", request);
        var retry = await PostAsync<DsaAttemptResponse>(client,
            $"/api/v1/dsa-problems/{problem.Id}/attempts", request);
        retry.Id.Should().Be(first.Id);
        first.DurationMinutes.Should().BeInRange(3, 4);
        var detail = await client.GetFromJsonAsync<DsaAttemptDetailResponse>(
            $"/api/v1/dsa-problems/{problem.Id}/attempts/{first.Id}");
        detail!.ProblemTitleSnapshot.Should().Be("Two Sum");
        detail.DifficultySnapshot.Should().Be("Easy");
        detail.StartedAtUtc.Should().BeCloseTo(started, TimeSpan.FromMilliseconds(1));
        detail.DurationSeconds.Should().BeInRange(180, 240);

        var otherProblem = await CreateProblemAsync(client, "Three Sum");
        using var conflict = await client.PostAsJsonAsync(
            $"/api/v1/dsa-problems/{otherProblem.Id}/attempts", request);
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<HttpClient> CreateClientAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
        var email = $"dsa-practice-{Guid.NewGuid():N}@example.com";
        (await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "DSA User", "Example123!"))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Example123!"))).EnsureSuccessStatusCode();
        return client;
    }

    private static Task<DsaProblemResponse> CreateProblemAsync(HttpClient client, string title) =>
        PostAsync<DsaProblemResponse>(client, "/api/v1/dsa-problems",
            new CreateDsaProblemRequest(title, "Find the result.", "Easy", "LeetCode", "https://leetcode.com/problems/two-sum", ["Array"]));

    private static async Task<T> PostAsync<T>(HttpClient client, string path, object body)
    {
        using var response = await client.PostAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
