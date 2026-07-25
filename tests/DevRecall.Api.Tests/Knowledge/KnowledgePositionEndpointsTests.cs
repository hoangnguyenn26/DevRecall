using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Knowledge;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevRecall.Api.Tests.Knowledge;

[Collection(AuthApiTestSuite.Name)]
public sealed class KnowledgePositionEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task ChangePosition_ShouldReorderWithinSameParent()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var parent = await CreateNodeAsync(client, "Programming");
        await CreateNodeAsync(client, "C#", parent.Id);
        await CreateNodeAsync(client, "Database", parent.Id);
        var architecture = await CreateNodeAsync(client, "Architecture", parent.Id);

        using var response = await ChangePositionAsync(
            client, architecture.Id, parent.Id, 0);
        var tree = await GetTreeAsync(client);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        tree[0].Children.Select(node => node.Title)
            .Should().Equal("Architecture", "C#", "Database");
        AssertContinuousSortOrder(tree[0].Children);
    }

    [Fact]
    public async Task ChangePosition_ShouldNormalizeOldAndNewParents()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var programming = await CreateNodeAsync(client, "Programming");
        await CreateNodeAsync(client, "C#", programming.Id);
        var database = await CreateNodeAsync(client, "Database", programming.Id);
        await CreateNodeAsync(client, "Architecture", programming.Id);
        var interview = await CreateNodeAsync(client, "Interview");
        await CreateNodeAsync(client, "Behavioral", interview.Id);

        using var response = await ChangePositionAsync(
            client, database.Id, interview.Id, 0);
        var tree = await GetTreeAsync(client);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        tree[0].Children.Select(node => node.Title)
            .Should().Equal("C#", "Architecture");
        tree[1].Children.Select(node => node.Title)
            .Should().Equal("Database", "Behavioral");
        AssertContinuousSortOrder(tree[0].Children);
        AssertContinuousSortOrder(tree[1].Children);
    }

    [Fact]
    public async Task ChangePosition_ShouldMoveChildToRoot()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var programming = await CreateNodeAsync(client, "Programming");
        var csharp = await CreateNodeAsync(client, "C#", programming.Id);
        await CreateNodeAsync(client, "Interview");

        using var response = await ChangePositionAsync(
            client, csharp.Id, targetParentId: null, targetIndex: 1);
        var tree = await GetTreeAsync(client);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        tree.Select(node => node.Title)
            .Should().Equal("Programming", "C#", "Interview");
        tree[0].Children.Should().BeEmpty();
        AssertContinuousSortOrder(tree);
    }

    [Fact]
    public async Task ChangePosition_ShouldAllowAppendAtTargetEnd()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var source = await CreateNodeAsync(client, "Source");
        var database = await CreateNodeAsync(client, "Database", source.Id);
        var target = await CreateNodeAsync(client, "Target");
        await CreateNodeAsync(client, "C#", target.Id);
        await CreateNodeAsync(client, "LINQ", target.Id);

        using var response = await ChangePositionAsync(
            client, database.Id, target.Id, 2);
        var tree = await GetTreeAsync(client);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        tree[1].Children.Select(node => node.Title)
            .Should().Equal("C#", "LINQ", "Database");
    }

    [Fact]
    public async Task ChangePosition_WithDeepCycle_ShouldReturnConflict()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var root = await CreateNodeAsync(client, "A");
        var child = await CreateNodeAsync(client, "B", root.Id);
        var grandchild = await CreateNodeAsync(client, "C", child.Id);

        using var response = await ChangePositionAsync(
            client, root.Id, grandchild.Id, 0);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        var tree = await GetTreeAsync(client);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("KNOWLEDGE_CIRCULAR_HIERARCHY");
        tree.Should().ContainSingle();
        tree[0].Title.Should().Be("A");
        tree[0].Children.Should().ContainSingle();
        tree[0].Children[0].Title.Should().Be("B");
        tree[0].Children[0].Children.Should().ContainSingle();
        tree[0].Children[0].Children[0].Title.Should().Be("C");
    }

    [Fact]
    public async Task ChangePosition_WithInvalidTargetIndex_ShouldReturnBadRequest()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var source = await CreateNodeAsync(client, "Source");
        var node = await CreateNodeAsync(client, "Node", source.Id);
        var target = await CreateNodeAsync(client, "Target");
        await CreateNodeAsync(client, "Child", target.Id);

        using var response = await ChangePositionAsync(
            client, node.Id, target.Id, 2);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePosition_RootToChild_ShouldPreserveMovedSubtree()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var rootA = await CreateNodeAsync(client, "Root A");
        var a1 = await CreateNodeAsync(client, "A1", rootA.Id);
        await CreateNodeAsync(client, "A1.1", a1.Id);
        var rootB = await CreateNodeAsync(client, "Root B");
        await CreateNodeAsync(client, "B1", rootB.Id);
        await CreateNodeAsync(client, "B2", rootB.Id);

        using var response = await ChangePositionAsync(
            client, rootB.Id, a1.Id, 1);
        var tree = await GetTreeAsync(client);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        tree.Should().ContainSingle(node => node.Id == rootA.Id);
        var movedRoot = tree[0].Children[0].Children[1];
        movedRoot.Id.Should().Be(rootB.Id);
        movedRoot.Children.Select(node => node.Title).Should().Equal("B1", "B2");
    }

    [Fact]
    public async Task ChangePosition_WithCrossUserParent_ShouldReturnNotFound()
    {
        using var firstClient = await CreateAuthenticatedClientAsync();
        var foreignParent = await CreateNodeAsync(firstClient, "Foreign Parent");
        using var secondClient = await CreateAuthenticatedClientAsync();
        var node = await CreateNodeAsync(secondClient, "Owned Node");

        using var response = await ChangePositionAsync(
            secondClient, node.Id, foreignParent.Id, 0);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("KNOWLEDGE_PARENT_NOT_FOUND");
    }

    [Fact]
    public async Task ChangePosition_WithArchivedNode_ShouldReturnConflict()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var node = await CreateNodeAsync(client, "Archived Node");
        await client.PostAsync(
            $"/api/v1/knowledge-nodes/{node.Id}/archive",
            content: null);

        using var response = await ChangePositionAsync(
            client, node.Id, targetParentId: null, targetIndex: 0);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("KNOWLEDGE_NODE_ARCHIVED");
    }

    [Fact]
    public async Task ChangePosition_WithArchivedParent_ShouldReturnConflict()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var parent = await CreateNodeAsync(client, "Archived Parent");
        var node = await CreateNodeAsync(client, "Node");
        await client.PostAsync(
            $"/api/v1/knowledge-nodes/{parent.Id}/archive",
            content: null);

        using var response = await ChangePositionAsync(
            client, node.Id, parent.Id, 0);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("KNOWLEDGE_INVALID_PARENT");
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var email = $"position-{Guid.NewGuid():N}@example.com";

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Position User", "Example123!"));
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

    private static Task<HttpResponseMessage> ChangePositionAsync(
        HttpClient client,
        Guid nodeId,
        Guid? targetParentId,
        int targetIndex) =>
        client.PutAsJsonAsync(
            $"/api/v1/knowledge-nodes/{nodeId}/position",
            new ChangeKnowledgeNodePositionRequest(targetParentId, targetIndex));

    private static async Task<IReadOnlyList<KnowledgeTreeNodeResponse>>
        GetTreeAsync(HttpClient client)
    {
        using var response = await client.GetAsync(
            "/api/v1/knowledge-nodes/tree");
        response.EnsureSuccessStatusCode();
        return (await response.Content
            .ReadFromJsonAsync<IReadOnlyList<KnowledgeTreeNodeResponse>>())!;
    }

    private static void AssertContinuousSortOrder(
        IReadOnlyList<KnowledgeTreeNodeResponse> nodes)
    {
        nodes.Select(node => node.SortOrder)
            .Should().Equal(Enumerable.Range(0, nodes.Count));
    }
}
