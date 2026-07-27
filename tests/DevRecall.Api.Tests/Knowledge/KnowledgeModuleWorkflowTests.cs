using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Knowledge;
using DevRecall.Contracts.Tags;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevRecall.Api.Tests.Knowledge;

[Collection(AuthApiTestSuite.Name)]
public sealed class KnowledgeModuleWorkflowTests(AuthApiFactory factory)
{
    [Fact]
    public async Task CompleteKnowledgeWorkflow_ShouldPersistAndQueryFinalState()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var dotnet = await CreateNodeAsync(client, ".NET");
        var csharp = await CreateNodeAsync(client, "C#", dotnet.Id);
        var note = await CreateNodeAsync(client, "TryGetValue", csharp.Id);
        var originalDetail = await GetDetailAsync(client, note.Id);

        using var contentResponse = await client.PutAsJsonAsync(
            $"/api/v1/knowledge-nodes/{note.Id}/content",
            new UpdateKnowledgeContentRequest(
                "dictionary.TryGetValue(key, out var value);",
                originalDetail.UpdatedAtUtc));
        using var metadataResponse = await client.PutAsJsonAsync(
            $"/api/v1/knowledge-nodes/{note.Id}/metadata",
            new UpdateKnowledgeMetadataRequest(
                "Safe dictionary lookup pattern.",
                "https://learn.microsoft.com/"));
        var interview = await CreateTagAsync(client, "Interview");
        var important = await CreateTagAsync(client, "Important");
        await AssignTagAsync(client, note.Id, interview.Id);
        await AssignTagAsync(client, note.Id, important.Id);

        var detail = await GetDetailAsync(client, note.Id);
        var filtered = await GetByTagsAsync(client, interview.Id, important.Id);
        using var positionResponse = await client.PutAsJsonAsync(
            $"/api/v1/knowledge-nodes/{note.Id}/position",
            new ChangeKnowledgeNodePositionRequest(null, 0));
        var tree = await GetTreeAsync(client);

        contentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        metadataResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        detail.Content.Should().Be("dictionary.TryGetValue(key, out var value);");
        detail.Description.Should().Be("Safe dictionary lookup pattern.");
        detail.Tags.Select(tag => tag.Name)
            .Should().Equal("Important", "Interview");
        filtered.Should().ContainSingle(node => node.Id == note.Id);
        positionResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        tree[0].Id.Should().Be(note.Id);
        tree[0].SortOrder.Should().Be(0);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var email = $"knowledge-module-{Guid.NewGuid():N}@example.com";

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Knowledge Module User", "Example123!"));
        registerResponse.EnsureSuccessStatusCode();
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Example123!"));
        loginResponse.EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<KnowledgeNodeResponse> CreateNodeAsync(
        HttpClient client,
        string title,
        Guid? parentId = null)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/knowledge-nodes",
            new CreateKnowledgeNodeRequest(title, parentId));
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<KnowledgeNodeResponse>())!;
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

    private static async Task AssignTagAsync(
        HttpClient client,
        Guid nodeId,
        Guid tagId)
    {
        using var response = await client.PutAsync(
            $"/api/v1/knowledge-nodes/{nodeId}/tags/{tagId}",
            content: null);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<KnowledgeNodeDetailResponse> GetDetailAsync(
        HttpClient client,
        Guid nodeId)
    {
        using var response = await client.GetAsync(
            $"/api/v1/knowledge-nodes/{nodeId}");
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<KnowledgeNodeDetailResponse>())!;
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

    private static async Task<IReadOnlyList<KnowledgeTreeNodeResponse>>
        GetTreeAsync(HttpClient client)
    {
        using var response = await client.GetAsync(
            "/api/v1/knowledge-nodes/tree");
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<IReadOnlyList<KnowledgeTreeNodeResponse>>())!;
    }
}
