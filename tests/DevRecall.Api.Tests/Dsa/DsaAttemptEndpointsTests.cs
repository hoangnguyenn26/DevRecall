using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.Dsa;
using DevRecall.Contracts.Dsa.Attempts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevRecall.Api.Tests.Dsa;

[Collection(AuthApiTestSuite.Name)]
public sealed class DsaAttemptEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task CreateAttempts_ShouldIncrementNumberAndPreserveCode()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var problem = await CreateProblemAsync(client);
        var code = "public int Solve()\n{\n    return 1;\n}";

        var first = await CreateAttemptAsync(
            client, problem.Id, "Failed", code);
        var second = await CreateAttemptAsync(
            client, problem.Id, "partially solved", code);

        first.AttemptNumber.Should().Be(1);
        second.AttemptNumber.Should().Be(2);
        second.Result.Should().Be("PartiallySolved");
        second.SolutionCode.Should().Be(code);
    }

    [Theory]
    [InlineData("Success", 10, "2026-07-28T03:00:00Z")]
    [InlineData("Solved", -1, "2026-07-28T03:00:00Z")]
    [InlineData("Solved", 10, "2026-07-28T10:00:00+07:00")]
    public async Task Create_WithInvalidInput_ShouldReturnValidationError(
        string result,
        int durationMinutes,
        string attemptedAt)
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var problem = await CreateProblemAsync(client);

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts",
            new CreateDsaAttemptRequest(
                result, null, null, null, null, null, durationMinutes, null,
                DateTimeOffset.Parse(
                    attemptedAt,
                    global::System.Globalization.CultureInfo.InvariantCulture)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ForArchivedProblem_ShouldReturnConflict()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var problem = await CreateProblemAsync(client);
        using var archiveResponse = await client.PostAsync(
            $"/api/v1/dsa-problems/{problem.Id}/archive", null);

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts",
            ValidAttemptRequest("Failed"));
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("DSA_PROBLEM_ARCHIVED");
    }

    [Fact]
    public async Task HistoryDetailAndLatestSuccessful_ShouldReturnExpectedSnapshots()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var problem = await CreateProblemAsync(client);
        await CreateAttemptAsync(client, problem.Id, "Failed", "code 1");
        var second = await CreateAttemptAsync(
            client, problem.Id, "Solved", "full solved code");
        await CreateAttemptAsync(
            client, problem.Id, "PartiallySolved", "code 3");
        var fourth = await CreateAttemptAsync(
            client, problem.Id, "Solved", "latest solved code");

        using var historyResponse = await client.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts");
        var history = await historyResponse.Content.ReadFromJsonAsync<
            PagedResponse<DsaAttemptListItemResponse>>();
        using var solvedResponse = await client.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts?result=solved&page=1&pageSize=1");
        var solved = await solvedResponse.Content.ReadFromJsonAsync<
            PagedResponse<DsaAttemptListItemResponse>>();
        using var detailResponse = await client.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts/{second.Id}");
        var detail = await detailResponse.Content
            .ReadFromJsonAsync<DsaAttemptResponse>();
        using var latestResponse = await client.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts/latest-successful");
        var latest = await latestResponse.Content
            .ReadFromJsonAsync<DsaAttemptResponse>();

        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        history!.Items.Select(item => item.AttemptNumber)
            .Should().Equal(4, 3, 2, 1);
        solved!.TotalCount.Should().Be(2);
        solved.TotalPages.Should().Be(2);
        solved.Items.Should().ContainSingle()
            .Which.AttemptNumber.Should().Be(4);
        detail!.SolutionCode.Should().Be("full solved code");
        detail.Notes.Should().Be("Attempt notes.");
        latest!.Id.Should().Be(fourth.Id);
        latest.AttemptNumber.Should().Be(4);
    }

    [Fact]
    public async Task HistoryList_ShouldNotContainLongSnapshotFields()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var problem = await CreateProblemAsync(client);
        await CreateAttemptAsync(client, problem.Id, "Solved", "secret code");

        using var response = await client.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts");
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        var item = document.RootElement.GetProperty("items")[0];

        item.TryGetProperty("solutionCode", out _).Should().BeFalse();
        item.TryGetProperty("approach", out _).Should().BeFalse();
        item.TryGetProperty("notes", out _).Should().BeFalse();
    }

    [Fact]
    public async Task LatestSuccessful_WhenNoneExists_ShouldReturnNoContent()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var problem = await CreateProblemAsync(client);
        await CreateAttemptAsync(client, problem.Id, "Failed", null);

        using var response = await client.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts/latest-successful");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ArchivedProblemHistory_ShouldRemainReadable()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var problem = await CreateProblemAsync(client);
        var first = await CreateAttemptAsync(
            client, problem.Id, "Failed", null);
        var second = await CreateAttemptAsync(
            client, problem.Id, "Solved", "solved code");
        using var archiveResponse = await client.PostAsync(
            $"/api/v1/dsa-problems/{problem.Id}/archive", null);

        using var response = await client.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts");
        var history = await response.Content.ReadFromJsonAsync<
            PagedResponse<DsaAttemptListItemResponse>>();
        using var compareResponse = await client.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts/compare"
            + $"?leftAttemptId={first.Id}&rightAttemptId={second.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        history!.Items.Should().HaveCount(2);
        compareResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Compare_ShouldReturnFullSnapshotsAndRightMinusLeftDifferences()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var problem = await CreateProblemAsync(client);
        var left = await CreateAttemptAsync(
            client, problem.Id,
            new CreateDsaAttemptRequest(
                "Failed", "C#", "left code", "Nested loops", "O(n²)",
                "O(1)", 35, "Left notes", DateTimeOffset.UtcNow));
        var right = await CreateAttemptAsync(
            client, problem.Id,
            new CreateDsaAttemptRequest(
                "Solved", "c#", "right code", "Dictionary", "O(n)",
                "O(n)", 18, "Right notes", DateTimeOffset.UtcNow));

        using var response = await client.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts/compare"
            + $"?leftAttemptId={left.Id}&rightAttemptId={right.Id}");
        var comparison = await response.Content
            .ReadFromJsonAsync<CompareDsaAttemptsResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        comparison!.Left.SolutionCode.Should().Be("left code");
        comparison.Right.SolutionCode.Should().Be("right code");
        comparison.Difference.AttemptNumberDifference.Should().Be(1);
        comparison.Difference.DurationDifferenceMinutes.Should().Be(-17);
        comparison.Difference.ResultTransition.Should().Be("Failed -> Solved");
        comparison.Difference.LanguageChanged.Should().BeFalse();
        comparison.Difference.SolutionCodeChanged.Should().BeTrue();
        comparison.Difference.NotesChanged.Should().BeTrue();
    }

    [Fact]
    public async Task Compare_WithSameAttempt_ShouldReturnValidationError()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var problem = await CreateProblemAsync(client);
        var attempt = await CreateAttemptAsync(
            client, problem.Id, "Failed", null);

        using var response = await client.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts/compare"
            + $"?leftAttemptId={attempt.Id}&rightAttemptId={attempt.Id}");
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        document.RootElement.GetProperty("errors")
            .TryGetProperty("rightAttemptId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CrossUserAttemptOperations_ShouldReturnProblemNotFound()
    {
        var firstSession = await CreateAuthenticatedClientAsync();
        using var firstClient = firstSession.Client;
        var problem = await CreateProblemAsync(firstClient);
        var attempt = await CreateAttemptAsync(
            firstClient, problem.Id, "Failed", null);
        var secondSession = await CreateAuthenticatedClientAsync();
        using var secondClient = secondSession.Client;

        using var createResponse = await secondClient.PostAsJsonAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts",
            ValidAttemptRequest("Solved"));
        using var historyResponse = await secondClient.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts");
        using var detailResponse = await secondClient.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts/{attempt.Id}");
        using var latestResponse = await secondClient.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts/latest-successful");
        using var compareResponse = await secondClient.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts/compare"
            + $"?leftAttemptId={attempt.Id}&rightAttemptId={Guid.NewGuid()}");

        createResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        historyResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        detailResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        latestResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        compareResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Detail_WithAttemptFromDifferentProblem_ShouldReturnAttemptNotFound()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var firstProblem = await CreateProblemAsync(client);
        var secondProblem = await CreateProblemAsync(client, "Three Sum");
        var attempt = await CreateAttemptAsync(
            client, firstProblem.Id, "Failed", null);

        using var response = await client.GetAsync(
            $"/api/v1/dsa-problems/{secondProblem.Id}/attempts/{attempt.Id}");
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("DSA_ATTEMPT_NOT_FOUND");
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=51")]
    [InlineData("?result=Success")]
    public async Task History_WithInvalidQuery_ShouldReturnValidationError(
        string query)
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var problem = await CreateProblemAsync(client);

        using var response = await client.GetAsync(
            $"/api/v1/dsa-problems/{problem.Id}/attempts{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<(HttpClient Client, RegisterResponse User)>
        CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var email = $"attempt-{Guid.NewGuid():N}@example.com";
        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Attempt User", "Example123!"));
        var user = await registerResponse.Content
            .ReadFromJsonAsync<RegisterResponse>();
        registerResponse.EnsureSuccessStatusCode();
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Example123!"));
        loginResponse.EnsureSuccessStatusCode();
        return (client, user!);
    }

    private static async Task<DsaProblemResponse> CreateProblemAsync(
        HttpClient client,
        string title = "Two Sum")
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/dsa-problems",
            new CreateDsaProblemRequest(
                title, "Find two numbers.", "Easy", "LeetCode",
                null, ["Array", "Hash Table"]));
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<DsaProblemResponse>())!;
    }

    private static async Task<DsaAttemptResponse> CreateAttemptAsync(
        HttpClient client,
        Guid problemId,
        string result,
        string? solutionCode)
        => await CreateAttemptAsync(
            client, problemId, ValidAttemptRequest(result, solutionCode));

    private static async Task<DsaAttemptResponse> CreateAttemptAsync(
        HttpClient client,
        Guid problemId,
        CreateDsaAttemptRequest request)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/dsa-problems/{problemId}/attempts",
            request);
        response.EnsureSuccessStatusCode();
        var attempt = (await response.Content
            .ReadFromJsonAsync<DsaAttemptResponse>())!;
        response.Headers.Location.Should().Be(
            $"/api/v1/dsa-problems/{problemId}/attempts/{attempt.Id}");
        return attempt;
    }

    private static CreateDsaAttemptRequest ValidAttemptRequest(
        string result,
        string? solutionCode = null) =>
        new(
            result, " C# ", solutionCode, "Attempt approach.", "O(n)",
            "O(n)", 20, "Attempt notes.", DateTimeOffset.UtcNow);
}
