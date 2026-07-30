using DevRecall.Domain.Recommendations;
using DevRecall.Domain.StudyPlans;
using FluentAssertions;

namespace DevRecall.Domain.Tests.StudyPlans;

public sealed class StudyPlanTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 20, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldNormalizeTitleAndStartDraftAtVersionOne()
    {
        var plan = CreatePlan("  Evening Practice  ");

        plan.Title.Should().Be("Evening Practice");
        plan.Status.Should().Be(StudyPlanStatus.Draft);
        plan.Version.Should().Be(1);
        plan.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_ShouldRejectBlankTitle(string title)
    {
        var action = () => CreatePlan(title);

        action.Should().Throw<StudyPlanDomainException>()
            .Which.Error.Should().Be(StudyPlanErrors.TitleRequired);
    }

    [Fact]
    public void Create_ShouldRejectOverlongTitleAndInvalidExpiration()
    {
        var longTitle = () => CreatePlan(
            new string('a', StudyPlanDefaults.MaximumTitleLength + 1));
        var expiration = () => StudyPlan.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Plan", Now, Now);

        longTitle.Should().Throw<StudyPlanDomainException>()
            .Which.Error.Should().Be(StudyPlanErrors.TitleTooLong);
        expiration.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddRecommendationItem_ShouldAssignPositionAndRejectDuplicateResource()
    {
        var plan = CreatePlan();
        var resourceId = Guid.NewGuid();
        var item = AddItem(plan, resourceId, 20);
        var duplicate = () => AddItem(plan, resourceId, 30);

        item.Position.Should().Be(1);
        item.SourceType.Should().Be(StudyPlanSourceType.Recommendation);
        plan.TotalPlannedDurationMinutes.Should().Be(20);
        duplicate.Should().Throw<StudyPlanDomainException>()
            .Which.Error.Should().Be(StudyPlanErrors.DuplicateResource);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(181)]
    public void AddRecommendationItem_ShouldRejectInvalidDuration(int minutes)
    {
        var action = () => AddItem(CreatePlan(), Guid.NewGuid(), minutes);

        action.Should().Throw<StudyPlanDomainException>()
            .Which.Error.Should().Be(StudyPlanErrors.InvalidItemDuration);
    }

    [Fact]
    public void AddRecommendationItem_ShouldEnforceItemAndTotalLimits()
    {
        var itemLimitPlan = CreatePlan();
        for (var index = 0; index < StudyPlanDefaults.MaximumItems; index++)
        {
            AddItem(itemLimitPlan, Guid.NewGuid(), 5);
        }

        var itemLimit = () => AddItem(itemLimitPlan, Guid.NewGuid(), 5);
        var durationPlan = CreatePlan();
        AddItem(durationPlan, Guid.NewGuid(), 180);
        AddItem(durationPlan, Guid.NewGuid(), 180);
        var durationLimit = () => AddItem(durationPlan, Guid.NewGuid(), 180);

        itemLimit.Should().Throw<StudyPlanDomainException>()
            .Which.Error.Should().Be(StudyPlanErrors.ItemLimitReached);
        durationLimit.Should().Throw<StudyPlanDomainException>()
            .Which.Error.Should().Be(StudyPlanErrors.DurationLimitExceeded);
    }

    [Fact]
    public void UpdateDuration_ShouldMutateOnceAndLeaveNoOpUnchanged()
    {
        var plan = CreatePlan();
        var item = AddItem(plan, Guid.NewGuid(), 20);
        var changed = plan.UpdateItemDuration(item.Id, 30, Now.AddMinutes(2));
        var version = plan.Version;
        var noOp = plan.UpdateItemDuration(item.Id, 30, Now.AddMinutes(3));

        changed.Should().BeTrue();
        noOp.Should().BeFalse();
        plan.Version.Should().Be(version);
        item.UpdatedAtUtc.Should().Be(Now.AddMinutes(2));
    }

    [Fact]
    public void Remove_ShouldNormalizePositions()
    {
        var plan = CreatePlan();
        var first = AddItem(plan, Guid.NewGuid(), 15);
        var second = AddItem(plan, Guid.NewGuid(), 20);
        var third = AddItem(plan, Guid.NewGuid(), 30);

        plan.RemoveItem(second.Id, Now.AddMinutes(4)).Should().BeTrue();

        plan.Items.OrderBy(x => x.Position).Select(x => x.Id)
            .Should().Equal(first.Id, third.Id);
        third.Position.Should().Be(2);
    }

    [Fact]
    public void Reorder_ShouldRequireCompleteUniqueSetAndNoOpForSameOrder()
    {
        var plan = CreatePlan();
        var first = AddItem(plan, Guid.NewGuid(), 15);
        var second = AddItem(plan, Guid.NewGuid(), 20);
        var invalid = () => plan.ReorderItems(
            [first.Id, first.Id], Now.AddMinutes(3));
        var version = plan.Version;

        invalid.Should().Throw<StudyPlanDomainException>()
            .Which.Error.Should().Be(StudyPlanErrors.InvalidItemOrder);
        plan.ReorderItems(
            [first.Id, second.Id], Now.AddMinutes(4)).Should().BeFalse();
        plan.Version.Should().Be(version);
        plan.ReorderItems(
            [second.Id, first.Id], Now.AddMinutes(5)).Should().BeTrue();
        second.Position.Should().Be(1);
    }

    [Fact]
    public void MarkReady_ShouldRequireItemsAndFreezeStructure()
    {
        var plan = CreatePlan();
        var empty = () => plan.MarkReady(plan.Version, Now.AddMinutes(1));
        empty.Should().Throw<StudyPlanDomainException>()
            .Which.Error.Should().Be(StudyPlanErrors.EmptyPlan);
        AddItem(plan, Guid.NewGuid(), 20);

        plan.MarkReady(plan.Version, Now.AddMinutes(2)).Should().BeTrue();
        var edit = () => AddItem(plan, Guid.NewGuid(), 20);

        plan.Status.Should().Be(StudyPlanStatus.Ready);
        edit.Should().Throw<StudyPlanDomainException>()
            .Which.Error.Should().Be(StudyPlanErrors.NotDraft);
    }

    [Fact]
    public void Cancel_ShouldAllowDraftAndReadyButNotConverted()
    {
        var draft = CreatePlan();
        draft.Cancel(draft.Version, Now.AddMinutes(1)).Should().BeTrue();
        var ready = CreatePlan();
        AddItem(ready, Guid.NewGuid(), 20);
        ready.MarkReady(ready.Version, Now.AddMinutes(1));
        ready.Cancel(ready.Version, Now.AddMinutes(2)).Should().BeTrue();
        var converted = CreateReadyPlan();
        converted.MarkConverted(
            converted.Version, Guid.NewGuid(), Now.AddMinutes(2));

        var action = () => converted.Cancel(
            converted.Version, Now.AddMinutes(3));

        action.Should().Throw<StudyPlanDomainException>()
            .Which.Error.Should().Be(StudyPlanErrors.AlreadyConverted);
    }

    [Fact]
    public void MarkConverted_ShouldRequireReadyAndPreserveSessionId()
    {
        var draft = CreatePlan();
        var invalid = () => draft.MarkConverted(
            draft.Version, Guid.NewGuid(), Now.AddMinutes(1));
        var plan = CreateReadyPlan();
        var sessionId = Guid.NewGuid();

        plan.MarkConverted(plan.Version, sessionId, Now.AddMinutes(2))
            .Should().BeTrue();

        invalid.Should().Throw<StudyPlanDomainException>()
            .Which.Error.Should().Be(StudyPlanErrors.NotReady);
        plan.ConvertedStudySessionId.Should().Be(sessionId);
        plan.Status.Should().Be(StudyPlanStatus.Converted);
    }

    [Fact]
    public void Lifecycle_ShouldRejectStaleVersionAndKeepNoOpVersion()
    {
        var plan = CreateReadyPlan();
        var stale = () => plan.MarkConverted(
            plan.Version - 1, Guid.NewGuid(), Now.AddMinutes(2));
        stale.Should().Throw<StudyPlanDomainException>()
            .Which.Error.Should().Be(StudyPlanErrors.Conflict);
        plan.Cancel(plan.Version, Now.AddMinutes(2));
        var version = plan.Version;

        plan.Cancel(version, Now.AddMinutes(3)).Should().BeFalse();
        plan.Version.Should().Be(version);
    }

    [Theory]
    [InlineData(RecommendationPriority.Low, 15)]
    [InlineData(RecommendationPriority.Medium, 20)]
    [InlineData(RecommendationPriority.High, 30)]
    [InlineData(RecommendationPriority.Critical, 45)]
    public void CompositionPolicy_ShouldMapDuration(
        RecommendationPriority priority, int expected) =>
        StudyPlanCompositionPolicy.GetDefaultDurationMinutes(priority)
            .Should().Be(expected);

    [Fact]
    public void CompositionPolicy_ShouldOrderSkipDuplicatesAndRespectBudget()
    {
        var duplicateResource = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(RecommendationPriority.High, 10, Now, duplicateResource),
            Candidate(RecommendationPriority.Critical, 12, Now.AddDays(-1)),
            Candidate(RecommendationPriority.High, 11, Now.AddDays(-2)),
            Candidate(
                RecommendationPriority.High, 9, Now.AddDays(1), duplicateResource),
            Candidate(RecommendationPriority.Medium, 20, Now)
        };

        var selected = StudyPlanCompositionPolicy.Select(candidates, 90);

        selected.Select(x => x.Priority).Should().Equal(
            RecommendationPriority.Critical, RecommendationPriority.High);
        selected.Sum(x => StudyPlanCompositionPolicy
            .GetDefaultDurationMinutes(x.Priority)).Should().Be(75);
    }

    [Fact]
    public void MappingPolicy_ShouldMapExplicitlyAndRejectUnknown()
    {
        StudyPlanMappingPolicy.MapResourceType(
            RecommendationResourceType.KnowledgeNode)
            .Should().Be(StudyPlanResourceType.KnowledgeNode);
        var action = () => StudyPlanMappingPolicy.MapResourceType(
            (RecommendationResourceType)999);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static StudyPlan CreatePlan(string title = "Plan") =>
        StudyPlan.Create(
            Guid.NewGuid(), Guid.NewGuid(), title, Now,
            Now.Add(StudyPlanDefaults.DefaultLifetime));

    private static StudyPlan CreateReadyPlan()
    {
        var plan = CreatePlan();
        AddItem(plan, Guid.NewGuid(), 20);
        plan.MarkReady(plan.Version, Now.AddMinutes(1));
        return plan;
    }

    private static StudyPlanItem AddItem(
        StudyPlan plan, Guid resourceId, int minutes) =>
        plan.AddRecommendationItem(
            Guid.NewGuid(), Guid.NewGuid(), StudyPlanResourceType.KnowledgeNode,
            resourceId, minutes, plan.UpdatedAtUtc.AddMinutes(1));

    private static StudyPlanCompositionCandidate Candidate(
        RecommendationPriority priority, decimal score, DateTimeOffset generatedAt,
        Guid? resourceId = null) =>
        new(
            Guid.NewGuid(), RecommendationResourceType.KnowledgeNode,
            resourceId ?? Guid.NewGuid(), priority, score, generatedAt);
}
