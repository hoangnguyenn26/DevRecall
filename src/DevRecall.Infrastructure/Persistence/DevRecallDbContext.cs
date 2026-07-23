using DevRecall.Domain.Common;
using DevRecall.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Persistence;

public sealed class DevRecallDbContext(
    DbContextOptions<DevRecallDbContext> options)
    : DbContext(options)
{
    public DbSet<SystemMetadata> SystemMetadata =>
        Set<SystemMetadata>();

    public DbSet<User> Users =>
        Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            AssemblyReference.Assembly);
    }
}
