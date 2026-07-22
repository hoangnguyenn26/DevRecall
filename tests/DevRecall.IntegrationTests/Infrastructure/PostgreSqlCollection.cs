namespace DevRecall.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class PostgreSqlTestSuite
    : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name =
        "PostgreSQL integration tests";
}
