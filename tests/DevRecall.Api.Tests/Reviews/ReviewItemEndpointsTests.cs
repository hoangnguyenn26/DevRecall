using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.Dsa;
using DevRecall.Contracts.Reviews;
using DevRecall.Domain.Reviews;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Api.Tests.Reviews;

[Collection(AuthApiTestSuite.Name)]
public sealed class ReviewItemEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task Create_ShouldAddActiveItemDueImmediatelyAndRejectDuplicate()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var problem = await CreateProblemAsync(client, "Two Sum");
        var request = new CreateReviewItemRequest(
            "dsa problem", problem.Id);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/review-items", request);
        var created = await response.Content
            .ReadFromJsonAsync<CreateReviewItemResponse>();
        using var duplicate = await client.PostAsJsonAsync(
            "/api/v1/review-items", request);
        var duplicateCode = await ReadErrorCodeAsync(duplicate);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should()
            .Be($"/api/v1/review-items/{created!.Id}");
        created.ResourceType.Should().Be("DsaProblem");
        created.Status.Should().Be("Active");
        created.LastReviewedAtUtc.Should().BeNull();
        created.IntervalDays.Should().Be(0);
        created.ReviewCount.Should().Be(0);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        duplicateCode.Should().Be("REVIEW_ITEM_ALREADY_EXISTS");
    }

    [Fact]
    public async Task Create_ShouldProtectOwnershipAndArchivedResource()
    {
        var owner = await CreateAuthenticatedClientAsync();
        using var ownerClient = owner.Client;
        var problem = await CreateProblemAsync(ownerClient, "Owned Problem");
        var other = await CreateAuthenticatedClientAsync();
        using var otherClient = other.Client;

        using var crossUser = await otherClient.PostAsJsonAsync(
            "/api/v1/review-items",
            new CreateReviewItemRequest("DsaProblem", problem.Id));
        using var archive = await ownerClient.PostAsync(
            $"/api/v1/dsa-problems/{problem.Id}/archive", null);
        using var archived = await ownerClient.PostAsJsonAsync(
            "/api/v1/review-items",
            new CreateReviewItemRequest("DsaProblem", problem.Id));

        archive.EnsureSuccessStatusCode();
        crossUser.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ReadErrorCodeAsync(crossUser))
            .Should().Be("REVIEW_RESOURCE_NOT_FOUND");
        archived.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadErrorCodeAsync(archived))
            .Should().Be("REVIEW_RESOURCE_ARCHIVED");
    }

    [Theory]
    [InlineData("", "00000000-0000-0000-0000-000000000001")]
    [InlineData("Algorithm", "00000000-0000-0000-0000-000000000001")]
    [InlineData("DsaProblem", "00000000-0000-0000-0000-000000000000")]
    public async Task Create_WithInvalidRequest_ShouldReturnValidationProblem(
        string resourceType, string resourceId)
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;

        using var response = await client.PostAsJsonAsync(
            "/api/v1/review-items",
            new CreateReviewItemRequest(resourceType, Guid.Parse(resourceId)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Due_ShouldFilterOrderPaginateAndExcludeArchivedItems()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var first = await CreateProblemAsync(client, "First");
        var second = await CreateProblemAsync(client, "Second");
        var future = await CreateProblemAsync(client, "Future");
        var archived = await CreateProblemAsync(client, "Archived");
        var now = DateTimeOffset.UtcNow;

        await AddReviewItemsAsync(
            session.User.Id,
            [
                (first.Id, now.AddHours(-3), false),
                (second.Id, now.AddHours(-1), false),
                (future.Id, now.AddDays(1), false),
                (archived.Id, now.AddDays(-1), true)
            ]);

        using var response = await client.GetAsync(
            "/api/v1/review-items/due?resourceType=dsa_problem&page=1&pageSize=1");
        var result = await response.Content.ReadFromJsonAsync<
            PagedResponse<DueReviewItemResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.TotalCount.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.Items.Should().ContainSingle();
        result.Items[0].ResourceId.Should().Be(first.Id);
        result.Items[0].ResourceTitle.Should().Be("First");
        result.Items[0].ResourcePreview.Should().Be("Description for First.");
        result.Items[0].OverdueMinutes.Should().BeGreaterThanOrEqualTo(180);
    }

    [Fact]
    public async Task Endpoints_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();

        using var create = await client.PostAsJsonAsync(
            "/api/v1/review-items",
            new CreateReviewItemRequest("DsaProblem", Guid.NewGuid()));
        using var due = await client.GetAsync("/api/v1/review-items/due");

        create.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        due.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task AddReviewItemsAsync(
        Guid userId,
        IEnumerable<(Guid ResourceId, DateTimeOffset DueAt, bool Archived)> rows)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<DevRecallDbContext>();
        foreach (var row in rows)
        {
            var item = ReviewItem.Create(
                Guid.NewGuid(), userId, ReviewResourceType.DsaProblem,
                row.ResourceId, row.DueAt, row.DueAt);
            if (row.Archived)
            {
                item.Archive(row.DueAt.AddMinutes(1));
            }

            context.ReviewItems.Add(item);
        }

        await context.SaveChangesAsync();
    }

    private async Task<(HttpClient Client, RegisterResponse User)>
        CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var email = $"review-{Guid.NewGuid():N}@example.com";
        using var register = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Review User", "Example123!"));
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

    private static async Task<string?> ReadErrorCodeAsync(
        HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("errorCode", out var code)
            ? code.GetString()
            : null;
    }
}
