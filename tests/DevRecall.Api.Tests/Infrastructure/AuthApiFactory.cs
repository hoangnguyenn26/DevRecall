using DevRecall.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace DevRecall.Api.Tests.Infrastructure;

public sealed class AuthApiFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:17")
            .WithDatabase("devrecall_api_tests")
            .WithUsername("devrecall")
            .WithPassword("devrecall_tests_password")
            .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync(CancellationToken.None);

        _ = CreateClient();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<DevRecallDbContext>();

        await dbContext.Database.MigrateAsync(CancellationToken.None);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseEnvironment("Testing")
            .UseSetting(
                "ConnectionStrings:Database",
                _postgres.GetConnectionString());
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<DevRecallDbContext>>();
            services.RemoveAll<DevRecallDbContext>();
            services.AddDbContext<DevRecallDbContext>(options =>
            {
                options
                    .UseNpgsql(_postgres.GetConnectionString())
                    .UseSnakeCaseNamingConvention();
            });
        });
    }
}
