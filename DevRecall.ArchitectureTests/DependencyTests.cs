using FluentAssertions;

namespace DevRecall.ArchitectureTests;

public sealed class DependencyTests
{
    [Fact]
    public void Domain_should_not_reference_forbidden_layers()
    {
        var references =
            AssemblyDependencyHelper.GetReferencedAssemblyNames(
                DevRecall.Domain.AssemblyReference.Assembly);

        references.Should().NotContain(
        [
            "DevRecall.Infrastructure",
            "DevRecall.Api",
            "Microsoft.EntityFrameworkCore"
        ]);
    }

    [Fact]
    public void Application_should_not_reference_forbidden_layers()
    {
        var references =
            AssemblyDependencyHelper.GetReferencedAssemblyNames(
                DevRecall.Application.AssemblyReference.Assembly);

        references.Should().NotContain(
        [
            "DevRecall.Infrastructure",
            "DevRecall.Api"
        ]);
    }

    [Fact]
    public void Infrastructure_should_not_reference_api()
    {
        var references =
            AssemblyDependencyHelper.GetReferencedAssemblyNames(
                DevRecall.Infrastructure.AssemblyReference.Assembly);

        references.Should()
            .NotContain("DevRecall.Api");
    }
}
