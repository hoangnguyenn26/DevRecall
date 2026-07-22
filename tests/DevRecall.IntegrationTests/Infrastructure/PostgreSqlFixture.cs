using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DevRecall.IntegrationTests.Infrastructure;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17")
            .WithDatabase("devrecall_tests")
            .WithUsername("devrecall")
            .WithPassword("devrecall_tests_password")
            .Build();

    public string ConnectionString =>
        _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync(CancellationToken.None);

        await using var context = CreateDbContext();

        await context.Database.MigrateAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public DevRecallDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<DevRecallDbContext>()
                .UseNpgsql(ConnectionString)
                .UseSnakeCaseNamingConvention()
                .Options;

        return new DevRecallDbContext(options);
    }
}
