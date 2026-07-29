using System.Net;
using System.Net.Http.Json;
using DevRecall.Api.Tests.Infrastructure;
using DevRecall.Contracts.Analytics;
using DevRecall.Contracts.Auth;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Api.Tests.Analytics;

[Collection(AuthApiTestSuite.Name)]
public sealed class ModuleBreakdownEndpointsTests(AuthApiFactory factory)
{
    private static readonly DateTimeOffset From =
        new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = From.AddDays(7);

    [Fact]
    public async Task Get_ShouldRequireAuthentication()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(
            "/api/v1/analytics/module-breakdown");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_EmptyRange_ShouldReturnFourZeroModules()
    {
        var auth = await CreateAuthenticatedClientAsync();
        using var client = auth.Client;

        using var response = await client.GetAsync(BuildUrl());
        var result = await response.Content
            .ReadFromJsonAsync<ModuleBreakdownResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.TotalCompletedItems.Should().Be(0);
        result.Modules.Select(module => module.ResourceType).Should().Equal(
            "KnowledgeNode", "InterviewQuestion", "DsaProblem", "ReviewItem");
        result.Modules.Should().OnlyContain(module =>
            module.CompletedItems == 0 && module.Percentage == 0m);
    }

    [Fact]
    public async Task Get_ShouldCountOnlyOwnedCompletedItemsInHalfOpenRange()
    {
        var owner = await CreateAuthenticatedClientAsync();
        using var client = owner.Client;
        var other = await CreateAuthenticatedClientAsync();
        using var otherClient = other.Client;
        await SeedItemsAsync(owner.User.Id, other.User.Id);

        using var response = await client.GetAsync(BuildUrl());
        var result = await response.Content
            .ReadFromJsonAsync<ModuleBreakdownResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.TotalCompletedItems.Should().Be(4);
        result.Modules.Should().Equal(
            new ModuleBreakdownItemResponse("KnowledgeNode", 3, 75m),
            new ModuleBreakdownItemResponse("InterviewQuestion", 0, 0m),
            new ModuleBreakdownItemResponse("DsaProblem", 1, 25m),
            new ModuleBreakdownItemResponse("ReviewItem", 0, 0m));
    }

    private async Task SeedItemsAsync(Guid ownerId, Guid otherUserId)
    {
        var owner = CreateSessionWithItems(ownerId);
        var other = CreateSessionWithCompletedItems(
            otherUserId, StudyResourceType.KnowledgeNode, 2, From);
        var atUpperBoundary = CreateSessionWithCompletedItems(
            ownerId, StudyResourceType.ReviewItem, 1, To);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<DevRecallDbContext>();
        context.StudySessions.AddRange(owner, other, atUpperBoundary);
        await context.SaveChangesAsync();
    }

    private static StudySession CreateSessionWithItems(Guid userId)
    {
        var session = StudySession.Create(
            Guid.NewGuid(), userId, "Breakdown", 30, null,
            From.AddMinutes(-1));
        var knowledge = Enumerable.Range(0, 3)
            .Select(_ => session.AddItem(
                Guid.NewGuid(), StudyResourceType.KnowledgeNode,
                Guid.NewGuid(), null, From.AddMinutes(-1)))
            .ToArray();
        var dsa = session.AddItem(
            Guid.NewGuid(), StudyResourceType.DsaProblem,
            Guid.NewGuid(), null, From.AddMinutes(-1));
        var skipped = session.AddItem(
            Guid.NewGuid(), StudyResourceType.InterviewQuestion,
            Guid.NewGuid(), null, From.AddMinutes(-1));
        _ = session.AddItem(
            Guid.NewGuid(), StudyResourceType.ReviewItem,
            Guid.NewGuid(), null, From.AddMinutes(-1));
        var inProgress = session.AddItem(
            Guid.NewGuid(), StudyResourceType.InterviewQuestion,
            Guid.NewGuid(), null, From.AddMinutes(-1));
        session.Start(From);
        foreach (var item in knowledge)
        {
            session.CompleteItem(item.Id, null, From);
        }

        session.CompleteItem(dsa.Id, null, To.AddSeconds(-1));
        session.SkipItem(skipped.Id, null, From.AddDays(1));
        session.StartItem(inProgress.Id, From.AddDays(1));
        return session;
    }

    private static StudySession CreateSessionWithCompletedItems(
        Guid userId, StudyResourceType type, int count,
        DateTimeOffset completedAt)
    {
        var session = StudySession.Create(
            Guid.NewGuid(), userId, "Other breakdown", 30, null,
            completedAt.AddMinutes(-1));
        var items = Enumerable.Range(0, count).Select(_ =>
            session.AddItem(
                Guid.NewGuid(), type, Guid.NewGuid(), null,
                completedAt.AddMinutes(-1))).ToArray();
        session.Start(completedAt.AddSeconds(-1));
        foreach (var item in items)
        {
            session.CompleteItem(item.Id, null, completedAt);
        }

        return session;
    }

    private async Task<(HttpClient Client, RegisterResponse User)>
        CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var email = $"breakdown-{Guid.NewGuid():N}@example.com";
        using var register = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Breakdown User", "Example123!"));
        var user = await register.Content
            .ReadFromJsonAsync<RegisterResponse>();
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

    private static string BuildUrl() =>
        "/api/v1/analytics/module-breakdown"
        + $"?fromUtc={Uri.EscapeDataString(From.ToString("O"))}"
        + $"&toUtc={Uri.EscapeDataString(To.ToString("O"))}";
}
