using DevRecall.Domain.Study;
using FluentAssertions;

namespace DevRecall.Domain.Tests.Study;

public sealed class StudySessionTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 20, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldNormalizeAndInitializePlannedState()
    {
        var session = CreateSession("  Evening   Practice  ", 60, " Notes ");

        session.Title.Should().Be("Evening Practice");
        session.Status.Should().Be(StudySessionStatus.Planned);
        session.PlannedDurationMinutes.Should().Be(60);
        session.Notes.Should().Be("Notes");
        session.StartedAtUtc.Should().BeNull();
        session.CompletedAtUtc.Should().BeNull();
        session.ActualDurationMinutes.Should().BeNull();
        session.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddRemoveAndReorder_ShouldKeepContiguousPositions()
    {
        var session = CreateSession();
        var first = AddItem(session, StudyResourceType.KnowledgeNode, Now);
        var second = AddItem(session, StudyResourceType.DsaProblem, Now);
        var third = AddItem(
            session, StudyResourceType.InterviewQuestion, Now);

        session.ReorderItems(
            [third.Id, first.Id, second.Id], Now.AddMinutes(1))
            .Should().BeTrue();
        session.Items.OrderBy(item => item.Position)
            .Select(item => item.Id)
            .Should().Equal(third.Id, first.Id, second.Id);
        session.RemoveItem(first.Id, Now.AddMinutes(2)).Should().BeTrue();
        session.Items.OrderBy(item => item.Position)
            .Select(item => item.Position)
            .Should().Equal(0, 1);
    }

    [Fact]
    public void AddItem_WithDuplicateResource_ShouldThrow()
    {
        var session = CreateSession();
        var resourceId = Guid.NewGuid();
        session.AddItem(
            Guid.NewGuid(), StudyResourceType.DsaProblem,
            resourceId, null, Now);

        var action = () => session.AddItem(
            Guid.NewGuid(), StudyResourceType.DsaProblem,
            resourceId, null, Now);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage(StudySessionErrors.ItemAlreadyExists.Message);
    }

    [Fact]
    public void StartAndCompleteItems_ShouldFollowLifecycle()
    {
        var session = CreateSession();
        var first = AddItem(session, StudyResourceType.KnowledgeNode, Now);
        var second = AddItem(session, StudyResourceType.DsaProblem, Now);
        session.Start(Now.AddMinutes(1));
        session.StartItem(first.Id, Now.AddMinutes(2));

        session.CompleteItem(first.Id, " Done ", Now.AddMinutes(10));
        session.CompleteItem(second.Id, null, Now.AddMinutes(11));

        first.Status.Should().Be(StudySessionItemStatus.Completed);
        first.StartedAtUtc.Should().Be(Now.AddMinutes(2));
        first.CompletedAtUtc.Should().Be(Now.AddMinutes(10));
        first.Notes.Should().Be("Done");
        second.Status.Should().Be(StudySessionItemStatus.Completed);
        second.StartedAtUtc.Should().BeNull();
    }

    [Fact]
    public void SkipItem_ShouldPreserveStartedTimeAndSetCompletionTime()
    {
        var session = CreateSession();
        var item = AddItem(session, StudyResourceType.ReviewItem, Now);
        session.Start(Now);
        session.StartItem(item.Id, Now.AddMinutes(1));

        session.SkipItem(item.Id, "Later", Now.AddMinutes(2));

        item.Status.Should().Be(StudySessionItemStatus.Skipped);
        item.StartedAtUtc.Should().Be(Now.AddMinutes(1));
        item.CompletedAtUtc.Should().Be(Now.AddMinutes(2));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(59, 0)]
    [InlineData(90, 1)]
    [InlineData(3179, 52)]
    public void Complete_ShouldFloorActualDuration(
        int elapsedSeconds, int expectedMinutes)
    {
        var session = CreateSession();
        session.Start(Now);

        session.Complete(Now.AddSeconds(elapsedSeconds));

        session.Status.Should().Be(StudySessionStatus.Completed);
        session.ActualDurationMinutes.Should().Be(expectedMinutes);
    }

    [Fact]
    public void Complete_ShouldKeepUnfinishedItemStates()
    {
        var session = CreateSession();
        var pending = AddItem(session, StudyResourceType.KnowledgeNode, Now);
        var skipped = AddItem(session, StudyResourceType.DsaProblem, Now);
        session.Start(Now);
        session.SkipItem(skipped.Id, null, Now.AddMinutes(1));

        session.Complete(Now.AddMinutes(30));

        pending.Status.Should().Be(StudySessionItemStatus.Pending);
        skipped.Status.Should().Be(StudySessionItemStatus.Skipped);
    }

    [Fact]
    public void Cancel_ShouldBeIdempotentAndPreserveStartedTime()
    {
        var session = CreateSession();
        session.Start(Now);

        session.Cancel(Now.AddMinutes(5)).Should().BeTrue();
        session.Cancel(Now.AddMinutes(10)).Should().BeFalse();

        session.Status.Should().Be(StudySessionStatus.Cancelled);
        session.StartedAtUtc.Should().Be(Now);
        session.CompletedAtUtc.Should().BeNull();
        session.UpdatedAtUtc.Should().Be(Now.AddMinutes(5));
    }

    [Fact]
    public void TerminalSession_ShouldRejectFurtherChanges()
    {
        var session = CreateSession();
        session.Start(Now);
        session.Complete(Now.AddMinutes(1));

        var start = () => session.Start(Now.AddMinutes(2));
        var cancel = () => session.Cancel(Now.AddMinutes(2));
        var add = () => AddItem(
            session, StudyResourceType.KnowledgeNode, Now.AddMinutes(2));

        start.Should().Throw<InvalidOperationException>()
            .WithMessage(StudySessionErrors.SessionCompleted.Message);
        cancel.Should().Throw<InvalidOperationException>()
            .WithMessage(StudySessionErrors.SessionCompleted.Message);
        add.Should().Throw<InvalidOperationException>()
            .WithMessage(StudySessionErrors.SessionCompleted.Message);
    }

    [Fact]
    public void Create_WithInvalidInputs_ShouldThrow()
    {
        var emptyTitle = () => CreateSession(" ");
        var invalidDuration = () => CreateSession(
            plannedDuration: StudySessionText.MaximumPlannedDurationMinutes + 1);
        var nonUtc = () => StudySession.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Study", 30, null,
            Now.ToOffset(TimeSpan.FromHours(7)));
        var invalidResource = () => CreateSession().AddItem(
            Guid.NewGuid(), (StudyResourceType)0, Guid.NewGuid(), null, Now);

        emptyTitle.Should().Throw<ArgumentException>();
        invalidDuration.Should().Throw<ArgumentOutOfRangeException>();
        nonUtc.Should().Throw<ArgumentException>();
        invalidResource.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage($"*{StudySessionErrors.InvalidResourceType.Message}*");
    }

    [Fact]
    public void Mutations_ShouldIncrementVersionAndCompletionShouldBuildSummary()
    {
        var session = CreateSession();
        var knowledge = AddItem(
            session, StudyResourceType.KnowledgeNode, Now);
        _ = AddItem(session, StudyResourceType.DsaProblem, Now);
        session.Start(Now.AddMinutes(1));
        session.CompleteItem(
            knowledge.Id, null, Now.AddMinutes(2));
        var expectedVersion = session.Version;

        var summary = session.Complete(
            expectedVersion, Now.AddMinutes(31));

        session.Version.Should().Be(expectedVersion + 1);
        summary.TotalItems.Should().Be(2);
        summary.CompletedItems.Should().Be(1);
        summary.PendingItems.Should().Be(1);
        summary.KnowledgeItemsCompleted.Should().Be(1);
        summary.DsaItemsCompleted.Should().Be(0);
    }

    [Fact]
    public void Complete_WithStaleVersion_ShouldRejectWithoutMutation()
    {
        var session = CreateSession();
        session.Start(Now);

        var action = () => session.Complete(
            session.Version - 1, Now.AddMinutes(10));

        action.Should().Throw<StudySessionDomainException>()
            .Which.Error.Should().Be(StudySessionErrors.Conflict);
        session.Status.Should().Be(StudySessionStatus.InProgress);
        session.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void NoOpMutations_ShouldPreserveVersionAndUpdatedTime()
    {
        var session = CreateSession();
        var item = AddItem(
            session, StudyResourceType.KnowledgeNode, Now);
        var version = session.Version;
        var updatedAt = session.UpdatedAtUtc;

        session.UpdatePlan(
            version, session.Title, session.PlannedDurationMinutes,
            session.Notes, Now.AddMinutes(1)).Should().BeFalse();
        session.ReorderItems(
            version, [item.Id], Now.AddMinutes(1)).Should().BeFalse();

        session.Version.Should().Be(version);
        session.UpdatedAtUtc.Should().Be(updatedAt);
    }

    [Fact]
    public void UpdateReflection_ShouldNormalizeAndRemainACompletedSession()
    {
        var session = CreateSession();
        session.Start(Now);
        session.Complete(session.Version, Now.AddMinutes(20));
        var version = session.Version;

        session.UpdateReflection(
            version, "  Revisit service lifetimes.  ", Now.AddMinutes(21))
            .Should().BeTrue();

        session.Reflection.Should().Be("Revisit service lifetimes.");
        session.Status.Should().Be(StudySessionStatus.Completed);
        session.Version.Should().Be(version + 1);
        session.UpdateReflection(
            session.Version, "Revisit service lifetimes.", Now.AddMinutes(22))
            .Should().BeFalse();
        session.Version.Should().Be(version + 1);
    }

    [Fact]
    public void UpdateReflection_ForActiveSession_ShouldReject()
    {
        var session = CreateSession();
        session.Start(Now);

        var action = () => session.UpdateReflection(
            session.Version, "Too early", Now.AddMinutes(1));

        action.Should().Throw<InvalidOperationException>()
            .WithMessage(StudySessionErrors.SessionNotCompleted.Message);
    }

    private static StudySession CreateSession(
        string title = "Study",
        int plannedDuration = 30,
        string? notes = null) =>
        StudySession.Create(
            Guid.NewGuid(), Guid.NewGuid(), title,
            plannedDuration, notes, Now);

    private static StudySessionItem AddItem(
        StudySession session,
        StudyResourceType resourceType,
        DateTimeOffset createdAt) =>
        session.AddItem(
            Guid.NewGuid(), resourceType, Guid.NewGuid(), null, createdAt);
}
