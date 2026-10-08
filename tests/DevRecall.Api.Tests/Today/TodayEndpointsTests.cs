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
using Microsoft.EntityFrameworkCore;
using DevRecall.Infrastructure.Development;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using DevRecall.Domain.StudyPlans;

namespace DevRecall.Api.Tests.Today;

[Collection(AuthApiTestSuite.Name)]
public sealed class TodayEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task V2Candidates_ShouldUseOwnedIntentBeforeDiscoverWithoutWritingEvidence()
    {
        var auth = await CreateAuthenticatedClientAsync("v2-intent");
        using var client = auth.Client;
        var other = await CreateAuthenticatedClientAsync("v2-isolation");
        using var otherClient = other.Client;
        Guid lessonId;
        var now = DateTimeOffset.UtcNow;
        var plan = StudyPlan.Create(Guid.NewGuid(), auth.User.Id, "Planned documentation", now, null);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<LearningContentSeeder>().SeedAsync();
            var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
            lessonId = await db.LearningContents.Where(row => row.Slug == "ef-core-transactions").Select(row => row.Id).SingleAsync();
            var resource = await db.LearningContents.SingleAsync(row => row.Slug == "ef-core-handling-concurrency-conflicts");
            plan.EnsureManualItem(Guid.NewGuid(), StudyPlanResourceType.LearningContent, resource.Id, 20, plan.Version, now);
            db.StudyPlans.Add(plan);
            db.LearningProfiles.Add(LearningProfile.Create(Guid.NewGuid(), auth.User.Id, TargetRole.BackendDeveloper,
                ExperienceLevel.Junior, 15, [(Technology.EfCore, true)], [LearningProfileGoal.ImproveBackendFundamentals], now));
            await db.SaveChangesAsync();
        }
        var planned = (await client.GetFromJsonAsync<GetTodayDashboardResponse>("/api/v1/today"))!;
        planned.NextAction.Type.Should().Be("ContinueStudyPlan");
        planned.NextAction.TargetPath.Should().Be($"/app/study-plans/{plan.Id}");
        planned.StudyPlan!.Items[0].IsResourceAvailable.Should().BeTrue();
        (await otherClient.GetFromJsonAsync<GetTodayDashboardResponse>("/api/v1/today"))!.NextAction.Type.Should().Be("BrowseLearning");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
            db.LearningContentProgresses.Add(LearningContentProgress.Start(Guid.NewGuid(), auth.User.Id, lessonId, now));
            await db.SaveChangesAsync();
        }
        var ongoing = (await client.GetFromJsonAsync<GetTodayDashboardResponse>("/api/v1/today"))!;
        ongoing.NextAction.Type.Should().Be("ContinueLearning");
        var sessionId = await SeedActiveSessionAsync(auth.User.Id, "Execution context");
        (await client.GetFromJsonAsync<GetTodayDashboardResponse>("/api/v1/today"))!.NextAction.Type.Should().Be("ContinueStudySession");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
            var session = await db.StudySessions.Include(row => row.Items).SingleAsync(row => row.Id == sessionId);
            session.Cancel(DateTimeOffset.UtcNow);
            var progress = await db.LearningContentProgresses.SingleAsync(row => row.UserId == auth.User.Id && row.LearningContentId == lessonId);
            progress.Complete(progress.Version, DateTimeOffset.UtcNow);
            var storedPlan = await db.StudyPlans.Include(row => row.Items).SingleAsync(row => row.Id == plan.Id);
            storedPlan.Cancel(storedPlan.Version, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }
        (await client.GetFromJsonAsync<GetTodayDashboardResponse>("/api/v1/today"))!.NextAction.Type.Should().Be("LearnRecommendedContent");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
            (await db.LearningContentCompletionEvidence.CountAsync(row => row.UserId == auth.User.Id)).Should().Be(0);
            (await db.LearningContentProgresses.CountAsync(row => row.UserId == auth.User.Id)).Should().Be(1);
        }
    }
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
        dashboard.NextAction.Type.Should().Be("BrowseLearning");
        dashboard.WeeklyActivity.Should().HaveCount(7);
        dashboard.WeeklyActivity.Select(point => point.Date)
            .Should().BeInAscendingOrder();
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
        dashboard!.NextAction.Type.Should().Be("BrowseLearning");
        dashboard.RecentActivity.Should().BeNull();
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
        dashboard.RecentActivity.Should().BeNull("the primary session action already represents this work");
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
