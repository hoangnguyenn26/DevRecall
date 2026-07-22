using System.Net;
using System.Text.Json;
using DevRecall.Api.Middleware;
using DevRecall.Contracts.System;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevRecall.Api.Tests.System;

public sealed class SystemEndpointsTests : IClassFixture<SystemApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;
    private readonly SystemApiFactory _factory;

    public SystemEndpointsTests(SystemApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetInfo_ReturnsSystemInformation()
    {
        var beforeRequest = DateTimeOffset.UtcNow;

        using var response = await _client.GetAsync(
            "/api/v1/system/info",
            CancellationToken.None);
        var json = await response.Content.ReadAsStringAsync(CancellationToken.None);
        var systemInfo = JsonSerializer.Deserialize<SystemInfoResponse>(
            json,
            JsonOptions);
        using var document = JsonDocument.Parse(json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        systemInfo.Should().NotBeNull();
        systemInfo!.ApplicationName.Should().Be("DevRecall");
        systemInfo.Version.Should().Be("0.1.0");
        systemInfo.Environment.Should().Be("Development");
        systemInfo.CurrentTimeUtc.Should().BeOnOrAfter(beforeRequest);
        systemInfo.CurrentTimeUtc.Offset.Should().Be(TimeSpan.Zero);
        document.RootElement.TryGetProperty("applicationName", out _).Should().BeTrue();
        document.RootElement.TryGetProperty("currentTimeUtc", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetInfo_FromDevelopmentFrontend_AllowsCorsRequest()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/system/info");
        request.Headers.Add("Origin", "http://localhost:5173");

        using var response = await _client.SendAsync(
            request,
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().ContainSingle("http://localhost:5173");
    }

    [Fact]
    public async Task OpenApi_InDevelopment_IsAvailable()
    {
        using var response = await _client.GetAsync(
            "/openapi/v1.json",
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetInfo_WithoutCorrelationId_ReturnsGeneratedCorrelationId()
    {
        using var response = await _client.GetAsync(
            "/api/v1/system/info",
            CancellationToken.None);

        response.Headers.TryGetValues(
            CorrelationIdMiddleware.HeaderName,
            out var values).Should().BeTrue();
        values.Should().ContainSingle();
        Guid.TryParseExact(values!.Single(), "N", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetInfo_WithCorrelationId_EchoesCorrelationId()
    {
        const string correlationId = "devrecall-local-test-001";
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/system/info");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);

        using var response = await _client.SendAsync(
            request,
            CancellationToken.None);

        response.Headers.GetValues(CorrelationIdMiddleware.HeaderName)
            .Should().ContainSingle(correlationId);
    }

    [Fact]
    public async Task LivenessEndpoint_WithoutDatabase_ReturnsHealthy()
    {
        using var response = await _client.GetAsync(
            "/health/live",
            CancellationToken.None);
        var content = await response.Content.ReadAsStringAsync(CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("Healthy");
    }

    [Fact]
    public void ReadinessHealthCheck_IsRegisteredForPostgreSql()
    {
        var options = _factory.Services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value;
        var registration = options.Registrations
            .Single(check => check.Name == "postgresql");

        registration.Tags.Should().ContainSingle("ready");
    }
}

public sealed class SystemApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Database"] =
                        "Host=127.0.0.1;Port=1;Database=dummy;Username=dummy;Password=dummy;Timeout=1"
                });
        });
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }
}
