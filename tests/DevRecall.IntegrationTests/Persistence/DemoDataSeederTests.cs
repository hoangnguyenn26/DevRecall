using DevRecall.Application.Identity;
using DevRecall.Infrastructure.Development;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class DemoDataSeederTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ShouldCreateCoherentDemoDataOnlyOnce()
    {
        await using var context = fixture.CreateDbContext();
        var seeder = new DemoDataSeeder(context, new FakePasswordHasher());

        var firstRun = await seeder.SeedAsync("local-demo-password", CancellationToken.None);
        var secondRun = await seeder.SeedAsync("local-demo-password", CancellationToken.None);

        firstRun.Should().BeTrue();
        secondRun.Should().BeFalse();
        (await context.Users.CountAsync(user => user.Email == DemoDataSeeder.Email)).Should().Be(1);
        (await context.KnowledgeNodes.CountAsync(node => node.UserId == Guid.Parse("00000000-0000-0000-0000-000000000001"))).Should().Be(12);
        (await context.StudySessions.CountAsync(session => session.UserId == Guid.Parse("00000000-0000-0000-0000-000000000001"))).Should().Be(2);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string passwordHash, string providedPassword) => passwordHash == $"hashed:{providedPassword}";
    }
}
