using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.Tags;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevRecall.Api.Tests.Knowledge.Tags;

[Collection(AuthApiTestSuite.Name)]
public sealed class TagEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task CreateAndList_ShouldReturnNormalizedActiveTag()
    {
        using var client = await CreateAuthenticatedClientAsync();

        using var createResponse = await client.PostAsJsonAsync(
            "/api/v1/tags",
            new CreateTagRequest("  ASP.NET   Core  "));
        var created = await createResponse.Content.ReadFromJsonAsync<TagResponse>();
        using var listResponse = await client.GetAsync("/api/v1/tags");
        var tags = await listResponse.Content
            .ReadFromJsonAsync<IReadOnlyList<TagResponse>>();

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        created!.Name.Should().Be("ASP.NET Core");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        tags.Should().ContainSingle(tag => tag.Id == created.Id);
    }

    [Fact]
    public async Task Create_WithDuplicateNormalizedName_ShouldReturnConflict()
    {
        using var client = await CreateAuthenticatedClientAsync();
        await CreateTagAsync(client, "Interview");

        using var response = await client.PostAsJsonAsync(
            "/api/v1/tags",
            new CreateTagRequest(" interview "));
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("TAG_NAME_ALREADY_EXISTS");
    }

    [Fact]
    public async Task Rename_ToExistingName_ShouldReturnConflict()
    {
        using var client = await CreateAuthenticatedClientAsync();
        await CreateTagAsync(client, "Interview");
        var backend = await CreateTagAsync(client, "Backend");

        using var response = await client.PutAsJsonAsync(
            $"/api/v1/tags/{backend.Id}",
            new RenameTagRequest(" interview "));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Rename_FromAnotherUser_ShouldReturnNotFound()
    {
        using var firstClient = await CreateAuthenticatedClientAsync();
        var tag = await CreateTagAsync(firstClient, "Interview");
        using var secondClient = await CreateAuthenticatedClientAsync();

        using var response = await secondClient.PutAsJsonAsync(
            $"/api/v1/tags/{tag.Id}",
            new RenameTagRequest("Backend"));
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        document.RootElement.GetProperty("errorCode").GetString()
            .Should().Be("TAG_NOT_FOUND");
    }

    [Fact]
    public async Task ManagementWorkflow_ShouldRenameAndHideArchivedTag()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var interview = await CreateTagAsync(client, "Interview");
        var backend = await CreateTagAsync(client, "Backend");

        using var renameResponse = await client.PutAsJsonAsync(
            $"/api/v1/tags/{backend.Id}",
            new RenameTagRequest(".NET Backend"));
        using var archiveResponse = await client.PostAsync(
            $"/api/v1/tags/{interview.Id}/archive",
            content: null);
        using var secondArchiveResponse = await client.PostAsync(
            $"/api/v1/tags/{interview.Id}/archive",
            content: null);
        using var listResponse = await client.GetAsync("/api/v1/tags");
        var tags = await listResponse.Content
            .ReadFromJsonAsync<IReadOnlyList<TagResponse>>();

        renameResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        secondArchiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        tags!.Select(tag => tag.Name).Should().Equal(".NET Backend");
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/tags",
            new CreateTagRequest("Interview"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var email = $"tags-{Guid.NewGuid():N}@example.com";

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Tag User", "Example123!"));
        registerResponse.EnsureSuccessStatusCode();

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Example123!"));
        loginResponse.EnsureSuccessStatusCode();

        return client;
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

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
}
