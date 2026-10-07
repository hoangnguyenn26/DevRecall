using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Analytics;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.Knowledge;
using DevRecall.Contracts.LearningContent;
using DevRecall.Contracts.Reviews;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using DevRecall.Domain.Reviews;
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
    public async Task ExternalResources_ShouldBrowseAsMetadataAndRejectLessonMutationsWithoutEvidence()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("resources");
        var all = await GetPageAsync(client, "");
        all.Items.Should().Contain(item => item.ContentType == "Lesson")
            .And.Contain(item => item.ContentType == "ExternalResource");
        var resources = await GetPageAsync(client, "contentType=ExternalResource&technology=EfCore&difficulty=Intermediate");
        resources.Items.Should().HaveCount(2).And.OnlyContain(item => item.ProgressStatus == null
            && item.ResourceKind == "Documentation" && item.SourceName == "Microsoft Learn");
        var slug = "ef-core-handling-concurrency-conflicts";
        var detail = await client.GetFromJsonAsync<LearningContentDetailResponse>($"/api/v1/learning-content/{slug}");
        detail!.Progress.Should().BeNull();
        detail.ResourceKind.Should().Be("Documentation");
        detail.Goals.Should().Contain("ImproveBackendFundamentals");
        detail.Objectives.Should().BeEmpty();
        detail.Sections.Should().BeEmpty();
        detail.ReviewCandidates.Should().BeEmpty();
        detail.Source.Url.Should().Be("https://learn.microsoft.com/en-us/ef/core/saving/concurrency");
        (await client.PostAsync($"/api/v1/learning-content/{slug}/progress/start", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        var complete = await client.PostAsJsonAsync($"/api/v1/learning-content/{slug}/progress/complete", new CompleteLearningContentRequest(null));
        complete.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await complete.Content.ReadAsStringAsync()).Should().Contain("LEARNING_CONTENT_LESSON_REQUIRED");
        (await client.PostAsJsonAsync($"/api/v1/knowledge/from-learning-content/{slug}", new
        {
            title = "Resource note", content = "Not supported", topicId = (Guid?)null, tagIds = Array.Empty<Guid>(), submissionId = Guid.NewGuid()
        })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync($"/api/v1/review/from-learning-content/{slug}", new
        {
            candidateKeys = new List<string> { "anything" }, submissionId = Guid.NewGuid()
        })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var options = await client.GetFromJsonAsync<object[]>($"/api/v1/study-plans/learning-content/{slug}/options");
        options.Should().BeEmpty();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        (await db.LearningContentProgresses.CountAsync(item => item.LearningContentId == detail.Id)).Should().Be(0);
        (await db.LearningContentCompletionEvidence.CountAsync(item => item.LearningContentId == detail.Id)).Should().Be(0);
        var source = await db.LearningContents.SingleAsync(item => item.Id == detail.Id);
        source.Archive(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
        (await client.GetAsync($"/api/v1/learning-content/{slug}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Discover_ShouldBeAuthenticatedReadOnlyAndExcludeOnlyTheCurrentUsersProgress()
    {
        await SeedAsync();
        using var anonymous = factory.CreateClient();
        (await anonymous.GetAsync("/api/v1/discover")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var client = await CreateAuthenticatedClientAsync("discover");
        var missing = await client.GetFromJsonAsync<DevRecall.Application.Discover.DiscoverResult>("/api/v1/discover");
        missing!.ProfileConfigured.Should().BeFalse();
        missing.BasedOnGoals.Should().BeEmpty();
        missing.Recommended.Should().BeEmpty();
        using var configured = await client.PutAsJsonAsync("/api/v1/learning-profile", new
        {
            targetRole = "BackendDeveloper", experienceLevel = "Junior", availableMinutesPerDay = 15,
            technologies = new[] { new { name = "AspNetCore", isPrimary = true } },
            goals = new List<string> { "ImproveBackendFundamentals", "PrepareForInterviews" }, expectedVersion = (int?)null
        });
        configured.EnsureSuccessStatusCode();
        var first = await client.GetFromJsonAsync<DevRecall.Application.Discover.DiscoverResult>("/api/v1/discover");
        first!.BasedOnGoals.Should().HaveCount(3);
        first.Recommended.Should().HaveCount(4);
        first.Recommended.Select(item => item.Slug).Should().NotIntersectWith(first.BasedOnGoals.Select(item => item.Slug));
        first.BasedOnGoals.Select(item => item.Slug).Should().OnlyHaveUniqueItems();
        first.BasedOnGoals.Should().OnlyContain(item => item.Reasons.Count >= 1 && item.Reasons.Count <= 2);
        first.BasedOnWeakTopics.Should().BeEmpty();
        var slug = first.Recommended[0].Slug;
        (await client.PostAsync($"/api/v1/learning-content/{slug}/progress/start", null)).EnsureSuccessStatusCode();
        var second = await client.GetFromJsonAsync<DevRecall.Application.Discover.DiscoverResult>("/api/v1/discover");
        second!.BasedOnGoals.Should().NotContain(item => item.Slug == slug);
        second.Recommended.Should().NotContain(item => item.Slug == slug);
        using var other = await CreateAuthenticatedClientAsync("discover-other");
        (await other.PutAsJsonAsync("/api/v1/learning-profile", new
        {
            targetRole = "BackendDeveloper", experienceLevel = "Junior", availableMinutesPerDay = 15,
            technologies = new[] { new { name = "AspNetCore", isPrimary = true } },
            goals = new List<string> { "ImproveBackendFundamentals" }, expectedVersion = (int?)null
        })).EnsureSuccessStatusCode();
        var otherResult = await other.GetFromJsonAsync<DevRecall.Application.Discover.DiscoverResult>("/api/v1/discover");
        otherResult!.Recommended.Concat(otherResult.BasedOnGoals).Should().Contain(item => item.Slug == slug);
        otherResult.BasedOnGoals.Should().OnlyContain(item => item.Reasons.Count == 1);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        var progressCount = await db.LearningContentProgresses.CountAsync();
        var evidenceCount = await db.LearningContentCompletionEvidence.CountAsync();
        var repeated = await client.GetFromJsonAsync<DevRecall.Application.Discover.DiscoverResult>("/api/v1/discover");
        repeated!.Should().BeEquivalentTo(second, options => options.WithStrictOrdering());
        (await db.LearningContentProgresses.CountAsync()).Should().Be(progressCount);
        (await db.LearningContentCompletionEvidence.CountAsync()).Should().Be(evidenceCount);
    }

    [Fact]
    public async Task Discover_ShouldNotUseDifficultyOrTimeAsEligibilityAndShouldAllowNoGoalMatches()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("discover-unmatched");
        (await client.PutAsJsonAsync("/api/v1/learning-profile", new
        {
            targetRole = "FrontendDeveloper", experienceLevel = "Beginner", availableMinutesPerDay = 15,
            technologies = new[] { new { name = "Vue", isPrimary = true } },
            goals = new List<string> { "ImproveDsa" }, expectedVersion = (int?)null
        })).EnsureSuccessStatusCode();
        var result = await client.GetFromJsonAsync<DevRecall.Application.Discover.DiscoverResult>("/api/v1/discover");
        result!.ProfileConfigured.Should().BeTrue();
        result.BasedOnGoals.Should().BeEmpty();
        result.Recommended.Should().BeEmpty();
        result.BasedOnWeakTopics.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverRanking_ShouldUseExactTechnologySignalsWithoutExposingScoresAndRefreshAfterProfileChange()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("ranking");
        (await client.PutAsJsonAsync("/api/v1/learning-profile", new
        {
            targetRole = "BackendDeveloper", experienceLevel = "Junior", availableMinutesPerDay = 30,
            technologies = new[] { new { name = "EfCore", isPrimary = true } },
            goals = new List<string> { "ImproveDsa" }, expectedVersion = (int?)null
        })).EnsureSuccessStatusCode();
        var first = await client.GetFromJsonAsync<DevRecall.Contracts.Discover.DiscoverResponse>("/api/v1/discover");
        first!.Recommended.Should().HaveCount(3);
        first.Recommended.Should().OnlyContain(item => item.Technologies.Any(technology => technology.Value == "EfCore"));
        first.Recommended.Should().OnlyContain(item => item.Reasons[0].Type == "PrimaryTechnologyMatch"
            && item.Reasons[0].Value == "EfCore" && item.Reasons.Count <= 2);
        var json = await client.GetStringAsync("/api/v1/discover");
        json.Should().NotContain("\"score\"").And.NotContain("\"breakdown\"").And.NotContain("\"sections\"");
        // Opening the reader is read-only and leaves both ordering and eligibility unchanged.
        (await client.GetAsync($"/api/v1/learning-content/{first.Recommended[0].Slug}")).EnsureSuccessStatusCode();
        (await client.GetStringAsync("/api/v1/discover")).Should().Be(json);
        (await client.PostAsJsonAsync($"/api/v1/learning-content/{first.Recommended[0].Slug}/progress/complete",
            new { expectedVersion = (int?)null })).EnsureSuccessStatusCode();
        var completed = await client.GetFromJsonAsync<DevRecall.Contracts.Discover.DiscoverResponse>("/api/v1/discover");
        completed!.Recommended.Should().HaveCount(2).And.NotContain(item => item.Slug == first.Recommended[0].Slug);
        (await client.PutAsJsonAsync("/api/v1/learning-profile", new
        {
            targetRole = "BackendDeveloper", experienceLevel = "Junior", availableMinutesPerDay = 30,
            technologies = new[] { new { name = "Java", isPrimary = true } },
            goals = new List<string> { "ImproveDsa" }, expectedVersion = 1
        })).EnsureSuccessStatusCode();
        var changed = await client.GetFromJsonAsync<DevRecall.Contracts.Discover.DiscoverResponse>("/api/v1/discover");
        changed!.Recommended.Should().BeEmpty();
    }
    [Fact]
    public async Task List_ShouldReturnPublishedSummariesWithDeterministicPaging()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("list");

        var firstPage = await client.GetFromJsonAsync<PagedResponse<LearningContentListItemResponse>>(
            "/api/v1/learning-content?contentType=Lesson&page=1&pageSize=3");
        var secondPage = await client.GetFromJsonAsync<PagedResponse<LearningContentListItemResponse>>(
            "/api/v1/learning-content?contentType=Lesson&page=2&pageSize=3");
        var thirdPage = await client.GetFromJsonAsync<PagedResponse<LearningContentListItemResponse>>(
            "/api/v1/learning-content?contentType=Lesson&page=3&pageSize=3");

        firstPage!.TotalCount.Should().Be(11);
        firstPage.Items.Should().HaveCount(3);
        secondPage!.Items.Should().HaveCount(3);
        thirdPage!.Items.Should().HaveCount(3);
        var fourthPage = await client.GetFromJsonAsync<PagedResponse<LearningContentListItemResponse>>(
            "/api/v1/learning-content?contentType=Lesson&page=4&pageSize=3");
        fourthPage!.Items.Should().HaveCount(2);
        firstPage.Items.Select(item => item.Slug).Should().NotIntersectWith(
            secondPage.Items.Select(item => item.Slug));
        secondPage.Items.Select(item => item.Slug).Should().NotIntersectWith(
            thirdPage.Items.Select(item => item.Slug));
        firstPage.Items.Should().OnlyContain(item => item.ContentType == "Lesson");
    }

    [Fact]
    public async Task List_ShouldFilterByCanonicalTechnologyTopicAndDifficulty()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("filters");

        var technology = await GetPageAsync(client, "contentType=Lesson&technology=EfCore");
        var topic = await GetPageAsync(client, "contentType=Lesson&topic=dependency-injection");
        var difficulty = await GetPageAsync(client, "difficulty=Beginner");
        var pipelineTopic = await GetPageAsync(client, "contentType=Lesson&topic=request-pipeline");
        var asyncTopic = await GetPageAsync(client, "contentType=Lesson&topic=asynchronous-programming");
        var transactionsTopic = await GetPageAsync(client, "topic=transactions");
        var concurrencyTopic = await GetPageAsync(client, "contentType=Lesson&topic=concurrency");

        technology.Items.Select(item => item.Slug).Should().BeEquivalentTo(
            ["ef-core-tracking-vs-no-tracking", "ef-core-transactions", "ef-core-optimistic-concurrency"]);
        topic.Items.Should().HaveCount(2);
        difficulty.Items.Select(item => item.Slug).Should().BeEquivalentTo(
            ["dependency-injection-fundamentals", "async-await-fundamentals", "authentication-vs-authorization"]);
        pipelineTopic.Items.Should().ContainSingle(item => item.Slug == "aspnet-core-middleware-pipeline");
        asyncTopic.Items.Select(item => item.Slug).Should().BeEquivalentTo(
            ["async-await-fundamentals", "aspnet-core-cancellation-tokens"]);
        transactionsTopic.Items.Should().ContainSingle(item => item.Slug == "ef-core-transactions");
        concurrencyTopic.Items.Should().ContainSingle(item => item.Slug == "ef-core-optimistic-concurrency");
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
    [InlineData("contentType=2")]
    [InlineData("contentType=Video")]
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

        before!.Progress!.Status.Should().Be("NotStarted");
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
        (await db.LearningContentCompletionEvidence.CountAsync(item =>
            item.Id == progress.CompletionEvidenceId)).Should().Be(1);
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

        var firstPage = await GetPageAsync(firstUser, "topic=change-tracking");
        var secondPage = await GetPageAsync(secondUser, "topic=change-tracking");
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
    public async Task Completion_ConcurrentRequests_ShouldConvergeOnOneEvidence()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("complete-race");
        var route = "/api/v1/learning-content/ef-core-tracking-vs-no-tracking/progress/complete";

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync(route, new CompleteLearningContentRequest(null)),
            client.PostAsJsonAsync(route, new CompleteLearningContentRequest(null)));

        responses.Should().OnlyContain(response => response.IsSuccessStatusCode);
        var results = await Task.WhenAll(responses.Select(response =>
            response.Content.ReadFromJsonAsync<LearningContentProgressResponse>()));
        results.Should().OnlyContain(result => result!.Status == "Completed");
        results.Select(result => result!.CompletionEvidenceId).Distinct().Should().ContainSingle();
        var evidenceId = results[0]!.CompletionEvidenceId;
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        (await db.LearningContentCompletionEvidence.CountAsync(item => item.Id == evidenceId))
            .Should().Be(1);
    }

    [Fact]
    public async Task ContinueAndHistory_ShouldReflectCanonicalProgressAndRemainOwnerScoped()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("recovery");
        using var other = await CreateAuthenticatedClientAsync("recovery-other");
        const string slug = "ef-core-tracking-vs-no-tracking";

        using var started = await client.PostAsync(
            $"/api/v1/learning-content/{slug}/progress/start", null);
        var inProgress = await client.GetFromJsonAsync<ContinueLearningContentResponse[]>(
            "/api/v1/learning-content/continue");
        var otherInProgress = await other.GetFromJsonAsync<ContinueLearningContentResponse[]>(
            "/api/v1/learning-content/continue");

        started.EnsureSuccessStatusCode();
        inProgress.Should().ContainSingle(item => item.Slug == slug);
        inProgress.Should().OnlyContain(item => item.StartedAtUtc != default);
        otherInProgress.Should().BeEmpty();

        using var completed = await client.PostAsJsonAsync(
            $"/api/v1/learning-content/{slug}/progress/complete",
            new CompleteLearningContentRequest(1));
        var afterCompletion = await client.GetFromJsonAsync<ContinueLearningContentResponse[]>(
            "/api/v1/learning-content/continue");
        var history = await client.GetFromJsonAsync<PagedResponse<LearningContentHistoryItemResponse>>(
            "/api/v1/learning-content/history?page=1&pageSize=20");
        var otherHistory = await other.GetFromJsonAsync<PagedResponse<LearningContentHistoryItemResponse>>(
            "/api/v1/learning-content/history?page=1&pageSize=20");

        completed.EnsureSuccessStatusCode();
        afterCompletion.Should().BeEmpty();
        history!.Items.Should().ContainSingle(item => item.Title == "EF Core Tracking vs No Tracking"
            && item.IsSourceAvailable && item.SourceSlug == slug);
        otherHistory!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Continue_ShouldBeBoundedAndNewestStartedFirst()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("continue-order");
        var slugs = new[]
        {
            "dependency-injection-fundamentals",
            "aspnet-core-service-lifetimes",
            "ef-core-tracking-vs-no-tracking"
        };
        foreach (var slug in slugs)
        {
            using var response = await client.PostAsync(
                $"/api/v1/learning-content/{slug}/progress/start", null);
            response.EnsureSuccessStatusCode();
            await Task.Delay(10);
        }

        var result = await client.GetFromJsonAsync<ContinueLearningContentResponse[]>(
            "/api/v1/learning-content/continue");

        result.Should().HaveCountLessThanOrEqualTo(5);
        result!.Select(item => item.Slug).Should().Equal(slugs.Reverse());
        result.Select(item => item.StartedAtUtc).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task History_ShouldPreserveSnapshotWhenCompletedLessonIsArchived()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("archived-history");
        const string slug = "ef-core-tracking-vs-no-tracking";
        using var completed = await client.PostAsJsonAsync(
            $"/api/v1/learning-content/{slug}/progress/complete",
            new CompleteLearningContentRequest(null));
        completed.EnsureSuccessStatusCode();

        await SetLessonArchivedAsync(slug, true);
        try
        {
            var history = await client.GetFromJsonAsync<PagedResponse<LearningContentHistoryItemResponse>>(
                "/api/v1/learning-content/history?page=1&pageSize=20");
            var inProgress = await client.GetFromJsonAsync<ContinueLearningContentResponse[]>(
                "/api/v1/learning-content/continue");

            history!.Items.Should().ContainSingle(item => item.Title == "EF Core Tracking vs No Tracking"
                && !item.IsSourceAvailable && item.SourceSlug == null);
            inProgress.Should().BeEmpty();
        }
        finally
        {
            await SetLessonArchivedAsync(slug, false);
        }
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public async Task History_ShouldValidateBoundedPagination(int page, int pageSize)
    {
        using var client = await CreateAuthenticatedClientAsync("history-validation");
        using var response = await client.GetAsync(
            $"/api/v1/learning-content/history?page={page}&pageSize={pageSize}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

    [Fact]
    public async Task SaveToKnowledge_ShouldCreateEditableNoteWithTagsAndSource()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("save-knowledge");
        var topic = await CreateKnowledgeAsync(client, "Dependency injection notes");
        var tag = await (await client.PostAsJsonAsync("/api/v1/knowledge/tags",
            new CreateKnowledgeTagRequest("dotnet"))).Content.ReadFromJsonAsync<KnowledgeTagSummaryResponse>();
        var submissionId = Guid.NewGuid();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/knowledge/from-learning-content/dependency-injection-fundamentals",
            new SaveLearningContentToKnowledgeRequest("My DI notes", "## Key takeaways\n\nPrefer explicit dependencies.",
                topic.Id, [tag!.Id], submissionId));

        response.EnsureSuccessStatusCode();
        var saved = await response.Content.ReadFromJsonAsync<SavedKnowledgeResponse>();
        saved!.AlreadyExisted.Should().BeFalse();
        var detail = await client.GetFromJsonAsync<KnowledgeWorkspaceDetailResponse>(
            $"/api/v1/knowledge/{saved.Id}");
        detail!.Title.Should().Be("My DI notes");
        detail.Content.Should().Contain("Prefer explicit dependencies");
        detail.TopicId.Should().Be(topic.Id);
        detail.Tags.Should().ContainSingle(item => item.Id == tag.Id);
        detail.Source.Should().Be(new KnowledgeSourceResponse("LearningContent",
            "Dependency Injection Fundamentals", "dependency-injection-fundamentals", true));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        var lesson = await db.LearningContents.Include(item => item.Topics)
            .Include(item => item.Objectives).Include(item => item.Sections)
            .SingleAsync(item => item.Slug == "dependency-injection-fundamentals");
        lesson.Archive(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
        var afterArchive = await client.GetFromJsonAsync<KnowledgeWorkspaceDetailResponse>(
            $"/api/v1/knowledge/{saved.Id}");
        afterArchive!.Source.Should().Be(new KnowledgeSourceResponse("LearningContent",
            "Dependency Injection Fundamentals", null, false));
        lesson.Publish(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task SaveToKnowledge_ShouldBeIdempotentPerUserSubmission()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("save-idempotent");
        var submissionId = Guid.NewGuid();
        var request = new SaveLearningContentToKnowledgeRequest("First title", "First body", null, [], submissionId);

        var first = await (await client.PostAsJsonAsync(
            "/api/v1/knowledge/from-learning-content/aspnet-core-service-lifetimes", request))
            .Content.ReadFromJsonAsync<SavedKnowledgeResponse>();
        var retry = await (await client.PostAsJsonAsync(
            "/api/v1/knowledge/from-learning-content/aspnet-core-service-lifetimes",
            request with { Title = "Changed retry title" })).Content.ReadFromJsonAsync<SavedKnowledgeResponse>();

        retry!.Id.Should().Be(first!.Id);
        retry.Title.Should().Be("First title");
        retry.AlreadyExisted.Should().BeTrue();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        (await db.KnowledgeSources.CountAsync(item => item.SubmissionId == submissionId)).Should().Be(1);
    }

    [Fact]
    public async Task SaveToKnowledge_DifferentSubmissions_ShouldCreateIndependentSnapshotNotes()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("save-independent-notes");
        var route = "/api/v1/knowledge/from-learning-content/aspnet-core-service-lifetimes";

        var first = await (await client.PostAsJsonAsync(route,
            new SaveLearningContentToKnowledgeRequest("Lifetime notes", "First personal note", null, [],
                Guid.NewGuid()))).Content.ReadFromJsonAsync<SavedKnowledgeResponse>();
        var second = await (await client.PostAsJsonAsync(route,
            new SaveLearningContentToKnowledgeRequest("Lifetime follow-up", "Second personal note", null, [],
                Guid.NewGuid()))).Content.ReadFromJsonAsync<SavedKnowledgeResponse>();

        second!.Id.Should().NotBe(first!.Id);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        (await db.KnowledgeSources.CountAsync(item => item.KnowledgeNodeId == first.Id
            || item.KnowledgeNodeId == second.Id)).Should().Be(2);

        var originalTitle = await db.LearningContents.Where(item => item.Slug == "aspnet-core-service-lifetimes")
            .Select(item => item.Title).SingleAsync();
        try
        {
            await db.LearningContents.Where(item => item.Slug == "aspnet-core-service-lifetimes")
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Title, "Revised source title"));
            var saved = await client.GetFromJsonAsync<KnowledgeWorkspaceDetailResponse>(
                $"/api/v1/knowledge/{first.Id}");
            saved!.Title.Should().Be("Lifetime notes");
            saved.Content.Should().Be("First personal note");
            saved.Source.Should().Be(new KnowledgeSourceResponse("LearningContent",
                originalTitle, "aspnet-core-service-lifetimes", true));
        }
        finally
        {
            await db.LearningContents.Where(item => item.Slug == "aspnet-core-service-lifetimes")
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Title, originalTitle));
        }
    }

    [Fact]
    public async Task SaveToKnowledge_ShouldRejectUnavailableLessonAndForeignOrganization()
    {
        await SeedAsync();
        await AddUnpublishedContentAsync();
        using var owner = await CreateAuthenticatedClientAsync("save-owner");
        using var other = await CreateAuthenticatedClientAsync("save-other");
        var foreignTopic = await CreateKnowledgeAsync(other, "Private topic");
        var request = new SaveLearningContentToKnowledgeRequest("Notes", "", foreignTopic.Id, [], Guid.NewGuid());

        using var foreign = await owner.PostAsJsonAsync(
            "/api/v1/knowledge/from-learning-content/dependency-injection-fundamentals", request);
        using var draft = await owner.PostAsJsonAsync(
            "/api/v1/knowledge/from-learning-content/hidden-draft-lesson", request with
            { TopicId = null, SubmissionId = Guid.NewGuid() });

        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        draft.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddToReview_ShouldCreateSelectedCandidateSnapshotsWithSchedulerDefaults()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("lesson-review-create");
        var submissionId = Guid.NewGuid();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/review/from-learning-content/aspnet-core-service-lifetimes",
            new CreateLearningContentReviewsRequest(
                ["three-service-lifetimes", "captive-dependency"], submissionId));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LearningContentReviewBatchResponse>();
        result!.CreatedCount.Should().Be(2);
        result.ExistingCount.Should().Be(0);
        result.Items.Should().HaveCount(2).And.OnlyContain(item => item.WasCreated);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        var createdIds = result.Items.Select(item => item.ReviewItemId).ToArray();
        var reviewItems = await db.ReviewItems.Where(item => createdIds.Contains(item.Id)).ToListAsync();
        reviewItems.Should().OnlyContain(item => item.ResourceType == ReviewResourceType.LearningContent
            && item.IntervalDays == 0 && item.ReviewCount == 0 && item.LastReviewedAtUtc == null);
        var sources = await db.ReviewLearningContentSources.Where(item => createdIds.Contains(item.ReviewItemId))
            .ToListAsync();
        sources.Should().Contain(item => item.CandidateKey == "three-service-lifetimes"
            && item.PromptSnapshot.Contains("three built-in", StringComparison.Ordinal)
            && item.AnswerSnapshot == "Transient, Scoped, and Singleton.");
        (await db.ReviewHistories.CountAsync(item => createdIds.Contains(item.ReviewItemId))).Should().Be(0);
        var due = await client.GetFromJsonAsync<PagedResponse<DueReviewItemResponse>>(
            "/api/v1/review-items/due?resourceType=LearningContent&page=1&pageSize=10");
        due!.Items.Should().Contain(item => item.ResourceTitle.Contains("three built-in", StringComparison.Ordinal)
            && item.ResourcePreview == "Transient, Scoped, and Singleton."
            && item.Source == new ReviewSourceResponse("LearningContent",
                "ASP.NET Core Service Lifetimes", "aspnet-core-service-lifetimes", true));
    }

    [Fact]
    public async Task LessonReviewSource_ShouldRemainUsableAndBecomeUnavailableAfterLessonArchive()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("lesson-review-archive");
        var created = await (await client.PostAsJsonAsync(
            "/api/v1/review/from-learning-content/aspnet-core-service-lifetimes",
            new CreateLearningContentReviewsRequest(["captive-dependency"], Guid.NewGuid()))).Content
            .ReadFromJsonAsync<LearningContentReviewBatchResponse>();
        var reviewItemId = created!.Items.Single().ReviewItemId;

        try
        {
            await SetLessonArchivedAsync("aspnet-core-service-lifetimes", true);
            var due = await client.GetFromJsonAsync<PagedResponse<DueReviewItemResponse>>(
                "/api/v1/review-items/due?resourceType=LearningContent&page=1&pageSize=10");
            var item = due!.Items.Single(value => value.ReviewItemId == reviewItemId);

            item.ResourceTitle.Should().Be("Why can injecting a Scoped service into a Singleton be problematic?");
            item.ResourcePreview.Should().Contain("captive dependency");
            item.Source.Should().Be(new ReviewSourceResponse("LearningContent",
                "ASP.NET Core Service Lifetimes", "aspnet-core-service-lifetimes", false));
        }
        finally
        {
            await SetLessonArchivedAsync("aspnet-core-service-lifetimes", false);
        }
    }

    [Fact]
    public async Task LessonReviews_ShouldUseNormalOutcomesWithoutChangingLessonCompletionEvidence()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("lesson-review-retention");
        const string slug = "aspnet-core-service-lifetimes";
        (await client.PostAsJsonAsync($"/api/v1/learning-content/{slug}/progress/complete",
            new CompleteLearningContentRequest(null))).EnsureSuccessStatusCode();
        var created = await (await client.PostAsJsonAsync($"/api/v1/review/from-learning-content/{slug}",
            new CreateLearningContentReviewsRequest(
                ["three-service-lifetimes", "captive-dependency"], Guid.NewGuid()))).Content
            .ReadFromJsonAsync<LearningContentReviewBatchResponse>();

        var first = created!.Items[0].ReviewItemId;
        var second = created.Items[1].ReviewItemId;
        using var good = await client.PostAsJsonAsync($"/api/v1/review-items/{first}/evaluate",
            new EvaluateReviewItemRequest("Good", 0, Guid.NewGuid()));
        using var again = await client.PostAsJsonAsync($"/api/v1/review-items/{second}/evaluate",
            new EvaluateReviewItemRequest("Again", 0, Guid.NewGuid()));
        var goodResult = await good.Content.ReadFromJsonAsync<EvaluateReviewItemResponse>();
        var againResult = await again.Content.ReadFromJsonAsync<EvaluateReviewItemResponse>();
        var analytics = await client.GetFromJsonAsync<AnalyticsOverviewResponse>(
            "/api/v1/analytics/overview?range=7d");

        good.EnsureSuccessStatusCode();
        again.EnsureSuccessStatusCode();
        goodResult!.Evaluation.Should().Be("Good");
        goodResult.NextIntervalDays.Should().BeGreaterThan(0);
        againResult!.Evaluation.Should().Be("Again");
        againResult.NextIntervalDays.Should().BeGreaterThan(0);
        analytics!.LearningContentCompletedCount.Should().Be(1);
        analytics.ReviewCount.Should().Be(2);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        var contentId = await db.LearningContents.Where(item => item.Slug == slug)
            .Select(item => item.Id).SingleAsync();
        var userId = await db.ReviewItems.Where(review => review.Id == first)
            .Select(review => review.UserId).SingleAsync();
        (await db.LearningContentCompletionEvidence.CountAsync(item =>
            item.UserId == userId && item.LearningContentId == contentId)).Should().Be(1);
        (await db.ReviewHistories.CountAsync(item => item.ReviewItemId == first
            || item.ReviewItemId == second)).Should().Be(2);
    }

    [Fact]
    public async Task AddToReview_ShouldKeepPromptAndAnswerSnapshotsAfterCandidateRevision()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("lesson-review-snapshot");
        var created = await (await client.PostAsJsonAsync(
            "/api/v1/review/from-learning-content/ef-core-tracking-vs-no-tracking",
            new CreateLearningContentReviewsRequest(["tracking-query"], Guid.NewGuid()))).Content
            .ReadFromJsonAsync<LearningContentReviewBatchResponse>();
        var reviewItemId = created!.Items.Single().ReviewItemId;

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        var candidate = await db.LearningContentReviewCandidates.AsNoTracking()
            .SingleAsync(item => item.Key == "tracking-query");
        try
        {
            await db.LearningContentReviewCandidates.Where(item => item.Id == candidate.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Prompt, "Revised prompt")
                    .SetProperty(item => item.Answer, "Revised answer"));
            var due = await client.GetFromJsonAsync<PagedResponse<DueReviewItemResponse>>(
                "/api/v1/review-items/due?resourceType=LearningContent&page=1&pageSize=10");
            due!.Items.Single(item => item.ReviewItemId == reviewItemId).Should().Match<DueReviewItemResponse>(item =>
                item.ResourceTitle == candidate.Prompt && item.ResourcePreview == candidate.Answer);
        }
        finally
        {
            await db.LearningContentReviewCandidates.Where(item => item.Id == candidate.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Prompt, candidate.Prompt)
                    .SetProperty(item => item.Answer, candidate.Answer));
        }
    }

    [Fact]
    public async Task AddToReview_ShouldBeBatchIdempotentAndPreventCandidateDuplicates()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("lesson-review-idempotent");
        var route = "/api/v1/review/from-learning-content/aspnet-core-service-lifetimes";
        var submissionId = Guid.NewGuid();
        var firstRequest = new CreateLearningContentReviewsRequest(["captive-dependency"], submissionId);

        var first = await (await client.PostAsJsonAsync(route, firstRequest)).Content
            .ReadFromJsonAsync<LearningContentReviewBatchResponse>();
        var retry = await (await client.PostAsJsonAsync(route, firstRequest)).Content
            .ReadFromJsonAsync<LearningContentReviewBatchResponse>();
        var anotherSubmission = await (await client.PostAsJsonAsync(route,
            firstRequest with { SubmissionId = Guid.NewGuid() })).Content
            .ReadFromJsonAsync<LearningContentReviewBatchResponse>();

        retry.Should().BeEquivalentTo(first);
        anotherSubmission!.CreatedCount.Should().Be(0);
        anotherSubmission.ExistingCount.Should().Be(1);
        anotherSubmission.Items.Single().ReviewItemId.Should().Be(first!.Items.Single().ReviewItemId);
    }

    [Fact]
    public async Task AddToReview_ConcurrentTabs_ShouldConvergeOnOneActiveCandidate()
    {
        await SeedAsync();
        using var client = await CreateAuthenticatedClientAsync("lesson-review-race");
        var route = "/api/v1/review/from-learning-content/aspnet-core-service-lifetimes";
        var requests = new[]
        {
            new CreateLearningContentReviewsRequest(["lifetime-selection"], Guid.NewGuid()),
            new CreateLearningContentReviewsRequest(["lifetime-selection"], Guid.NewGuid())
        };

        var responses = await Task.WhenAll(requests.Select(request => client.PostAsJsonAsync(route, request)));
        responses.Should().OnlyContain(response => response.IsSuccessStatusCode);
        var results = await Task.WhenAll(responses.Select(response =>
            response.Content.ReadFromJsonAsync<LearningContentReviewBatchResponse>()));
        results.SelectMany(result => result!.Items).Select(item => item.ReviewItemId).Distinct().Should().ContainSingle();
        results.Sum(result => result!.CreatedCount).Should().Be(1);
        results.Sum(result => result!.ExistingCount).Should().Be(1);
    }

    [Fact]
    public async Task LessonDetail_ShouldProjectOwnerScopedReviewCandidateStateAndAllowReAddAfterArchive()
    {
        await SeedAsync();
        using var owner = await CreateAuthenticatedClientAsync("lesson-review-owner");
        using var other = await CreateAuthenticatedClientAsync("lesson-review-other");
        var route = "/api/v1/review/from-learning-content/aspnet-core-service-lifetimes";
        var created = await (await owner.PostAsJsonAsync(route,
            new CreateLearningContentReviewsRequest(["scoped-per-request"], Guid.NewGuid()))).Content
            .ReadFromJsonAsync<LearningContentReviewBatchResponse>();

        var ownerDetail = await owner.GetFromJsonAsync<LearningContentDetailResponse>(
            "/api/v1/learning-content/aspnet-core-service-lifetimes");
        var otherDetail = await other.GetFromJsonAsync<LearningContentDetailResponse>(
            "/api/v1/learning-content/aspnet-core-service-lifetimes");
        ownerDetail!.ReviewCandidates.Single(item => item.Key == "scoped-per-request").IsInReview.Should().BeTrue();
        otherDetail!.ReviewCandidates.Single(item => item.Key == "scoped-per-request").IsInReview.Should().BeFalse();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        var item = await db.ReviewItems.SingleAsync(value => value.Id == created!.Items.Single().ReviewItemId);
        item.Archive(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
        var readded = await (await owner.PostAsJsonAsync(route,
            new CreateLearningContentReviewsRequest(["scoped-per-request"], Guid.NewGuid()))).Content
            .ReadFromJsonAsync<LearningContentReviewBatchResponse>();
        readded!.CreatedCount.Should().Be(1);
        readded.Items.Single().ReviewItemId.Should().NotBe(item.Id);
    }

    [Fact]
    public async Task AddToReview_ShouldRejectUnknownCandidateAndUnavailableLessonAtomically()
    {
        await SeedAsync();
        await AddUnpublishedContentAsync();
        using var client = await CreateAuthenticatedClientAsync("lesson-review-invalid");
        var invalidSubmissionId = Guid.NewGuid();
        using var invalid = await client.PostAsJsonAsync(
            "/api/v1/review/from-learning-content/aspnet-core-service-lifetimes",
            new CreateLearningContentReviewsRequest(
                ["three-service-lifetimes", "not-a-candidate"], invalidSubmissionId));
        using var archived = await client.PostAsJsonAsync(
            "/api/v1/review/from-learning-content/hidden-archived-lesson",
            new CreateLearningContentReviewsRequest(["anything"], Guid.NewGuid()));

        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        archived.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        (await db.LearningContentReviewSubmissions.AnyAsync(item =>
            item.SubmissionId == invalidSubmissionId)).Should().BeFalse();
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

    private async Task SetLessonArchivedAsync(string slug, bool archived)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        var content = await db.LearningContents.Include(item => item.Topics)
            .Include(item => item.Objectives).Include(item => item.Sections)
            .SingleAsync(item => item.Slug == slug);
        if (archived) content.Archive(DateTimeOffset.UtcNow);
        else content.Publish(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
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

    private static async Task<KnowledgeNodeResponse> CreateKnowledgeAsync(HttpClient client, string title)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/knowledge-nodes",
            new CreateKnowledgeNodeRequest(title, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<KnowledgeNodeResponse>())!;
    }

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
