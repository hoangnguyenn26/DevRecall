using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Knowledge;
using DevRecall.Contracts.Search;
using FluentAssertions;

namespace DevRecall.Api.Tests.Search;

[Collection(AuthApiTestSuite.Name)]
public sealed class SearchEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task Search_ShouldReturnOnlyOwnedResources()
    {
        using var owner = await CreateAuthenticatedClientAsync();
        using var other = await CreateAuthenticatedClientAsync();
        await CreateKnowledgeAsync(owner, "Owner isolation marker");
        await CreateKnowledgeAsync(other, "Other isolation marker");

        var result = await SearchAsync(owner, "isolation marker");

        result.Results.Should().ContainSingle();
        result.Results[0].Title.Should().Be("Owner isolation marker");
    }

    [Fact]
    public async Task Search_ShouldRankExactTitleBeforePrefixTitle()
    {
        using var client = await CreateAuthenticatedClientAsync();
        await CreateKnowledgeAsync(client, "Dependency Injection");
        await CreateKnowledgeAsync(client, "Dependency Injection Lifetimes");

        var result = await SearchAsync(client, "dependency injection");

        result.Results.Should().HaveCount(2);
        result.Results[0].Title.Should().Be("Dependency Injection");
    }

    [Fact]
    public async Task Search_ShouldBeBoundedPerTypeAndReportMore()
    {
        using var client = await CreateAuthenticatedClientAsync();
        await CreateKnowledgeAsync(client, "Bounded search alpha");
        await CreateKnowledgeAsync(client, "Bounded search beta");

        var result = await SearchAsync(client, "bounded search", 1);

        result.Results.Should().ContainSingle();
        result.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task Search_ShouldReturnSafeProjectionAndTrustedTarget()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var node = await CreateKnowledgeAsync(client, "Projection marker");

        var result = await SearchAsync(client, "projection marker");

        var item = result.Results.Should().ContainSingle().Subject;
        item.ResourceType.Should().Be("Knowledge");
        item.TargetPath.Should().Be($"/app/knowledge/{node.Id}");
        item.Summary.Should().BeNull();
        item.Highlights.Should().BeEmpty();
    }

    [Theory]
    [InlineData("a", 5)]
    [InlineData("valid", 0)]
    [InlineData("valid", 11)]
    public async Task Search_WithInvalidInput_ShouldReturnValidationProblem(
        string query, int takePerType)
    {
        using var client = await CreateAuthenticatedClientAsync();
        using var response = await client.GetAsync(
            $"/api/v1/search?q={query}&takePerType={takePerType}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var response = await CreateClient().GetAsync("/api/v1/search?q=dependency");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<GlobalSearchResponse> SearchAsync(
        HttpClient client, string query, int takePerType = 5)
    {
        using var response = await client.GetAsync(
            $"/api/v1/search?q={Uri.EscapeDataString(query)}&takePerType={takePerType}");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the API returned {0}", body);
        return (await response.Content.ReadFromJsonAsync<GlobalSearchResponse>())!;
    }

    private static async Task<KnowledgeNodeResponse> CreateKnowledgeAsync(
        HttpClient client, string title)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/knowledge-nodes", new CreateKnowledgeNodeRequest(title, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<KnowledgeNodeResponse>())!;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"search-{suffix}@devrecall.local";
        const string password = "Search-tests-password-123!";
        using var register = await client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterRequest(email, "Search Tester", password));
        register.EnsureSuccessStatusCode();
        using var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(email, password));
        login.EnsureSuccessStatusCode();
        return client;
    }

    private HttpClient CreateClient() => factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
}
