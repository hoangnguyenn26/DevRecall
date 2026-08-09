using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Analytics;
using DevRecall.Contracts.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevRecall.Api.Tests.Analytics;

[Collection(AuthApiTestSuite.Name)]
public sealed class AnalyticsInsightsEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task Overview_RequiresAuthentication()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync("/api/v1/analytics/overview?range=7d");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Overview_RejectsUnsupportedRange()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var response = await auth.Client.GetAsync("/api/v1/analytics/overview?range=365d");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Overview_ZeroFillsSevenDaysForNewUser()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var response = await auth.Client.GetAsync("/api/v1/analytics/overview?range=7d");
        var result = await response.Content.ReadFromJsonAsync<AnalyticsOverviewResponse>();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Activity.Should().HaveCount(7);
        result.Activity.Should().OnlyContain(x => x.StudyMinutes == 0 && x.PracticeCount == 0);
    }

    [Fact]
    public async Task Performance_ReturnsZeroObjectsForNewUser()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var response = await auth.Client.GetAsync("/api/v1/analytics/performance?range=30d");
        var result = await response.Content.ReadFromJsonAsync<LearningPerformanceResponse>();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.ReviewCurrent.Total.Should().Be(0);
        result.InterviewCurrent.Total.Should().Be(0);
        result.DsaCurrent.Total.Should().Be(0);
    }

    private async Task<(HttpClient Client, RegisterResponse User)> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var email = $"analytics-insights-{Guid.NewGuid():N}@example.com";
        using var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "Analytics User", "Example123!"));
        var user = await register.Content.ReadFromJsonAsync<RegisterResponse>();
        register.EnsureSuccessStatusCode();
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Example123!"));
        login.EnsureSuccessStatusCode();
        return (client, user!);
    }
}
