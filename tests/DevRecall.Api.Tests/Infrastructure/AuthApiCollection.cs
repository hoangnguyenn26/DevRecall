namespace DevRecall.Api.Tests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class AuthApiTestSuite : ICollectionFixture<AuthApiFactory>
{
    public const string Name = "Auth API tests";
}
