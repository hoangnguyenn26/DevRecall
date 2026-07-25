using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Knowledge;
using DevRecall.Contracts.Tags;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevRecall.Api.Tests.Knowledge.Tags;

[Collection(AuthApiTestSuite.Name)]
public sealed class KnowledgeByTagsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task FilterByOneTag_ShouldReturnMatchingNodes()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var interview = await CreateTagAsync(client, "Interview");
        var firstNode = await CreateNodeAsync(client, "Middleware");
        var secondNode = await CreateNodeAsync(client, "TryGetValue");
        await AssignAsync(client, firstNode.Id, interview.Id);
        await AssignAsync(client, secondNode.Id, interview.Id);

        var result = await GetByTagsAsync(client, interview.Id);

        result.Select(node => node.Title)
            .Should().Equal("Middleware", "TryGetValue");
    }

    [Fact]
    public async Task FilterByMultipleTags_ShouldUseAndSemantics()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var csharp = await CreateTagAsync(client, "C#");
        var interview = await CreateTagAsync(client, "Interview");
        var tryGetValue = await CreateNodeAsync(client, "TryGetValue");
        var middleware = await CreateNodeAsync(client, "Middleware");
        await AssignAsync(client, tryGetValue.Id, csharp.Id);
        await AssignAsync(client, tryGetValue.Id, interview.Id);
        await AssignAsync(client, middleware.Id, interview.Id);

        var result = await GetByTagsAsync(
            client,
            csharp.Id,
            csharp.Id,
            interview.Id);

        result.Should().ContainSingle();
        result[0].Title.Should().Be("TryGetValue");
        result[0].Tags.Select(tag => tag.Name)
            .Should().Equal("C#", "Interview");
    }

    [Fact]
    public async Task Filter_ShouldExcludeArchivedNodes()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var interview = await CreateTagAsync(client, "Interview");
        var node = await CreateNodeAsync(client, "TryGetValue");
        await AssignAsync(client, node.Id, interview.Id);
        await client.PostAsync(
            $"/api/v1/knowledge-nodes/{node.Id}/archive",
            content: null);

        var result = await GetByTagsAsync(client, interview.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Filter_WithForeignTag_ShouldReturnNotFound()
    {
        using var firstClient = await CreateAuthenticatedClientAsync();
        var foreignTag = await CreateTagAsync(firstClient, "Interview");
        using var secondClient = await CreateAuthenticatedClientAsync();

        using var response = await secondClient.GetAsync(
            $"/api/v1/knowledge-nodes/by-tags?tagIds={foreignTag.Id}");
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("TAG_NOT_FOUND");
    }

    [Fact]
    public async Task Filter_WithValidUnusedTag_ShouldReturnEmptyList()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var tag = await CreateTagAsync(client, "Unused");

        var result = await GetByTagsAsync(client, tag.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Filter_WithoutTags_ShouldReturnValidationError()
    {
        using var client = await CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(
            "/api/v1/knowledge-nodes/by-tags");
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        document.RootElement.GetProperty("errors")
            .GetProperty("tagIds")[0]
            .GetString()
            .Should().Be("At least one tag is required.");
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var email = $"by-tags-{Guid.NewGuid():N}@example.com";

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Filter User", "Example123!"));
        registerResponse.EnsureSuccessStatusCode();
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Example123!"));
        loginResponse.EnsureSuccessStatusCode();

        return client;
    }

    private static async Task<TagResponse> CreateTagAsync(
        HttpClient client,
        string name)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/tags",
            new CreateTagRequest(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TagResponse>())!;
    }

    private static async Task<KnowledgeNodeResponse> CreateNodeAsync(
        HttpClient client,
        string title)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/knowledge-nodes",
            new CreateKnowledgeNodeRequest(title, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<KnowledgeNodeResponse>())!;
    }

    private static async Task AssignAsync(
        HttpClient client,
        Guid nodeId,
        Guid tagId)
    {
        using var response = await client.PutAsync(
            $"/api/v1/knowledge-nodes/{nodeId}/tags/{tagId}",
            content: null);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<IReadOnlyList<KnowledgeNodeListItemResponse>>
        GetByTagsAsync(HttpClient client, params Guid[] tagIds)
    {
        var query = string.Join(
            '&',
            tagIds.Select(tagId => $"tagIds={tagId}"));
        using var response = await client.GetAsync(
            $"/api/v1/knowledge-nodes/by-tags?{query}");
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<IReadOnlyList<KnowledgeNodeListItemResponse>>())!;
    }
}
