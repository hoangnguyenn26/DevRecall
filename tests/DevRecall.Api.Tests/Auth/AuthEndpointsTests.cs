using System.Net;
using DevRecall.Api.Tests.System;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevRecall.Api.Tests.Auth;

public sealed class AuthEndpointsTests : IClassFixture<SystemApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(SystemApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetCurrentUser_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var response = await _client.GetAsync(
            "/api/v1/auth/me",
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task Logout_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var response = await _client.PostAsync(
            "/api/v1/auth/logout",
            null,
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentUser_FromDevelopmentFrontend_AllowsCredentials()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/auth/me");
        request.Headers.Add("Origin", "http://localhost:5173");

        using var response = await _client.SendAsync(
            request,
            CancellationToken.None);

        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().ContainSingle("http://localhost:5173");
        response.Headers.GetValues("Access-Control-Allow-Credentials")
            .Should().ContainSingle("true");
    }
}
