using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
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
    public async Task CreateAndUpdate_ShouldPersistPlannedSessionAndPreserveNoOpTime()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var client = auth.Client;
        var session = await CreateSessionAsync(client);

        using var update = await client.PutAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}",
            new UpdateStudySessionRequest("Updated plan", 75, "Focus"));
        var changed = await update.Content
            .ReadFromJsonAsync<StudySessionResponse>();
        using var noOp = await client.PutAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}",
            new UpdateStudySessionRequest("Updated plan", 75, "Focus"));
        var unchanged = await noOp.Content
            .ReadFromJsonAsync<StudySessionResponse>();

        session.Status.Should().Be("Planned");
        changed!.Title.Should().Be("Updated plan");
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
                "dsa_problem", problem.Id, "No hints"));
        using var duplicate = await ownerClient.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/items",
            new AddStudySessionItemRequest("DsaProblem", problem.Id, null));
        var other = await CreateAuthenticatedClientAsync();
        using var otherClient = other.Client;
        var otherSession = await CreateSessionAsync(otherClient);
        using var crossUser = await otherClient.PostAsJsonAsync(
            $"/api/v1/study-sessions/{otherSession.Id}/items",
            new AddStudySessionItemRequest("DsaProblem", problem.Id, null));
        using var archive = await ownerClient.PostAsync(
            $"/api/v1/dsa-problems/{problem.Id}/archive", null);
        var archivedProblem = await CreateProblemAsync(ownerClient, "Archived");
        using var archiveSecond = await ownerClient.PostAsync(
            $"/api/v1/dsa-problems/{archivedProblem.Id}/archive", null);
        using var archived = await ownerClient.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/items",
            new AddStudySessionItemRequest(
                "DsaProblem", archivedProblem.Id, null));

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

        using var remove = await client.DeleteAsync(
            $"/api/v1/study-sessions/{session.Id}/items/{second.Id}");
        using var reorder = await client.PutAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/items/reorder",
            new ReorderStudySessionItemsRequest([third.Id, first.Id]));
        var result = await reorder.Content.ReadFromJsonAsync<
            ReorderStudySessionItemsResponse>();

        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);
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
        using var start = await client.PostAsync(
            $"/api/v1/study-sessions/{session.Id}/start", null);
        using var startItem = await client.PostAsync(
            $"/api/v1/study-sessions/{session.Id}/items/{first.Id}/start",
            null);
        using var complete = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/items/{first.Id}/complete",
            new CompleteStudySessionItemRequest("Done"));
        using var skip = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{session.Id}/items/{second.Id}/skip",
            new SkipStudySessionItemRequest("Later"));

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
        persisted.Status.Should().Be(StudySessionStatus.InProgress);
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
        using var startFirst = await client.PostAsync(
            $"/api/v1/study-sessions/{firstSession.Id}/start", null);
        using var beforeStart = await client.PostAsync(
            $"/api/v1/study-sessions/{secondSession.Id}/items/{item.Id}/start",
            null);
        using var start = await client.PostAsync(
            $"/api/v1/study-sessions/{secondSession.Id}/start", null);
        using var doubleStart = await client.PostAsync(
            $"/api/v1/study-sessions/{secondSession.Id}/start", null);
        using var wrongItem = await client.PostAsJsonAsync(
            $"/api/v1/study-sessions/{firstSession.Id}/items/{item.Id}/complete",
            new CompleteStudySessionItemRequest(null));

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
        using var crossUser = await otherClient.PostAsync(
            $"/api/v1/study-sessions/{session.Id}/start", null);
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
                "DsaProblem", problem.Id, null));
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
}
