using FluentAssertions;

namespace DevRecall.Domain.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void Test_infrastructure_should_be_operational()
    {
        true.Should().BeTrue();
    }
}
