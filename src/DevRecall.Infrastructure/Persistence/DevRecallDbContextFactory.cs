using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DevRecall.Infrastructure.Persistence;

public sealed class DevRecallDbContextFactory
    : IDesignTimeDbContextFactory<DevRecallDbContext>
{
    public DevRecallDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException(
                "Connection string 'Database' was not configured.");

        var options =
            new DbContextOptionsBuilder<DevRecallDbContext>();

        options
            .UseNpgsql(
                connectionString,
                npgsqlOptions =>
                    npgsqlOptions.MigrationsAssembly(
                        typeof(DevRecallDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention();

        return new DevRecallDbContext(options.Options);
    }
}
