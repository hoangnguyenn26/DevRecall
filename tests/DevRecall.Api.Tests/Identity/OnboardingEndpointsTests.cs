using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Onboarding;
using DevRecall.Contracts.Today;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevRecall.Api.Tests.Identity;

[Collection(AuthApiTestSuite.Name)]
public sealed class OnboardingEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task Get_ShouldReturnIncompleteForExistingUserWithoutProfile()
    {
        using var client = await CreateAuthenticatedClientAsync("empty");

        var result = await client.GetFromJsonAsync<GetOnboardingResponse>(
            "/api/v1/onboarding");
        var today = await client.GetFromJsonAsync<GetTodayDashboardResponse>(
            "/api/v1/today");

        result!.HasCompleted.Should().BeFalse();
        result.Goal.Should().BeNull();
        today!.User.HasCompletedOnboarding.Should().BeFalse();
        today.Metrics.WeeklyTargetDays.Should().Be(5);
    }

    [Fact]
    public async Task Complete_ShouldPersistOwnedPreferencesAndConfigureToday()
    {
        using var client = await CreateAuthenticatedClientAsync("complete");
        await AddCsrfTokenAsync(client);
        var request = new CompleteOnboardingRequest(
            "PracticeAlgorithms", 45, 7,
            ["AlgorithmsAndDataStructures", "Databases", "Databases"]);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/onboarding/complete", request);
        var result = await response.Content
            .ReadFromJsonAsync<GetOnboardingResponse>();
        var today = await client.GetFromJsonAsync<GetTodayDashboardResponse>(
            "/api/v1/today");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.CompletionType.Should().Be("Completed");
        result.FocusAreas.Should().BeEquivalentTo(
            "AlgorithmsAndDataStructures", "Databases");
        today!.User.HasCompletedOnboarding.Should().BeTrue();
        today.Metrics.WeeklyTargetDays.Should().Be(7);
    }

    [Theory]
    [InlineData("1", "DotNet")]
    [InlineData("01", "DotNet")]
    [InlineData("+1", "DotNet")]
    [InlineData("PracticeAlgorithms", "5")]
    [InlineData("PracticeAlgorithms", "01")]
    [InlineData("PracticeAlgorithms", "+1")]
    public async Task Complete_ShouldRejectNumericEnumStrings(
        string goal, string focusArea)
    {
        using var client = await CreateAuthenticatedClientAsync("numeric");
        await AddCsrfTokenAsync(client);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/onboarding/complete",
            new CompleteOnboardingRequest(goal, 30, 5, [focusArea]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Skip_ShouldApplyDefaultsAndBeIdempotent()
    {
        using var client = await CreateAuthenticatedClientAsync("skip");
        await AddCsrfTokenAsync(client);

        using var first = await client.PostAsync(
            "/api/v1/onboarding/skip", null);
        using var second = await client.PostAsync(
            "/api/v1/onboarding/skip", null);
        var result = await second.Content
            .ReadFromJsonAsync<GetOnboardingResponse>();

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.CompletionType.Should().Be("Skipped");
        result.DailyCommitmentMinutes.Should().Be(30);
        result.WeeklyTargetDays.Should().Be(5);
    }

    [Fact]
    public async Task Skip_ShouldNotOverwriteCompletedPreferences()
    {
        using var client = await CreateAuthenticatedClientAsync("conflict");
        await AddCsrfTokenAsync(client);
        using var complete = await client.PostAsJsonAsync(
            "/api/v1/onboarding/complete",
            new CompleteOnboardingRequest(
                "StrengthenDotNetSkills", 60, 6, ["DotNet"]));
        complete.EnsureSuccessStatusCode();

        using var response = await client.PostAsync(
            "/api/v1/onboarding/skip", null);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        problem.Should().NotBeNull();
        problem!.Extensions.TryGetValue("errorCode", out var errorCode)
            .Should().BeTrue();
        errorCode!.ToString()
            .Should().Contain("ONBOARDING_ALREADY_COMPLETED");
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string prefix)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var email = $"onboarding-{prefix}-{Guid.NewGuid():N}@example.com";
        using var register = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "New Learner", "Example123!"));
        register.EnsureSuccessStatusCode();
        using var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Example123!"));
        login.EnsureSuccessStatusCode();
        return client;
    }

    private static async Task AddCsrfTokenAsync(HttpClient client)
    {
        var token = await client.GetFromJsonAsync<CsrfTokenResponse>(
            "/api/v1/auth/csrf-token");
        client.DefaultRequestHeaders.Remove(token!.HeaderName);
        client.DefaultRequestHeaders.Add(token.HeaderName, token.RequestToken);
    }
}
