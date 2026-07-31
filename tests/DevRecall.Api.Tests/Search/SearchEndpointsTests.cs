using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.Knowledge;
using DevRecall.Contracts.Search;
using FluentAssertions;

namespace DevRecall.Api.Tests.Search;

[Collection(AuthApiTestSuite.Name)]
public sealed class SearchEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task Search_ShouldReturnOwnedRankedResources()
    {
        using var client = CreateClient();
        await AuthenticateAsync(client);
        using var create = await client.PostAsJsonAsync(
            "/api/v1/knowledge-nodes",
            new CreateKnowledgeNodeRequest("Dependency Injection Lifetimes", null));
        create.EnsureSuccessStatusCode();

        using var response = await client.GetAsync(
            "/api/v1/search?q=dependency%20lifetimes&modules=knowledge&page=1&pageSize=10");

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(
            HttpStatusCode.OK, "the API returned {0}", responseBody);
        var result = await response.Content.ReadFromJsonAsync<
            PagedResponse<SearchResultResponse>>();
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle(item =>
            item.ResourceType == "Knowledge"
            && item.Title == "Dependency Injection Lifetimes");
        result.Items[0].Rank.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Search_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var response = await CreateClient().GetAsync(
            "/api/v1/search?q=dependency");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Search_WithInvalidModule_ShouldReturnValidationProblem()
    {
        using var client = CreateClient();
        await AuthenticateAsync(client);
        using var response = await client.GetAsync(
            "/api/v1/search?q=dependency&modules=projectStory");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private HttpClient CreateClient() => factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"search-{suffix}@devrecall.local";
        const string password = "Search-tests-password-123!";
        using var register = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Search Tester", password));
        register.EnsureSuccessStatusCode();
        using var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(email, password));
        login.EnsureSuccessStatusCode();
    }
}
