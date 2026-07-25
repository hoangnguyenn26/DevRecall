using DevRecall.Domain.Knowledge.Tags;
using FluentAssertions;

namespace DevRecall.Domain.Tests.Knowledge.Tags;

public sealed class TagTests
{
    [Fact]
    public void Create_ShouldNormalizeTagName()
    {
        var tag = CreateTag("  ASP.NET   Core  ");

        tag.Name.Should().Be("ASP.NET Core");
        tag.NormalizedName.Should().Be("ASP.NET CORE");
    }

    [Fact]
    public void Rename_WithEquivalentName_ShouldBeNoOp()
    {
        var tag = CreateTag("Interview");
        var originalUpdatedAtUtc = tag.UpdatedAtUtc;

        var changed = tag.Rename(" Interview ", originalUpdatedAtUtc.AddMinutes(1));

        changed.Should().BeFalse();
        tag.UpdatedAtUtc.Should().Be(originalUpdatedAtUtc);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldBeIdempotent()
    {
        var tag = CreateTag("Interview");
        var firstArchivedAtUtc = tag.UpdatedAtUtc.AddMinutes(1);

        tag.Archive(firstArchivedAtUtc);
        var changed = tag.Archive(firstArchivedAtUtc.AddMinutes(1));

        changed.Should().BeFalse();
        tag.Status.Should().Be(TagStatus.Archived);
        tag.UpdatedAtUtc.Should().Be(firstArchivedAtUtc);
    }

    private static Tag CreateTag(string name) =>
        Tag.Create(Guid.NewGuid(), Guid.NewGuid(), name, DateTimeOffset.UtcNow);
}
