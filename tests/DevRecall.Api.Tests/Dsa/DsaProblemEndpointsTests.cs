using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.Dsa;
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
}
