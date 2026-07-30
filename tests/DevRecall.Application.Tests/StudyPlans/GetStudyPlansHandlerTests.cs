using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Identity;
using DevRecall.Application.StudyPlans.GetDetail;
using DevRecall.Application.StudyPlans.GetList;
using DevRecall.Application.StudyPlans.Resources;
using DevRecall.Domain.StudyPlans;
using FluentAssertions;

namespace DevRecall.Application.Tests.StudyPlans;

public sealed class GetStudyPlansHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task List_ShouldPassOptionalFilterAndBuildPagination()
    {
        var reader = new ListReaderStub
        {
            Result = new(
                [ListModel(StudyPlanStatus.Ready)], 25)
        };
        var handler = new GetStudyPlansHandler(
            reader, new CurrentUserStub(Guid.NewGuid()));

        var result = await handler.HandleAsync(
            new("Ready", 2, 10), CancellationToken.None);

        reader.Status.Should().Be(StudyPlanStatus.Ready);
        reader.Skip.Should().Be(10);
        result.TotalPages.Should().Be(3);
        result.Items.Should().ContainSingle(x => x.Status == "Ready");
    }

    [Fact]
    public async Task List_ShouldAllowNullStatusAndValidatePagination()
    {
        var reader = new ListReaderStub();
        var handler = new GetStudyPlansHandler(
            reader, new CurrentUserStub(Guid.NewGuid()));

        await handler.HandleAsync(new(null, 1, 20), CancellationToken.None);
        var invalid = () => handler.HandleAsync(
            new(null, 0, 101), CancellationToken.None);

        reader.Status.Should().BeNull();
        await invalid.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("+1")]
    [InlineData("01")]
    public void StatusParser_ShouldRejectNumericValues(string value)
    {
        var action = () => StudyPlanStatusParser.Parse(value);

        action.Should().Throw<ValidationException>();
    }

    [Fact]
    public async Task Detail_ShouldOrderItemsAndUseMissingResourceFallback()
    {
        var missingId = Guid.NewGuid();
        var availableId = Guid.NewGuid();
        var detailReader = new DetailReaderStub
        {
            Result = DetailModel(
            [
                Item(2, missingId),
                Item(1, availableId)
            ])
        };
        var resources = new ResourceReaderStub
        {
            Items =
            [
                new(
                    StudyPlanResourceType.KnowledgeNode, availableId,
                    "Available", "Preview", true)
            ]
        };
        var handler = new GetStudyPlanDetailHandler(
            detailReader, resources, new CurrentUserStub(Guid.NewGuid()));

        var result = await handler.HandleAsync(
            new(detailReader.Result.StudyPlanId), CancellationToken.None);

        result.Items.Select(x => x.Position).Should().Equal(1, 2);
        result.Items[0].ResourceTitle.Should().Be("Available");
        result.Items[1].ResourceTitle.Should().Be("Unavailable resource");
        result.Items[1].IsResourceAvailable.Should().BeFalse();
        result.TotalPlannedDurationMinutes.Should().Be(40);
    }

    [Fact]
    public async Task Detail_ShouldBatchDistinctReferencesOnce()
    {
        var resourceId = Guid.NewGuid();
        var detailReader = new DetailReaderStub
        {
            Result = DetailModel([Item(1, resourceId), Item(2, resourceId)])
        };
        var resources = new ResourceReaderStub();
        var handler = new GetStudyPlanDetailHandler(
            detailReader, resources, new CurrentUserStub(Guid.NewGuid()));

        await handler.HandleAsync(
            new(detailReader.Result.StudyPlanId), CancellationToken.None);

        resources.ReadCount.Should().Be(1);
        resources.References.Should().ContainSingle();
    }

    [Fact]
    public async Task Detail_NotOwnedShouldReturnNotFoundWithoutResourceRead()
    {
        var resources = new ResourceReaderStub();
        var handler = new GetStudyPlanDetailHandler(
            new DetailReaderStub(), resources,
            new CurrentUserStub(Guid.NewGuid()));
        var action = () => handler.HandleAsync(
            new(Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .Where(x => x.ErrorCode == StudyPlanErrors.NotFound.Code);
        resources.ReadCount.Should().Be(0);
    }

    [Fact]
    public async Task Detail_ShouldRejectEmptyId()
    {
        var handler = new GetStudyPlanDetailHandler(
            new DetailReaderStub(), new ResourceReaderStub(),
            new CurrentUserStub(Guid.NewGuid()));
        var action = () => handler.HandleAsync(
            new(Guid.Empty), CancellationToken.None);

        await action.Should().ThrowAsync<ValidationException>();
    }

    private static StudyPlanListReadModel ListModel(StudyPlanStatus status) =>
        new(
            Guid.NewGuid(), "Plan", status, 2, 35, Now, Now.AddDays(7),
            status == StudyPlanStatus.Ready ? Now : null, null, null, null,
            Now, 1);

    private static StudyPlanDetailReadModel DetailModel(
        IReadOnlyList<StudyPlanItemReadModel> items) =>
        new(
            Guid.NewGuid(), "Plan", StudyPlanStatus.Draft, Now,
            Now.AddDays(7), null, null, null, null, Now, Now, 1, items);

    private static StudyPlanItemReadModel Item(int position, Guid resourceId) =>
        new(
            Guid.NewGuid(), Guid.NewGuid(), StudyPlanSourceType.Recommendation,
            StudyPlanResourceType.KnowledgeNode, resourceId, 20, position);

    private sealed class ListReaderStub : IStudyPlanListReader
    {
        public PagedReadResult<StudyPlanListReadModel> Result { get; set; } =
            new([], 0);
        public StudyPlanStatus? Status { get; private set; }
        public int Skip { get; private set; }
        public Task<PagedReadResult<StudyPlanListReadModel>> ReadAsync(
            Guid userId, StudyPlanStatus? status, int skip, int take,
            CancellationToken cancellationToken)
        {
            Status = status;
            Skip = skip;
            return Task.FromResult(Result);
        }
    }

    private sealed class DetailReaderStub : IStudyPlanDetailReader
    {
        public StudyPlanDetailReadModel? Result { get; set; }
        public Task<StudyPlanDetailReadModel?> FindAsync(
            Guid userId, Guid studyPlanId,
            CancellationToken cancellationToken) => Task.FromResult(Result);
    }

    private sealed class ResourceReaderStub : IStudyPlanResourceSummaryReader
    {
        public IReadOnlyList<StudyPlanResourceSummary> Items { get; set; } = [];
        public int ReadCount { get; private set; }
        public IReadOnlyCollection<StudyPlanResourceReference> References
        {
            get;
            private set;
        } = [];
        public Task<IReadOnlyList<StudyPlanResourceSummary>> ReadManyAsync(
            Guid userId,
            IReadOnlyCollection<StudyPlanResourceReference> resources,
            CancellationToken cancellationToken)
        {
            ReadCount++;
            References = resources;
            return Task.FromResult(Items);
        }
    }

    private sealed class CurrentUserStub(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
    }
}
