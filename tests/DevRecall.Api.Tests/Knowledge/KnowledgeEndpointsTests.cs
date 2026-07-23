using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Knowledge;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Api.Tests.Knowledge;

[Collection(AuthApiTestSuite.Name)]
public sealed class KnowledgeEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task CreateRoot_ShouldReturnCreatedAndPersistForAuthenticatedUser()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;

        using var response = await client.PostAsJsonAsync(
            "/api/v1/knowledge-nodes",
            new CreateKnowledgeNodeRequest("Programming", null));
        var result = await response.Content
            .ReadFromJsonAsync<KnowledgeNodeResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Should().NotBeNull();
        result!.ParentId.Should().BeNull();
        result.Title.Should().Be("Programming");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<DevRecallDbContext>();
        var persisted = await dbContext.KnowledgeNodes
            .AsNoTracking()
            .SingleAsync(node => node.Id == result.Id);

        persisted.UserId.Should().Be(session.User.Id);
    }

    [Fact]
    public async Task CreateChild_WithValidParent_ShouldReturnCreated()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;
        var parent = await CreateNodeAsync(client, "Programming", null);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/knowledge-nodes",
            new CreateKnowledgeNodeRequest("C#", parent.Id));
        var child = await response.Content
            .ReadFromJsonAsync<KnowledgeNodeResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        child.Should().NotBeNull();
        child!.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task Create_WithMissingParent_ShouldReturnNotFound()
    {
        var session = await CreateAuthenticatedClientAsync();
        using var client = session.Client;

        using var response = await client.PostAsJsonAsync(
            "/api/v1/knowledge-nodes",
            new CreateKnowledgeNodeRequest("Child", Guid.NewGuid()));
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("KNOWLEDGE_PARENT_NOT_FOUND");
    }

    [Fact]
    public async Task Create_WithAnotherUsersParent_ShouldReturnNotFound()
    {
        var firstSession = await CreateAuthenticatedClientAsync();
        using var firstClient = firstSession.Client;
        var parent = await CreateNodeAsync(
            firstClient,
            "First user's node",
            null);

        var secondSession = await CreateAuthenticatedClientAsync();
        using var secondClient = secondSession.Client;

        using var response = await secondClient.PostAsJsonAsync(
            "/api/v1/knowledge-nodes",
            new CreateKnowledgeNodeRequest("Invalid child", parent.Id));
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("KNOWLEDGE_PARENT_NOT_FOUND");
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/knowledge-nodes",
            new CreateKnowledgeNodeRequest("Programming", null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Location.Should().BeNull();
    }

    private async Task<(HttpClient Client, RegisterResponse User)>
        CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var email = $"knowledge-{Guid.NewGuid():N}@example.com";

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Knowledge User", "Example123!"));
        var user = await registerResponse.Content
            .ReadFromJsonAsync<RegisterResponse>();
        registerResponse.EnsureSuccessStatusCode();

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Example123!"));
        loginResponse.EnsureSuccessStatusCode();

        return (client, user!);
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    private static async Task<KnowledgeNodeResponse> CreateNodeAsync(
        HttpClient client,
        string title,
        Guid? parentId)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/knowledge-nodes",
            new CreateKnowledgeNodeRequest(title, parentId));
        response.EnsureSuccessStatusCode();

        return (await response.Content
            .ReadFromJsonAsync<KnowledgeNodeResponse>())!;
    }
}
