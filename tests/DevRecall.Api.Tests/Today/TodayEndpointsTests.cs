using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Navigation;
using DevRecall.Contracts.Today;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Api.Tests.Today;

[Collection(AuthApiTestSuite.Name)]
public sealed class TodayEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task Get_ShouldRequireAuthentication()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/api/v1/today");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NavigationIndicators_ShouldRequireAuthentication()
    {
        using var client = CreateClient();
        using var response = await client.GetAsync("/api/v1/navigation-indicators");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NavigationIndicators_ShouldReturnCurrentUserSummary()
    {
        var auth = await CreateAuthenticatedClientAsync("indicators");
        using var client = auth.Client;
        using var response = await client.GetAsync("/api/v1/navigation-indicators");
        var indicators = await response.Content.ReadFromJsonAsync<NavigationIndicatorsResponse>();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        indicators.Should().BeEquivalentTo(new NavigationIndicatorsResponse(0, false, 0, true));
    }

    [Fact]
    public async Task EmptyDashboard_ShouldReturnSevenDaysAndOnboardingAction()
    {
        var auth = await CreateAuthenticatedClientAsync("empty");
        using var client = auth.Client;

        using var response = await client.GetAsync("/api/v1/today");
        var dashboard = await response.Content
            .ReadFromJsonAsync<GetTodayDashboardResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        dashboard!.User.UserId.Should().Be(auth.User.Id);
        dashboard.NextAction.Type.Should().Be("CreateKnowledge");
        dashboard.WeeklyActivity.Should().HaveCount(7);
        dashboard.Recommendations.Should().HaveCountLessThanOrEqualTo(3);
        dashboard.WeakTopics.Should().HaveCountLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task Dashboard_ShouldIgnoreCrossUserActiveSession()
    {
        var owner = await CreateAuthenticatedClientAsync("owner");
        using var client = owner.Client;
        var other = await CreateAuthenticatedClientAsync("other");
        using var otherClient = other.Client;
        await SeedActiveSessionAsync(other.User.Id, "Other user's session");

        using var response = await client.GetAsync("/api/v1/today");
        var dashboard = await response.Content
            .ReadFromJsonAsync<GetTodayDashboardResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        dashboard!.NextAction.Type.Should().Be("CreateKnowledge");
    }

    [Fact]
    public async Task Dashboard_ShouldPrioritizeOwnedActiveSession()
    {
        var auth = await CreateAuthenticatedClientAsync("session");
        using var client = auth.Client;
        var sessionId = await SeedActiveSessionAsync(
            auth.User.Id, "Focused practice");

        using var response = await client.GetAsync("/api/v1/today");
        var dashboard = await response.Content
            .ReadFromJsonAsync<GetTodayDashboardResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        dashboard!.NextAction.Type.Should().Be("ContinueStudySession");
        dashboard.NextAction.TargetPath.Should().Be(
            $"/app/study-sessions/{sessionId}");
        dashboard.RecentActivity.Should().NotBeNull();
        dashboard.RecentActivity!.Type.Should().Be("StudySession");
        dashboard.RecentActivity.ResourceId.Should().Be(sessionId);
    }

    private async Task<Guid> SeedActiveSessionAsync(Guid userId, string title)
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-5);
        var session = StudySession.Create(
            Guid.NewGuid(), userId, title, 30, null, now);
        session.AddItem(
            Guid.NewGuid(), StudyResourceType.KnowledgeNode,
            Guid.NewGuid(), null, now);
        session.Start(now.AddMinutes(1));
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<DevRecallDbContext>();
        context.StudySessions.Add(session);
        await context.SaveChangesAsync();
        return session.Id;
    }

    private async Task<(HttpClient Client, RegisterResponse User)>
        CreateAuthenticatedClientAsync(string prefix)
    {
        var client = CreateClient();
        var email = $"today-{prefix}-{Guid.NewGuid():N}@example.com";
        using var register = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Today Learner", "Example123!"));
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
}
