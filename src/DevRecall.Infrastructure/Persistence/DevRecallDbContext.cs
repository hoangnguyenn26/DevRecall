using DevRecall.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Persistence;

public sealed class DevRecallDbContext(
    DbContextOptions<DevRecallDbContext> options)
    : DbContext(options)
{
    public DbSet<SystemMetadata> SystemMetadata =>
        Set<SystemMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            AssemblyReference.Assembly);
    }
}
