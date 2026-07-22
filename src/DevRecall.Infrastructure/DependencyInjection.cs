using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException(
                "Connection string 'Database' was not configured.");

        services.AddDbContext<DevRecallDbContext>(
            options =>
            {
                options
                    .UseNpgsql(
                        connectionString,
                        npgsqlOptions =>
                            npgsqlOptions.MigrationsAssembly(
                                typeof(DevRecallDbContext).Assembly.FullName))
                    .UseSnakeCaseNamingConvention();
            });

        return services;
    }
}
