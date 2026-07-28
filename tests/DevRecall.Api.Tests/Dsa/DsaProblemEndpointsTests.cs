using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.Dsa;
using DevRecall.Contracts.Dsa.Attempts;
using DevRecall.Domain.Dsa;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Api.Tests.Dsa;

[Collection(AuthApiTestSuite.Name)]
public sealed class DsaProblemEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task Create_ShouldNormalizeAndPersistForCurrentUser()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;

        using var response = await client.PostAsJsonAsync(
            "/api/v1/dsa-problems",
            new CreateDsaProblemRequest(
                "  Two   Sum ", " Find two numbers. ", "easy",
                " LeetCode ", "https://leetcode.com/problems/two-sum/",
                [" Array ", "array", " Hash   Table "]));
        var result = await response.Content
            .ReadFromJsonAsync<DsaProblemResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should()
            .Be($"/api/v1/dsa-problems/{result!.Id}");
        result.Title.Should().Be("Two Sum");
        result.Difficulty.Should().Be("Easy");
        result.Source.Should().Be("LeetCode");
        result.Topics.Should().Equal("Array", "Hash Table");
        result.Status.Should().Be("Active");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<DevRecallDbContext>();
        var ownerId = await context.DsaProblems
            .Where(problem => problem.Id == result.Id)
            .Select(problem => problem.UserId)
            .SingleAsync();
        ownerId.Should().Be(session.User.Id);
    }

    [Theory]
    [InlineData("Intermediate", null)]
    [InlineData("Easy", "leetcode.com/problems/two-sum")]
    public async Task Create_WithInvalidInput_ShouldReturnValidationError(
        string difficulty,
        string? externalUrl)
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;

        using var response = await client.PostAsJsonAsync(
            "/api/v1/dsa-problems",
            ValidRequest("Invalid problem", difficulty, externalUrl));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithMissingRequiredFields_ShouldReturnValidationErrors()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;

        using var response = await client.PostAsJsonAsync(
            "/api/v1/dsa-problems",
            new CreateDsaProblemRequest(
                " ", "", "Easy", null, null, []));
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        var errors = document.RootElement.GetProperty("errors");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        errors.TryGetProperty("title", out _).Should().BeTrue();
        errors.TryGetProperty("description", out _).Should().BeTrue();

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<DevRecallDbContext>();
        var problemCount = await context.DsaProblems.CountAsync(
            problem => problem.UserId == session.User.Id);
        problemCount.Should().Be(0);
    }

    [Fact]
    public async Task Create_WithNullTopics_ShouldReturnEmptyTopics()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;

        using var response = await client.PostAsJsonAsync(
            "/api/v1/dsa-problems",
            new CreateDsaProblemRequest(
                "Binary Search", "Search a sorted array.", "Easy",
                null, null, null));
        var result = await response.Content
            .ReadFromJsonAsync<DsaProblemResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        result!.Topics.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/dsa-problems", ValidRequest("Two Sum"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListAndDetail_ShouldUseCombinedFiltersAndReturnFullProblem()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        await CreateProblemAsync(
            client, "Two Sum", "Easy", "LeetCode", ["Array"]);
        var expected = await CreateProblemAsync(
            client, "Three Sum", "Medium", "LeetCode",
            ["Array", "Two Pointers"]);
        await CreateProblemAsync(
            client, "Dijkstra", "Hard", "Internal", ["Graph"]);

        using var listResponse = await client.GetAsync(
            "/api/v1/dsa-problems?difficulty=medium&topic=array&source=leetcode");
        var list = await listResponse.Content.ReadFromJsonAsync<
            PagedResponse<DsaProblemListItemResponse>>();
        using var detailResponse = await client.GetAsync(
            $"/api/v1/dsa-problems/{expected.Id}");
        var detail = await detailResponse.Content
            .ReadFromJsonAsync<DsaProblemResponse>();

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        list!.TotalCount.Should().Be(1);
        list.Items.Should().ContainSingle(item => item.Id == expected.Id);
        list.Items[0].Topics.Should().Contain("Array");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        detail!.Description.Should().Be("Description for Three Sum.");
        detail.ExternalUrl.Should().Be(
            "https://example.com/problems/three-sum");
        detail.Topics.Should().BeEquivalentTo(["Array", "Two Pointers"]);
        detail.Status.Should().Be("Active");
    }

    [Fact]
    public async Task List_ShouldReturnOnlyCurrentUsersProblemsAndPagination()
    {
        var firstSession = await CreateAuthenticatedClientAsync();
        using var firstClient = firstSession.Client;
        await CreateProblemAsync(
            firstClient, "First problem", "Easy", "Internal", ["Array"]);
        var secondSession = await CreateAuthenticatedClientAsync();
        using var secondClient = secondSession.Client;
        await CreateProblemAsync(
            secondClient, "Problem A", "Easy", "Internal", ["Array"]);
        await CreateProblemAsync(
            secondClient, "Problem B", "Medium", "Internal", ["Array"]);
        await CreateProblemAsync(
            secondClient, "Problem C", "Hard", "Internal", ["Graph"]);

        using var response = await secondClient.GetAsync(
            "/api/v1/dsa-problems?page=2&pageSize=2");
        var result = await response.Content.ReadFromJsonAsync<
            PagedResponse<DsaProblemListItemResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(2);
        result.Items.Should().ContainSingle();
        result.Items.Should().NotContain(item => item.Title == "First problem");
    }

    [Fact]
    public async Task Detail_ForAnotherUsersProblem_ShouldReturnNotFound()
    {
        var firstSession = await CreateAuthenticatedClientAsync();
        using var firstClient = firstSession.Client;
        var problem = await CreateProblemAsync(
            firstClient, "Owned problem", "Easy", "Internal", ["Array"]);
        var secondSession = await CreateAuthenticatedClientAsync();
        using var secondClient = secondSession.Client;

        using var response = await secondClient.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}");
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("DSA_PROBLEM_NOT_FOUND");
    }

    [Fact]
    public async Task ArchivedProblem_ShouldBeReadableButExcludedFromActiveList()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var problem = DsaProblem.Create(
            Guid.NewGuid(), session.User.Id, "Archived problem",
            "Archived description.", DsaProblemDifficulty.Medium,
            "Internal", null, ["Array"], DateTimeOffset.UtcNow);
        problem.Archive(DateTimeOffset.UtcNow.AddMinutes(1));
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<DevRecallDbContext>();
            context.DsaProblems.Add(problem);
            await context.SaveChangesAsync();
        }

        using var detailResponse = await client.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}");
        var detail = await detailResponse.Content
            .ReadFromJsonAsync<DsaProblemResponse>();
        using var listResponse = await client.GetAsync(
            "/api/v1/dsa-problems");
        var list = await listResponse.Content.ReadFromJsonAsync<
            PagedResponse<DsaProblemListItemResponse>>();

        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        detail!.Status.Should().Be("Archived");
        list!.Items.Should().NotContain(item => item.Id == problem.Id);
    }

    [Fact]
    public async Task Update_AsOwner_ShouldReplaceTopicsAndAffectFilters()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var created = await CreateProblemAsync(
            client, "Two Sum", "Easy", "LeetCode",
            ["Array", "Hash Table"]);
        var persistedBeforeUpdate = await GetDetailAsync(client, created.Id);

        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/dsa-problems/{created.Id}",
            new UpdateDsaProblemRequest(
                "Two Sum Optimized", "Use one pass.", "Hard",
                null, null, ["Array", "Two Pointers"]));
        var updated = await updateResponse.Content
            .ReadFromJsonAsync<DsaProblemResponse>();
        using var filterResponse = await client.GetAsync(
            "/api/v1/dsa-problems?difficulty=Hard&topic=two%20pointers");
        var filtered = await filterResponse.Content.ReadFromJsonAsync<
            PagedResponse<DsaProblemListItemResponse>>();

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updated!.CreatedAtUtc.Should().Be(persistedBeforeUpdate.CreatedAtUtc);
        updated.UpdatedAtUtc.Should()
            .BeAfter(persistedBeforeUpdate.UpdatedAtUtc);
        updated.Source.Should().BeNull();
        updated.ExternalUrl.Should().BeNull();
        updated.Topics.Should().Equal("Array", "Two Pointers");
        filtered!.Items.Should().ContainSingle(item => item.Id == created.Id);
    }

    [Fact]
    public async Task Update_WithEquivalentValues_ShouldPreserveTimestamp()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var created = await CreateProblemAsync(
            client, "Two Sum", "Easy", "LeetCode",
            ["Array", "Hash Table"]);
        var persistedBeforeUpdate = await GetDetailAsync(client, created.Id);

        using var response = await client.PutAsJsonAsync(
            $"/api/v1/dsa-problems/{created.Id}",
            new UpdateDsaProblemRequest(
                " Two   Sum ", " Description for Two Sum. ", "easy",
                " LeetCode ", created.ExternalUrl,
                ["hash table", " ARRAY "]));
        var updated = await response.Content
            .ReadFromJsonAsync<DsaProblemResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        updated!.UpdatedAtUtc.Should().Be(persistedBeforeUpdate.UpdatedAtUtc);
    }

    [Fact]
    public async Task ProblemLifecycle_ShouldArchiveIdempotentlyAndBlockUpdate()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var created = await CreateProblemAsync(
            client, "Lifecycle problem", "Easy", "LeetCode",
            ["Array", "Hash Table"]);
        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/dsa-problems/{created.Id}",
            new UpdateDsaProblemRequest(
                "Updated lifecycle problem", "Updated description.", "Medium",
                "LeetCode", null, ["Array", "Sorting"]));
        using var archiveResponse = await client.PostAsync(
            $"/api/v1/dsa-problems/{created.Id}/archive", null);
        var archived = await GetDetailAsync(client, created.Id);
        using var secondArchiveResponse = await client.PostAsync(
            $"/api/v1/dsa-problems/{created.Id}/archive", null);
        var secondArchived = await GetDetailAsync(client, created.Id);
        using var listResponse = await client.GetAsync(
            "/api/v1/dsa-problems?topic=Sorting");
        var list = await listResponse.Content.ReadFromJsonAsync<
            PagedResponse<DsaProblemListItemResponse>>();
        using var archivedUpdateResponse = await client.PutAsJsonAsync(
            $"/api/v1/dsa-problems/{created.Id}",
            new UpdateDsaProblemRequest(
                "Forbidden", "Forbidden.", "Hard", null, null, []));
        using var errorDocument = JsonDocument.Parse(
            await archivedUpdateResponse.Content.ReadAsStringAsync());

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        archived.Status.Should().Be("Archived");
        archived.Topics.Should().Equal("Array", "Sorting");
        secondArchiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        secondArchived.UpdatedAtUtc.Should().Be(archived.UpdatedAtUtc);
        list!.Items.Should().NotContain(item => item.Id == created.Id);
        archivedUpdateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        errorDocument.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("DSA_PROBLEM_ARCHIVED");
    }

    [Fact]
    public async Task CrossUserUpdateAndArchive_ShouldReturnNotFound()
    {
        var firstSession = await CreateAuthenticatedClientAsync();
        using var firstClient = firstSession.Client;
        var problem = await CreateProblemAsync(
            firstClient, "Owned problem", "Easy", "Internal", ["Array"]);
        var secondSession = await CreateAuthenticatedClientAsync();
        using var secondClient = secondSession.Client;

        using var updateResponse = await secondClient.PutAsJsonAsync(
            $"/api/v1/dsa-problems/{problem.Id}",
            new UpdateDsaProblemRequest(
                "Changed", "Changed.", "Hard", null, null, []));
        using var archiveResponse = await secondClient.PostAsync(
            $"/api/v1/dsa-problems/{problem.Id}/archive", null);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var unchanged = await GetDetailAsync(firstClient, problem.Id);
        unchanged.Title.Should().Be("Owned problem");
        unchanged.Status.Should().Be("Active");
    }

    [Fact]
    public async Task CompleteDetail_WithoutAttempts_ShouldReturnZeroSummary()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var problem = await CreateProblemAsync(
            client, "Empty history", "Easy", "Internal", ["Array"]);

        var detail = await GetCompleteDetailAsync(client, problem.Id);

        detail.AttemptSummary.TotalAttempts.Should().Be(0);
        detail.AttemptSummary.AverageDurationMinutes.Should().Be(0);
        detail.AttemptSummary.LastAttemptedAtUtc.Should().BeNull();
        detail.LatestAttempt.Should().BeNull();
        detail.LatestSuccessfulAttempt.Should().BeNull();
        detail.RecentAttempts.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteDetail_ShouldSummarizeAndLimitRecentAttempts()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var problem = await CreateProblemAsync(
            client, "Progress problem", "Medium", "Internal", ["Graph"]);
        var results = new[]
        {
            ("Solved", 10),
            ("Failed", 20),
            ("PartiallySolved", 30),
            ("Skipped", 0),
            ("Failed", 40),
            ("PartiallySolved", 50),
            ("Failed", 60)
        };
        foreach (var (result, duration) in results)
        {
            using var response = await client.PostAsJsonAsync(
                $"/api/v1/dsa-problems/{problem.Id}/attempts",
                new CreateDsaAttemptRequest(
                    result, "C#", "long code", "approach", "O(n)", "O(n)",
                    duration, "notes", DateTimeOffset.UtcNow));
            response.EnsureSuccessStatusCode();
        }

        using var archiveResponse = await client.PostAsync(
            $"/api/v1/dsa-problems/{problem.Id}/archive", null);
        var detail = await GetCompleteDetailAsync(client, problem.Id);

        detail.Status.Should().Be("Archived");
        detail.AttemptSummary.TotalAttempts.Should().Be(7);
        detail.AttemptSummary.SolvedAttempts.Should().Be(1);
        detail.AttemptSummary.PartiallySolvedAttempts.Should().Be(2);
        detail.AttemptSummary.FailedAttempts.Should().Be(3);
        detail.AttemptSummary.SkippedAttempts.Should().Be(1);
        detail.AttemptSummary.TotalDurationMinutes.Should().Be(210);
        detail.AttemptSummary.AverageDurationMinutes.Should().Be(30);
        detail.LatestAttempt!.AttemptNumber.Should().Be(7);
        detail.LatestSuccessfulAttempt!.AttemptNumber.Should().Be(1);
        detail.RecentAttempts.Select(attempt => attempt.AttemptNumber)
            .Should().Equal(7, 6, 5, 4, 3);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?difficulty=Intermediate")]
    public async Task List_WithInvalidQuery_ShouldReturnValidationError(
        string query)
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;

        using var response = await client.GetAsync(
            $"/api/v1/dsa-problems{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<(HttpClient Client, RegisterResponse User)>
        CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var email = $"dsa-{Guid.NewGuid():N}@example.com";
        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "DSA User", "Example123!"));
        var user = await registerResponse.Content
            .ReadFromJsonAsync<RegisterResponse>();
        registerResponse.EnsureSuccessStatusCode();
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Example123!"));
        loginResponse.EnsureSuccessStatusCode();
        return (client, user!);
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static CreateDsaProblemRequest ValidRequest(
        string title,
        string difficulty = "Easy",
        string? externalUrl = null) =>
        new(
            title, $"Description for {title}.", difficulty, "Internal",
            externalUrl, ["Array"]);

    private static async Task<DsaProblemResponse> CreateProblemAsync(
        HttpClient client,
        string title,
        string difficulty,
        string source,
        IReadOnlyCollection<string> topics)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/dsa-problems",
            new CreateDsaProblemRequest(
                title, $"Description for {title}.", difficulty, source,
                $"https://example.com/problems/{title.ToLowerInvariant().Replace(' ', '-')}",
                topics));
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<DsaProblemResponse>())!;
    }

    private static async Task<DsaProblemResponse> GetDetailAsync(
        HttpClient client,
        Guid id)
    {
        using var response = await client.GetAsync(
            $"/api/v1/dsa-problems/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<DsaProblemResponse>())!;
    }

    private static async Task<DsaProblemDetailResponse> GetCompleteDetailAsync(
        HttpClient client,
        Guid id)
    {
        using var response = await client.GetAsync(
            $"/api/v1/dsa-problems/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<DsaProblemDetailResponse>())!;
    }
}
