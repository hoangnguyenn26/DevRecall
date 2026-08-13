using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.LearningContent;
using DevRecall.Contracts.Knowledge;
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
            && item.ResourcePreview == "Transient, Scoped, and Singleton.");
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
            new CreateLearningContentReviewsRequest(["singleton-lifetime"], Guid.NewGuid()),
            new CreateLearningContentReviewsRequest(["singleton-lifetime"], Guid.NewGuid())
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
