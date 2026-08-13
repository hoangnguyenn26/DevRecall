using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Analytics;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.LearningContent;
using DevRecall.Infrastructure.Development;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
        result.Activity.Should().OnlyContain(x => x.StudyMinutes == 0 && x.PracticeCount == 0
            && x.LessonsCompleted == 0);
        result.LearningContentCompletedCount.Should().Be(0);
    }

    [Fact]
    public async Task Overview_CountsCanonicalLessonEvidenceOnceWithoutInventingStudyTime()
    {
        await SeedLearningContentAsync();
        var auth = await CreateAuthenticatedClientAsync();
        const string completeRoute =
            "/api/v1/learning-content/ef-core-tracking-vs-no-tracking/progress/complete";

        using var first = await auth.Client.PostAsJsonAsync(
            completeRoute, new CompleteLearningContentRequest(null));
        using var retry = await auth.Client.PostAsJsonAsync(
            completeRoute, new CompleteLearningContentRequest(null));
        var other = await CreateAuthenticatedClientAsync();
        using var otherCompletion = await other.Client.PostAsJsonAsync(
            completeRoute, new CompleteLearningContentRequest(null));
        using var response = await auth.Client.GetAsync("/api/v1/analytics/overview?range=7d");
        var result = await response.Content.ReadFromJsonAsync<AnalyticsOverviewResponse>();

        first.EnsureSuccessStatusCode();
        retry.EnsureSuccessStatusCode();
        otherCompletion.EnsureSuccessStatusCode();
        response.EnsureSuccessStatusCode();
        result!.LearningContentCompletedCount.Should().Be(1);
        result.StudyMinutes.Current.Should().Be(0);
        result.ActiveDays.Current.Should().Be(1);
        result.Activity.Sum(day => day.LessonsCompleted).Should().Be(1);
        result.RecentActivity.Should().ContainSingle(item =>
            item.Type == "LearningContentCompleted"
            && item.Title == "EF Core Tracking vs No Tracking"
            && item.IsSourceAvailable
            && item.SourceSlug == "ef-core-tracking-vs-no-tracking");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
            var content = await db.LearningContents
                .Include(item => item.Topics).Include(item => item.Objectives).Include(item => item.Sections)
                .SingleAsync(item => item.Slug == "ef-core-tracking-vs-no-tracking");
            content.Archive(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }
        using var archivedResponse = await auth.Client.GetAsync("/api/v1/analytics/overview?range=7d");
        var archived = await archivedResponse.Content.ReadFromJsonAsync<AnalyticsOverviewResponse>();
        archived!.LearningContentCompletedCount.Should().Be(1);
        archived.RecentActivity.Should().ContainSingle(item =>
            item.Title == "EF Core Tracking vs No Tracking"
            && !item.IsSourceAvailable && item.SourceSlug == null);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
            var content = await db.LearningContents
                .Include(item => item.Topics).Include(item => item.Objectives).Include(item => item.Sections)
                .SingleAsync(item => item.Slug == "ef-core-tracking-vs-no-tracking");
            content.Publish(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Overview_DoesNotCountStartingALesson()
    {
        await SeedLearningContentAsync();
        var auth = await CreateAuthenticatedClientAsync();
        using var started = await auth.Client.PostAsync(
            "/api/v1/learning-content/ef-core-tracking-vs-no-tracking/progress/start", null);
        using var response = await auth.Client.GetAsync("/api/v1/analytics/overview?range=7d");
        var result = await response.Content.ReadFromJsonAsync<AnalyticsOverviewResponse>();

        started.EnsureSuccessStatusCode();
        result!.LearningContentCompletedCount.Should().Be(0);
        result.ActiveDays.Current.Should().Be(0);
        result.RecentActivity.Should().BeEmpty();
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

    [Fact]
    public async Task Insights_ReturnsBoundedEmptyResultForNewUser()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var response = await auth.Client.GetAsync("/api/v1/analytics/insights?range=7d&take=10");
        var result = await response.Content.ReadFromJsonAsync<LearningInsightsResponse>();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Range.Should().Be("7d");
        result.Items.Should().BeEmpty();
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

    private async Task SeedLearningContentAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LearningContentSeeder>().SeedAsync();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        var content = await db.LearningContents
            .Include(item => item.Topics).Include(item => item.Objectives).Include(item => item.Sections)
            .SingleAsync(item => item.Slug == "ef-core-tracking-vs-no-tracking");
        if (content.Status == DevRecall.Domain.LearningContent.ContentStatus.Archived)
        {
            content.Publish(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }
    }
}
