using DevRecall.Infrastructure.Development;
using FluentAssertions;

namespace DevRecall.Api.Tests.System;

public sealed class DemoDataSeedPolicyTests
{
    [Fact]
    public void ShouldRejectProductionEnvironment()
    {
        var action = () => DemoDataSeeder.EnsureDevelopmentEnvironment(false);

        action.Should().Throw<InvalidOperationException>().WithMessage("*Development environment*");
    }
}
