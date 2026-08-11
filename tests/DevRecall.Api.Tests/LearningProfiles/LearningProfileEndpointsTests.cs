using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Auth;
using DevRecall.Contracts.LearningProfiles;
using DevRecall.Contracts.Today;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Api.Tests.LearningProfiles;

[Collection(AuthApiTestSuite.Name)]
public sealed class LearningProfileEndpointsTests(AuthApiFactory factory)
{
    [Fact]
    public async Task Get_ShouldReturnValidUnconfiguredProfile()
    {
        using var client = await CreateAuthenticatedClientAsync("missing");
        var profile = await client.GetFromJsonAsync<LearningProfileResponse>("/api/v1/learning-profile");

        profile!.IsConfigured.Should().BeFalse();
        profile.TargetRole.Should().BeNull();
        profile.Technologies.Should().BeEmpty();
        profile.Version.Should().BeNull();
        (await client.GetFromJsonAsync<GetTodayDashboardResponse>("/api/v1/today"))
            .Should().NotBeNull();
    }

    [Fact]
    public async Task Options_ShouldReturnStableValuesAndLabels()
    {
        using var client = await CreateAuthenticatedClientAsync("options");
        var options = await client.GetFromJsonAsync<LearningProfileOptionsResponse>("/api/v1/learning-profile/options");

        options!.TargetRoles.Should().Contain(item => item.Value == "BackendDeveloper" && item.Label == "Backend Developer");
        options.ExperienceLevels.Should().Contain(item => item.Value == "Junior");
        options.TechnologyGroups.SelectMany(group => group.Items)
            .Should().Contain(item => item.Value == "PostgreSql" && item.Label == "PostgreSQL");
        options.Goals.Should().Contain(item => item.Value == "ImproveBackendFundamentals");
        options.StudyTimeOptions.Should().Equal(15, 30, 45, 60, 90, 120);
    }

    [Fact]
    public async Task Put_ShouldCreateUpdateAndKeepVersionForNoOp()
    {
        using var client = await CreateAuthenticatedClientAsync("put");
        await AddCsrfTokenAsync(client);
        var create = Request(null, 45);
        var created = await PutAsync(client, create);
        var noOp = await PutAsync(client, Request(created.Version, 45));
        var updated = await PutAsync(client, Request(noOp.Version, 60));

        created.Version.Should().Be(1);
        created.TargetRole!.Label.Should().Be("Backend Developer");
        created.Technologies.Should().Contain(item => item.Value == "CSharp" && item.Label == "C#");
        created.UpdatedAtUtc.Should().NotBeNull();
        noOp.Version.Should().Be(1);
        updated.Version.Should().Be(2);
        updated.AvailableMinutesPerDay.Should().Be(60);
    }

    [Fact]
    public async Task Put_ShouldRejectStaleVersionAndPreserveOwnerIsolation()
    {
        using var first = await CreateAuthenticatedClientAsync("owner-a");
        using var second = await CreateAuthenticatedClientAsync("owner-b");
        await AddCsrfTokenAsync(first);
        var created = await PutAsync(first, Request(null, 45));

        using var conflict = await first.PutAsJsonAsync("/api/v1/learning-profile", Request(0, 60));
        var secondProfile = await second.GetFromJsonAsync<LearningProfileResponse>("/api/v1/learning-profile");

        created.IsConfigured.Should().BeTrue();
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        secondProfile!.IsConfigured.Should().BeFalse();
    }

    [Theory]
    [InlineData("1", "CSharp", "PrepareForInterviews")]
    [InlineData("BackendDeveloper", "1", "PrepareForInterviews")]
    [InlineData("BackendDeveloper", "CSharp", "1")]
    [InlineData("BackendDeveloper", "COBOL.NET.AI", "PrepareForInterviews")]
    [InlineData("BackendDeveloper", "CSharp", "BecomeWizard")]
    public async Task Put_ShouldRejectNumericEnumStrings(string role, string technology, string goal)
    {
        using var client = await CreateAuthenticatedClientAsync("numeric");
        await AddCsrfTokenAsync(client);
        var request = Request(null, 45) with
        {
            TargetRole = role,
            Technologies = [new(technology, true)],
            Goals = [goal]
        };
        using var response = await client.PutAsJsonAsync("/api/v1/learning-profile", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_ShouldRejectDuplicateTechnologiesAndGoals()
    {
        using var client = await CreateAuthenticatedClientAsync("duplicates");
        await AddCsrfTokenAsync(client);
        var request = Request(null, 45) with
        {
            Technologies = [new("CSharp", true), new("CSharp", false)],
            Goals = ["PrepareForInterviews", "PrepareForInterviews"]
        };
        using var response = await client.PutAsJsonAsync("/api/v1/learning-profile", request);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problem!.Extensions.Should().ContainKey("errors");
    }

    [Fact]
    public async Task Put_ShouldRejectMoreThanFivePrimaryTechnologies()
    {
        using var client = await CreateAuthenticatedClientAsync("primary-limit");
        await AddCsrfTokenAsync(client);
        var request = Request(null, 45) with
        {
            Technologies = [new("CSharp", true), new("DotNet", true), new("AspNetCore", true),
                new("EfCore", true), new("PostgreSql", true), new("Docker", true)]
        };
        using var response = await client.PutAsJsonAsync("/api/v1/learning-profile", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_ShouldNotCreateObservedEvidenceOrMutatePlans()
    {
        using var client = await CreateAuthenticatedClientAsync("boundaries");
        await AddCsrfTokenAsync(client);
        var user = await client.GetFromJsonAsync<CurrentUserResponse>("/api/v1/auth/me");

        var created = await PutAsync(client, Request(null, 45));
        await PutAsync(client, Request(created.Version, 60));

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        (await dbContext.WeakTopicProfiles.CountAsync(item => item.UserId == user!.Id)).Should().Be(0);
        (await dbContext.StudyPlans.CountAsync(item => item.UserId == user!.Id)).Should().Be(0);
        (await dbContext.ReviewHistories.CountAsync(item => item.UserId == user!.Id)).Should().Be(0);
    }

    private static PutLearningProfileRequest Request(int? version, int minutes) => new(
        "BackendDeveloper", "Junior", minutes,
        [new("CSharp", true), new("DotNet", true), new("PostgreSql", false)],
        ["PrepareForInterviews", "ImproveBackendFundamentals"], version);

    private static async Task<LearningProfileResponse> PutAsync(HttpClient client, PutLearningProfileRequest request)
    {
        using var response = await client.PutAsJsonAsync("/api/v1/learning-profile", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LearningProfileResponse>())!;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string prefix)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true
        });
        var email = $"profile-{prefix}-{Guid.NewGuid():N}@example.com";
        using var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Profile Learner", "Example123!"));
        register.EnsureSuccessStatusCode();
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Example123!"));
        login.EnsureSuccessStatusCode();
        return client;
    }

    private static async Task AddCsrfTokenAsync(HttpClient client)
    {
        var token = await client.GetFromJsonAsync<CsrfTokenResponse>("/api/v1/auth/csrf-token");
        client.DefaultRequestHeaders.Remove(token!.HeaderName);
        client.DefaultRequestHeaders.Add(token.HeaderName, token.RequestToken);
    }
}
