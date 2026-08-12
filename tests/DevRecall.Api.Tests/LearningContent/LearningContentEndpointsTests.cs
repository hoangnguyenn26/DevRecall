using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.LearningContent;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using DevRecall.Infrastructure.Development;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LearningContentAggregate = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.Api.Tests.LearningContent;

[Collection(AuthApiTestSuite.Name)]
public sealed class LearningContentEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task List_ShouldReturnPublishedSummariesWithDeterministicPaging()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("list");

        var firstPage = await client.GetFromJsonAsync<PagedResponse<LearningContentListItemResponse>>(
            "/api/v1/learning-content?page=1&pageSize=2");
        var secondPage = await client.GetFromJsonAsync<PagedResponse<LearningContentListItemResponse>>(
            "/api/v1/learning-content?page=2&pageSize=2");

        firstPage!.TotalCount.Should().Be(3);
        firstPage.Items.Should().HaveCount(2);
        secondPage!.Items.Should().ContainSingle();
        firstPage.Items.Select(item => item.Slug).Should().NotIntersectWith(
            secondPage.Items.Select(item => item.Slug));
        firstPage.Items.Should().OnlyContain(item => item.ContentType == "Lesson");
    }

    [Fact]
    public async Task List_ShouldFilterByCanonicalTechnologyTopicAndDifficulty()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("filters");

        var technology = await GetPageAsync(client, "technology=EfCore");
        var topic = await GetPageAsync(client, "topic=dependency-injection");
        var difficulty = await GetPageAsync(client, "difficulty=Beginner");

        technology.Items.Should().ContainSingle(item => item.Slug == "ef-core-tracking-vs-no-tracking");
        topic.Items.Should().HaveCount(2);
        difficulty.Items.Should().ContainSingle(item => item.Slug == "dependency-injection-fundamentals");
    }

    [Fact]
    public async Task Detail_ShouldReturnOrderedLessonBodyAndProvenance()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("detail");

        var detail = await client.GetFromJsonAsync<LearningContentDetailResponse>(
            "/api/v1/learning-content/aspnet-core-service-lifetimes");

        detail!.Source.Should().Be(new LearningContentSourceResponse("Internal", "DevRecall", null));
        detail.Objectives.Select(item => item.Position).Should().BeInAscendingOrder();
        detail.Sections.Select(item => item.Position).Should().BeInAscendingOrder();
        detail.Sections.Should().Contain(item => item.BodyMarkdown.Contains("```csharp", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Detail_ShouldHideDraftArchivedAndUnknownSlugs()
    {
        await SeedAsync();
        await AddUnpublishedContentAsync();
        using var client = await CreateAuthenticatedClientAsync("hidden");

        using var draft = await client.GetAsync("/api/v1/learning-content/hidden-draft-lesson");
        using var archived = await client.GetAsync("/api/v1/learning-content/hidden-archived-lesson");
        using var unknown = await client.GetAsync("/api/v1/learning-content/not-exist");

        draft.StatusCode.Should().Be(HttpStatusCode.NotFound);
        archived.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("technology=1")]
    [InlineData("difficulty=2")]
    [InlineData("technology=NinjaFramework")]
    [InlineData("page=0")]
    [InlineData("pageSize=1000")]
    public async Task List_ShouldRejectInvalidFiltersAndPagination(string query)
    {
        using var client = await CreateAuthenticatedClientAsync("invalid");
        using var response = await client.GetAsync($"/api/v1/learning-content?{query}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoints_ShouldRequireAuthentication()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        using var list = await client.GetAsync("/api/v1/learning-content");
        using var detail = await client.GetAsync("/api/v1/learning-content/aspnet-core-service-lifetimes");
        list.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        detail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Start_ShouldCreateProgressAndRemainIdempotent()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("start");

        var before = await client.GetFromJsonAsync<LearningContentDetailResponse>(
            "/api/v1/learning-content/aspnet-core-service-lifetimes");
        using var first = await client.PostAsJsonAsync(
            "/api/v1/learning-content/aspnet-core-service-lifetimes/progress/start", new { });
        using var second = await client.PostAsJsonAsync(
            "/api/v1/learning-content/aspnet-core-service-lifetimes/progress/start", new { });
        var firstProgress = await first.Content.ReadFromJsonAsync<LearningContentProgressResponse>();
        var secondProgress = await second.Content.ReadFromJsonAsync<LearningContentProgressResponse>();

        before!.Progress.Status.Should().Be("NotStarted");
        secondProgress!.Status.Should().Be(firstProgress!.Status);
        secondProgress.Version.Should().Be(firstProgress.Version);
        secondProgress.StartedAtUtc.Should().BeCloseTo(firstProgress.StartedAtUtc!.Value, TimeSpan.FromMilliseconds(1));
        firstProgress.Status.Should().Be("InProgress");
        firstProgress.Version.Should().Be(1);
    }

    [Fact]
    public async Task Complete_ShouldSupportDirectCompletionAndCreateEvidenceOnce()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("complete");
        var route = "/api/v1/learning-content/dependency-injection-fundamentals/progress/complete";
        await using var beforeScope = factory.Services.CreateAsyncScope();
        var beforeDb = beforeScope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        var reviewCount = await beforeDb.ReviewItems.CountAsync();
        var weakTopicCount = await beforeDb.WeakTopicProfiles.CountAsync();

        using var first = await client.PostAsJsonAsync(route, new CompleteLearningContentRequest(null));
        using var retry = await client.PostAsJsonAsync(route, new CompleteLearningContentRequest(null));
        first.EnsureSuccessStatusCode();
        retry.EnsureSuccessStatusCode();
        var progress = await retry.Content.ReadFromJsonAsync<LearningContentProgressResponse>();
        progress!.Status.Should().Be("Completed");

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        (await db.LearningContentCompletionEvidence.CountAsync()).Should().Be(1);
        (await db.ReviewItems.CountAsync()).Should().Be(reviewCount);
        (await db.WeakTopicProfiles.CountAsync()).Should().Be(weakTopicCount);
    }

    [Fact]
    public async Task Progress_ShouldBeIsolatedPerUserAndProjectedInCatalog()
    {
        await SeedAsync();
        using var firstUser = await CreateAuthenticatedClientAsync("owner-a");
        using var secondUser = await CreateAuthenticatedClientAsync("owner-b");
        await firstUser.PostAsJsonAsync(
            "/api/v1/learning-content/ef-core-tracking-vs-no-tracking/progress/start", new { });

        var firstPage = await GetPageAsync(firstUser, "technology=EfCore");
        var secondPage = await GetPageAsync(secondUser, "technology=EfCore");
        firstPage.Items.Single().ProgressStatus.Should().Be("InProgress");
        secondPage.Items.Single().ProgressStatus.Should().Be("NotStarted");
    }

    [Fact]
    public async Task Completion_ShouldBeRetrySafeAcrossTabsAndRemainImmutable()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("multi-tab");
        var baseRoute = "/api/v1/learning-content/aspnet-core-service-lifetimes/progress";
        var started = await (await client.PostAsJsonAsync($"{baseRoute}/start", new { }))
            .Content.ReadFromJsonAsync<LearningContentProgressResponse>();

        var completed = await (await client.PostAsJsonAsync($"{baseRoute}/complete",
            new CompleteLearningContentRequest(started!.Version))).Content
            .ReadFromJsonAsync<LearningContentProgressResponse>();
        var staleTab = await (await client.PostAsJsonAsync($"{baseRoute}/complete",
            new CompleteLearningContentRequest(started.Version))).Content
            .ReadFromJsonAsync<LearningContentProgressResponse>();
        var startAgain = await (await client.PostAsJsonAsync($"{baseRoute}/start", new { }))
            .Content.ReadFromJsonAsync<LearningContentProgressResponse>();

        staleTab!.Status.Should().Be("Completed");
        staleTab.Version.Should().Be(completed!.Version);
        staleTab.CompletedAtUtc.Should().BeCloseTo(completed.CompletedAtUtc!.Value, TimeSpan.FromMilliseconds(1));
        startAgain!.Status.Should().Be("Completed");
        startAgain.Version.Should().Be(completed.Version);
        completed.Version.Should().Be(2);
    }

    [Fact]
    public async Task Mutations_ShouldHideDraftAndArchivedLessons()
    {
        await SeedAsync();
        await AddUnpublishedContentAsync();
        using var client = await CreateAuthenticatedClientAsync("hidden-mutations");

        foreach (var slug in new[] { "hidden-draft-lesson", "hidden-archived-lesson" })
        {
            using var start = await client.PostAsJsonAsync($"/api/v1/learning-content/{slug}/progress/start", new { });
            using var complete = await client.PostAsJsonAsync($"/api/v1/learning-content/{slug}/progress/complete",
                new CompleteLearningContentRequest(null));
            start.StatusCode.Should().Be(HttpStatusCode.NotFound);
            complete.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    private async Task SeedAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LearningContentSeeder>().SeedAsync();
    }

    private async Task AddUnpublishedContentAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        if (await dbContext.LearningContents.AnyAsync(item => item.Slug == "hidden-draft-lesson")) return;
        var topicId = await dbContext.ContentTopics.Select(item => item.Id).FirstAsync();
        var draft = Create("hidden-draft-lesson", topicId);
        var archived = Create("hidden-archived-lesson", topicId);
        archived.Publish(DateTimeOffset.UtcNow.AddMinutes(-1));
        archived.Archive(DateTimeOffset.UtcNow);
        dbContext.LearningContents.AddRange(draft, archived);
        await dbContext.SaveChangesAsync();
    }

    private static LearningContentAggregate Create(string slug, Guid topicId) =>
        LearningContentAggregate.CreateDraft(Guid.NewGuid(), slug, "Hidden lesson title",
            "A valid summary for unpublished content.", LearningContentType.Lesson,
            ContentDifficulty.Beginner, 10, ContentSourceType.Internal, "DevRecall", null,
            [Technology.DotNet], [topicId], [new("Understand the hidden lesson.")],
            [new(LearningContentSectionType.Explanation, null, "Hidden body")],
            DateTimeOffset.UtcNow.AddMinutes(-2));

    private static async Task<PagedResponse<LearningContentListItemResponse>> GetPageAsync(
        HttpClient client, string query) => (await client.GetFromJsonAsync<
            PagedResponse<LearningContentListItemResponse>>($"/api/v1/learning-content?{query}"))!;

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string prefix)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var email = $"content-{prefix}-{Guid.NewGuid():N}@example.com";
        using var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Content Learner", "Example123!"));
        register.EnsureSuccessStatusCode();
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "Example123!"));
        login.EnsureSuccessStatusCode();
        return client;
    }
}
