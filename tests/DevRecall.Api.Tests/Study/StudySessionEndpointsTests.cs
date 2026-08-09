using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.Dsa;
using DevRecall.Contracts.Study;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Api.Tests.Study;

[Collection(AuthApiTestSuite.Name)]
public sealed class StudySessionEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task Complete_ShouldCalculateDurationPreserveItemsAndReturnSummary()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var client = auth.Client;
        var session = await CreateSessionAsync(client);
        var pending = await AddProblemItemAsync(client, session.Id, "Pending");
        using var start = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/start",
            new StartStudySessionRequest(pending.Version));
        var detail = await GetDetailAsync(client, session.Id);

        using var complete = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/complete",
            new CompleteStudySessionRequest(detail.Version));
        var result = await complete.Content
            .ReadFromJsonAsync<CompleteStudySessionResponse>();
        var persisted = await GetDetailAsync(client, session.Id);

        complete.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Status.Should().Be("Completed");
        result.ActualDurationMinutes.Should().BeGreaterThanOrEqualTo(0);
        result.Summary.TotalItems.Should().Be(1);
        result.Summary.PendingItems.Should().Be(1);
        persisted.Items.Single(item => item.Id == pending.Id).Status
            .Should().Be("Pending");
        persisted.Version.Should().Be(detail.Version + 1);
    }

    [Fact]
    public async Task Reflection_ShouldBeOwnerScopedNormalizedAndConcurrencyProtected()
    {
        var owner = await CreateAuthenticatedClientAsync();
        using var client = owner.Client;
        var session = await CreateSessionAsync(client);
        using var start = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/start",
            new StartStudySessionRequest(session.Version));
        var started = await start.Content
            .ReadFromJsonAsync<StartStudySessionResponse>();
        using var complete = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/complete",
            new CompleteStudySessionRequest(started!.Version));
        var completed = await complete.Content
            .ReadFromJsonAsync<CompleteStudySessionResponse>();

        using var update = await client.PutAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/reflection",
            new UpdateStudySessionReflectionRequest(
                "  Revisit IQueryable behavior.  ", completed!.Version));
        var result = await update.Content
            .ReadFromJsonAsync<UpdateStudySessionReflectionResponse>();
        using var stale = await client.PutAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/reflection",
            new UpdateStudySessionReflectionRequest(
                "Overwrite", completed.Version));

        update.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Reflection.Should().Be("Revisit IQueryable behavior.");
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await GetDetailAsync(client, session.Id)).Reflection
            .Should().Be("Revisit IQueryable behavior.");
    }

    [Fact]
    public async Task Complete_ShouldRejectPlannedStaleAndCrossUserSessions()
    {
        var owner = await CreateAuthenticatedClientAsync();
        using var ownerClient = owner.Client;
        var planned = await CreateSessionAsync(ownerClient);
        using var notStarted = await ownerClient.PostAsJsonAsync(
            $"/api/v1/study-sessions/{planned.Id}/complete",
            new CompleteStudySessionRequest(1));
        using var start = await ownerClient.PostAsJsonAsync(
            $"/api/v1/study-sessions/{planned.Id}/start",
            new StartStudySessionRequest(1));
        using var stale = await ownerClient.PostAsJsonAsync(
            $"/api/v1/study-sessions/{planned.Id}/complete",
            new CompleteStudySessionRequest(1));
        var other = await CreateAuthenticatedClientAsync();
        using var otherClient = other.Client;
        using var crossUser = await otherClient.PostAsJsonAsync(
            $"/api/v1/study-sessions/{planned.Id}/complete",
            new CompleteStudySessionRequest(2));

        notStarted.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(notStarted)).Should()
            .Be("STUDY_SESSION_NOT_STARTED");
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(stale)).Should().Be("STUDY_SESSION_CONFLICT");
        crossUser.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_ShouldSupportPlannedInProgressAndIdempotentRetry()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var client = auth.Client;
        var planned = await CreateSessionAsync(client);
        using var cancel = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{planned.Id}/cancel",
            new CancelStudySessionRequest(1));
        var first = await cancel.Content
            .ReadFromJsonAsync<CancelStudySessionResponse>();
        using var retry = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{planned.Id}/cancel",
            new CancelStudySessionRequest(first!.Version));
        var second = await retry.Content
            .ReadFromJsonAsync<CancelStudySessionResponse>();
        var active = await CreateSessionAsync(client);
        using var start = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{active.Id}/start",
            new StartStudySessionRequest(1));
        var started = await GetDetailAsync(client, active.Id);
        using var cancelActive = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{active.Id}/cancel",
            new CancelStudySessionRequest(started.Version));
        var activeResult = await cancelActive.Content
            .ReadFromJsonAsync<CancelStudySessionResponse>();

        first.Status.Should().Be("Cancelled");
        second!.Version.Should().Be(first.Version);
        second.UpdatedAtUtc.Should()
            .BeCloseTo(first.UpdatedAtUtc, TimeSpan.FromMilliseconds(1));
        activeResult!.Status.Should().Be("Cancelled");
        activeResult.StartedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task List_ShouldScopeFilterAndPaginateNewestFirst()
    {
        var owner = await CreateAuthenticatedClientAsync();
        using var client = owner.Client;
        var first = await CreateSessionAsync(client);
        var second = await CreateSessionAsync(client);
        using var cancel = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{first.Id}/cancel",
            new CancelStudySessionRequest(1));
        var other = await CreateAuthenticatedClientAsync();
        using var otherClient = other.Client;
        _ = await CreateSessionAsync(otherClient);

        using var response = await client.GetAsync(
            "/api/v1/study-sessions?status=Planned&page=1&pageSize=1");
        var result = await response.Content.ReadFromJsonAsync<
            PagedResponse<StudySessionListItemResponse>>();
        using var invalid = await client.GetAsync(
            "/api/v1/study-sessions?status=Running");
        using var numeric = await client.GetAsync(
            "/api/v1/study-sessions?status=1");

        result!.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(second.Id);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        numeric.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Detail_ShouldReturnOrderedItemsResourceSummariesAndProgress()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var client = auth.Client;
        var session = await CreateSessionAsync(client);
        var first = await AddProblemItemAsync(client, session.Id, "First");
        var second = await AddProblemItemAsync(client, session.Id, "Second");
        var beforeReorder = await GetDetailAsync(client, session.Id);
        using var reorder = await client.PutAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/items/reorder",
            new ReorderStudySessionItemsRequest(
                [second.Id, first.Id], beforeReorder.Version));
        var reordered = await reorder.Content
            .ReadFromJsonAsync<ReorderStudySessionItemsResponse>();
        using var start = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/start",
            new StartStudySessionRequest(reordered!.Version));
        var started = await start.Content
            .ReadFromJsonAsync<StartStudySessionResponse>();
        using var complete = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/items/{second.Id}/complete",
            new CompleteStudySessionItemRequest("Done", started!.Version));

        var detail = await GetDetailAsync(client, session.Id);

        detail.Items.Select(item => item.Id)
            .Should().Equal(second.Id, first.Id);
        detail.Items[0].ResourceTitle.Should().Be("Second");
        detail.Items.Should().OnlyContain(item => item.IsResourceAvailable);
        detail.Progress.TotalItems.Should().Be(2);
        detail.Progress.CompletedItems.Should().Be(1);
        detail.Progress.PendingItems.Should().Be(1);
        detail.Progress.CompletionPercentage.Should().Be(50);
        detail.Progress.DsaItems.Should().Be(2);
    }

    [Fact]
    public async Task Detail_ShouldHideCrossUserSession()
    {
        var owner = await CreateAuthenticatedClientAsync();
        using var ownerClient = owner.Client;
        var session = await CreateSessionAsync(ownerClient);
        var other = await CreateAuthenticatedClientAsync();
        using var otherClient = other.Client;

        using var response = await otherClient.GetAsync(
            $"/api/v1/study-sessions/{session.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ErrorCodeAsync(response)).Should()
            .Be("STUDY_SESSION_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAndUpdate_ShouldPersistPlannedSessionAndPreserveNoOpTime()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var client = auth.Client;
        var session = await CreateSessionAsync(client);

        using var update = await client.PutAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}",
            new UpdateStudySessionRequest(
                "Updated plan", 75, "Focus", session.Version));
        var changed = await update.Content
            .ReadFromJsonAsync<StudySessionResponse>();
        using var noOp = await client.PutAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}",
            new UpdateStudySessionRequest(
                "Updated plan", 75, "Focus", changed!.Version));
        var unchanged = await noOp.Content
            .ReadFromJsonAsync<StudySessionResponse>();

        session.Status.Should().Be("Planned");
        changed.Title.Should().Be("Updated plan");
        unchanged!.UpdatedAtUtc.Should()
            .BeCloseTo(changed.UpdatedAtUtc, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task AddItem_ShouldEnforceOwnershipArchiveAndDuplicateRules()
    {
        var owner = await CreateAuthenticatedClientAsync();
        using var ownerClient = owner.Client;
        var session = await CreateSessionAsync(ownerClient);
        var problem = await CreateProblemAsync(ownerClient, "Owned");
        using var added = await ownerClient.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/items",
            new AddStudySessionItemRequest(
                "dsa_problem", problem.Id, "No hints", session.Version));
        var addedItem = await added.Content
            .ReadFromJsonAsync<StudySessionItemResponse>();
        using var duplicate = await ownerClient.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/items",
            new AddStudySessionItemRequest(
                "DsaProblem", problem.Id, null, addedItem!.Version));
        var other = await CreateAuthenticatedClientAsync();
        using var otherClient = other.Client;
        var otherSession = await CreateSessionAsync(otherClient);
        using var crossUser = await otherClient.PostAsJsonAsync(
            $"/api/v1/study-sessions/{otherSession.Id}/items",
            new AddStudySessionItemRequest(
                "DsaProblem", problem.Id, null, otherSession.Version));
        using var archive = await ownerClient.PostAsync(
            $"/api/v1/dsa-problems/{problem.Id}/archive", null);
        var archivedProblem = await CreateProblemAsync(ownerClient, "Archived");
        using var archiveSecond = await ownerClient.PostAsync(
            $"/api/v1/dsa-problems/{archivedProblem.Id}/archive", null);
        using var archived = await ownerClient.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/items",
            new AddStudySessionItemRequest(
                "DsaProblem", archivedProblem.Id, null,
                addedItem.Version));

        added.StatusCode.Should().Be(HttpStatusCode.Created);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(duplicate)).Should()
            .Be("STUDY_SESSION_ITEM_ALREADY_EXISTS");
        crossUser.StatusCode.Should().Be(HttpStatusCode.NotFound);
        archived.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(archived)).Should()
            .Be("STUDY_RESOURCE_ARCHIVED");
    }

    [Fact]
    public async Task RemoveAndReorder_ShouldMaintainContiguousPositions()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var client = auth.Client;
        var session = await CreateSessionAsync(client);
        var first = await AddProblemItemAsync(client, session.Id, "First");
        var second = await AddProblemItemAsync(client, session.Id, "Second");
        var third = await AddProblemItemAsync(client, session.Id, "Third");

        var beforeRemove = await GetDetailAsync(client, session.Id);
        using var remove = await client.DeleteAsync(
            $"/api/v1/study-sessions/{session.Id}/items/{second.Id}" +
            $"?expectedVersion={beforeRemove.Version}");
        var removed = await remove.Content
            .ReadFromJsonAsync<RemoveStudySessionItemResponse>();
        using var reorder = await client.PutAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/items/reorder",
            new ReorderStudySessionItemsRequest(
                [third.Id, first.Id], removed!.Version));
        var result = await reorder.Content.ReadFromJsonAsync<
            ReorderStudySessionItemsResponse>();

        remove.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Items.Select(item => (item.Id, item.Position))
            .Should().Equal((third.Id, 0), (first.Id, 1));
    }

    [Fact]
    public async Task Lifecycle_ShouldPersistStartCompleteAndSkipTransitions()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var client = auth.Client;
        var session = await CreateSessionAsync(client);
        var first = await AddProblemItemAsync(client, session.Id, "First");
        var second = await AddProblemItemAsync(client, session.Id, "Second");
        var beforeStart = await GetDetailAsync(client, session.Id);
        using var start = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/start",
            new StartStudySessionRequest(beforeStart.Version));
        var started = await start.Content
            .ReadFromJsonAsync<StartStudySessionResponse>();
        using var startItem = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/items/{first.Id}/start",
            new StartStudySessionItemRequest(started!.Version));
        var firstStarted = await startItem.Content
            .ReadFromJsonAsync<StudySessionItemStateResponse>();
        using var complete = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/items/{first.Id}/complete",
            new CompleteStudySessionItemRequest(
                "Done", firstStarted!.Version));
        var firstCompleted = await complete.Content
            .ReadFromJsonAsync<StudySessionItemStateResponse>();
        using var skip = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/items/{second.Id}/skip",
            new SkipStudySessionItemRequest(
                "Later", firstCompleted!.Version));

        start.StatusCode.Should().Be(HttpStatusCode.OK);
        startItem.StatusCode.Should().Be(HttpStatusCode.OK);
        complete.StatusCode.Should().Be(HttpStatusCode.OK);
        skip.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<DevRecallDbContext>();
        var persisted = await context.StudySessions.AsNoTracking()
            .Include(candidate => candidate.Items)
            .SingleAsync(candidate => candidate.Id == session.Id);
        persisted.Status.Should().Be(StudySessionStatus.Completed);
        persisted.Items.Single(item => item.Id == first.Id).Status
            .Should().Be(StudySessionItemStatus.Completed);
        persisted.Items.Single(item => item.Id == second.Id).Status
            .Should().Be(StudySessionItemStatus.Skipped);
    }

    [Fact]
    public async Task InvalidLifecycleAndWrongItem_ShouldReturnStableConflictsAndNotFound()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var client = auth.Client;
        var firstSession = await CreateSessionAsync(client);
        var secondSession = await CreateSessionAsync(client);
        var item = await AddProblemItemAsync(
            client, secondSession.Id, "Wrong session");
        using var startFirst = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{firstSession.Id}/start",
            new StartStudySessionRequest(firstSession.Version));
        using var beforeStart = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{secondSession.Id}/items/{item.Id}/start",
            new StartStudySessionItemRequest(item.Version));
        using var start = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{secondSession.Id}/start",
            new StartStudySessionRequest(item.Version));
        var started = await start.Content
            .ReadFromJsonAsync<StartStudySessionResponse>();
        using var doubleStart = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{secondSession.Id}/start",
            new StartStudySessionRequest(started!.Version));
        using var wrongItem = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{firstSession.Id}/items/{item.Id}/complete",
            new CompleteStudySessionItemRequest(
                null, firstSession.Version + 1));

        beforeStart.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(beforeStart)).Should()
            .Be("STUDY_SESSION_NOT_STARTED");
        doubleStart.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(doubleStart)).Should()
            .Be("STUDY_SESSION_ALREADY_STARTED");
        wrongItem.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ErrorCodeAsync(wrongItem)).Should()
            .Be("STUDY_SESSION_ITEM_NOT_FOUND");
    }

    [Fact]
    public async Task Endpoints_ShouldHideCrossUserSessionAndRequireAuthentication()
    {
        var owner = await CreateAuthenticatedClientAsync();
        using var ownerClient = owner.Client;
        var session = await CreateSessionAsync(ownerClient);
        var other = await CreateAuthenticatedClientAsync();
        using var otherClient = other.Client;
        using var crossUser = await otherClient.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/start",
            new StartStudySessionRequest(session.Version));
        using var anonymous = CreateClient();
        using var unauthorized = await anonymous.PostAsJsonAsync(
            "/api/v1/study-sessions",
            new CreateStudySessionRequest("Study", 30, null));

        crossUser.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(HttpClient Client, RegisterResponse User)>
        CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var email = $"study-{Guid.NewGuid():N}@example.com";
        using var register = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Study User", "Example123!"));
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

    private static async Task<StudySessionResponse> CreateSessionAsync(
        HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/study-sessions",
            new CreateStudySessionRequest(" Evening Study ", 60, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<StudySessionResponse>())!;
    }

    private static async Task<StudySessionItemResponse> AddProblemItemAsync(
        HttpClient client, Guid sessionId, string title)
    {
        var problem = await CreateProblemAsync(client, title);
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{sessionId}/items",
            new AddStudySessionItemRequest(
                "DsaProblem", problem.Id, null,
                (await GetDetailAsync(client, sessionId)).Version));
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<StudySessionItemResponse>())!;
    }

    private static async Task<DsaProblemResponse> CreateProblemAsync(
        HttpClient client, string title)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/dsa-problems",
            new CreateDsaProblemRequest(
                title, $"Description for {title}.", "Easy",
                "Internal", null, ["Array"]));
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<DsaProblemResponse>())!;
    }

    private static async Task<string?> ErrorCodeAsync(
        HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("errorCode", out var code)
            ? code.GetString()
            : null;
    }

    private static async Task<StudySessionDetailResponse> GetDetailAsync(
        HttpClient client, Guid sessionId)
    {
        using var response = await client.GetAsync(
            $"/api/v1/study-sessions/{sessionId}");
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<StudySessionDetailResponse>())!;
    }
}
