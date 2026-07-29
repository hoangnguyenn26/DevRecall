using DevRecall.Application.Analytics;
using DevRecall.Application.Analytics.ModuleBreakdown;
using DevRecall.Application.Identity;
using DevRecall.Application.Tests.Common.Time;
using DevRecall.Domain.Study;
using FluentAssertions;

namespace DevRecall.Application.Tests.Analytics;

public sealed class ModuleBreakdownHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset From =
        new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = From.AddDays(7);

    [Fact]
    public async Task Handler_ShouldCalculatePercentagesInStableOrder()
    {
        var result = await HandleAsync(
        [
            new ModuleActivityCount(StudyResourceType.DsaProblem, 5),
            new ModuleActivityCount(StudyResourceType.KnowledgeNode, 7),
            new ModuleActivityCount(StudyResourceType.ReviewItem, 4),
            new ModuleActivityCount(
                StudyResourceType.InterviewQuestion, 4)
        ]);

        result.TotalCompletedItems.Should().Be(20);
        result.Modules.Select(module => module.ResourceType).Should().Equal(
            "KnowledgeNode", "InterviewQuestion", "DsaProblem", "ReviewItem");
        result.Modules.Select(module => module.Percentage).Should().Equal(
            35m, 20m, 25m, 20m);
    }

    [Fact]
    public async Task Handler_ShouldRoundEachPercentageIndependently()
    {
        var result = await HandleAsync(
        [
            new ModuleActivityCount(StudyResourceType.KnowledgeNode, 1),
            new ModuleActivityCount(
                StudyResourceType.InterviewQuestion, 1),
            new ModuleActivityCount(StudyResourceType.DsaProblem, 1)
        ]);

        result.Modules.Select(module => module.Percentage).Should().Equal(
            33.33m, 33.33m, 33.33m, 0m);
    }

    [Fact]
    public async Task Handler_EmptyState_ShouldReturnFourZeroModules()
    {
        var result = await HandleAsync([]);

        result.TotalCompletedItems.Should().Be(0);
        result.Modules.Should().HaveCount(4);
        result.Modules.Should().OnlyContain(module =>
            module.CompletedItems == 0 && module.Percentage == 0m);
    }

    private static async Task<GetModuleBreakdownResult> HandleAsync(
        IReadOnlyList<ModuleActivityCount> counts)
    {
        var handler = new GetModuleBreakdownHandler(
            new AnalyticsDateRangeResolver(new FakeUtcClock(To)),
            new ReaderStub(counts), new CurrentUserStub());
        return await handler.HandleAsync(
            new GetModuleBreakdownQuery(From, To),
            CancellationToken.None);
    }

    private sealed class ReaderStub(
        IReadOnlyList<ModuleActivityCount> result)
        : IModuleBreakdownReader
    {
        public Task<IReadOnlyList<ModuleActivityCount>> ReadAsync(
            Guid userId, AnalyticsDateRange range,
            CancellationToken cancellationToken)
        {
            userId.Should().Be(UserId);
            return Task.FromResult(result);
        }
    }

    private sealed class CurrentUserStub : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => ModuleBreakdownHandlerTests.UserId;
    }
}
