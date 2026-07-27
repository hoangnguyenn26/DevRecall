using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Knowledge;
using DevRecall.Contracts.Tags;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Api.Tests.Knowledge.Tags;

[Collection(AuthApiTestSuite.Name)]
public sealed class KnowledgeTagAssignmentTests(AuthApiFactory factory)
{
    [Fact]
    public async Task Assign_ShouldReturnTagInKnowledgeDetail()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var node = await CreateNodeAsync(client, "TryGetValue");
        var tag = await CreateTagAsync(client, "Interview");

        using var response = await client.PutAsync(
            $"/api/v1/knowledge-nodes/{node.Id}/tags/{tag.Id}",
            content: null);
        var detail = await GetDetailAsync(client, node.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        detail.Tags.Should().ContainSingle(item =>
            item.Id == tag.Id && item.Name == "Interview");
    }

    [Fact]
    public async Task AssignTwice_ShouldCreateOnlyOneRelation()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var node = await CreateNodeAsync(client, "TryGetValue");
        var tag = await CreateTagAsync(client, "Interview");
        var route = $"/api/v1/knowledge-nodes/{node.Id}/tags/{tag.Id}";

        using var firstResponse = await client.PutAsync(route, content: null);
        using var secondResponse = await client.PutAsync(route, content: null);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        var relationCount = await context.KnowledgeNodeTags.CountAsync(
            relation => relation.KnowledgeNodeId == node.Id
                && relation.TagId == tag.Id);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        relationCount.Should().Be(1);
    }

    [Fact]
    public async Task RemoveTwice_ShouldBeNoOpAndRemoveTagFromDetail()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var node = await CreateNodeAsync(client, "TryGetValue");
        var tag = await CreateTagAsync(client, "Interview");
        var route = $"/api/v1/knowledge-nodes/{node.Id}/tags/{tag.Id}";
        await client.PutAsync(route, content: null);

        using var firstResponse = await client.DeleteAsync(route);
        using var secondResponse = await client.DeleteAsync(route);
        var detail = await GetDetailAsync(client, node.Id);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        detail.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task Assign_ForeignTag_ShouldReturnNotFound()
    {
        using var firstClient = await CreateAuthenticatedClientAsync();
        var node = await CreateNodeAsync(firstClient, "TryGetValue");
        using var secondClient = await CreateAuthenticatedClientAsync();
        var foreignTag = await CreateTagAsync(secondClient, "Interview");

        using var response = await firstClient.PutAsync(
            $"/api/v1/knowledge-nodes/{node.Id}/tags/{foreignTag.Id}",
            content: null);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("TAG_NOT_FOUND");
    }

    [Fact]
    public async Task Assign_ArchivedTag_ShouldReturnConflict()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var node = await CreateNodeAsync(client, "TryGetValue");
        var tag = await CreateTagAsync(client, "Interview");
        await client.PostAsync($"/api/v1/tags/{tag.Id}/archive", content: null);

        using var response = await client.PutAsync(
            $"/api/v1/knowledge-nodes/{node.Id}/tags/{tag.Id}",
            content: null);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("TAG_ARCHIVED");
    }

    [Fact]
    public async Task Remove_FromArchivedNode_ShouldReturnConflict()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var node = await CreateNodeAsync(client, "TryGetValue");
        var tag = await CreateTagAsync(client, "Interview");
        await client.PutAsync(
            $"/api/v1/knowledge-nodes/{node.Id}/tags/{tag.Id}",
            content: null);
        await client.PostAsync(
            $"/api/v1/knowledge-nodes/{node.Id}/archive",
            content: null);

        using var response = await client.DeleteAsync(
            $"/api/v1/knowledge-nodes/{node.Id}/tags/{tag.Id}");
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("KNOWLEDGE_NODE_ARCHIVED");
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var email = $"assignment-{Guid.NewGuid():N}@example.com";

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Assignment User", "Example123!"));
        registerResponse.EnsureSuccessStatusCode();
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Example123!"));
        loginResponse.EnsureSuccessStatusCode();

        return client;
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
}
