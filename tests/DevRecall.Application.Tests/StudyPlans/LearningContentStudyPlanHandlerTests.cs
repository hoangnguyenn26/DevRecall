using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.StudyPlans;
using DevRecall.Application.StudyPlans.LearningContent;
using DevRecall.Domain.StudyPlans;
using FluentAssertions;

namespace DevRecall.Application.Tests.StudyPlans;

public sealed class LearningContentStudyPlanHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Add_ShouldCreateManualLessonItemAndRetryAsSuccessEquivalent()
    {
        var plan = StudyPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Backend plan", Now, null);
        var repository = new RepositoryStub(plan);
        var source = new SourceStub(new(Guid.NewGuid(), "DI lifetimes", 15, false));
        var handler = new AddLearningContentToStudyPlanHandler(repository, source,
            new UserStub(plan.UserId), new ClockStub());
        var submissionId = Guid.NewGuid();

        var first = await handler.HandleAsync(new(plan.Id, "di-lifetimes", 1, submissionId), default);
        var retry = await handler.HandleAsync(new(plan.Id, "di-lifetimes", 1, submissionId), default);

        first.Added.Should().BeTrue();
        retry.Added.Should().BeFalse();
        retry.ItemId.Should().Be(first.ItemId);
        plan.Items.Should().ContainSingle(item => item.ResourceType == StudyPlanResourceType.LearningContent);
        repository.Saves.Should().Be(1);
    }

    [Fact]
    public async Task Add_ShouldRejectCompletedLessonWithoutChangingPlan()
    {
        var plan = StudyPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Backend plan", Now, null);
        var handler = new AddLearningContentToStudyPlanHandler(new RepositoryStub(plan),
            new SourceStub(new(Guid.NewGuid(), "DI lifetimes", 15, true)),
            new UserStub(plan.UserId), new ClockStub());

        var action = () => handler.HandleAsync(new(plan.Id, "di-lifetimes", 1, Guid.NewGuid()), default);

        await action.Should().ThrowAsync<ConflictException>()
            .Where(error => error.ErrorCode == "LEARNING_CONTENT_ALREADY_COMPLETED");
        plan.Items.Should().BeEmpty();
    }

    private sealed class RepositoryStub(StudyPlan plan) : IStudyPlanRepository
    {
        public int Saves { get; private set; }
        public Task<StudyPlan?> GetByIdAndUserIdForUpdateAsync(Guid studyPlanId, Guid userId,
            CancellationToken cancellationToken) => Task.FromResult<StudyPlan?>(
                plan.Id == studyPlanId && plan.UserId == userId ? plan : null);
        public Task<StudyPlan?> GetDraftByUserIdForUpdateAsync(Guid userId,
            CancellationToken cancellationToken) => Task.FromResult<StudyPlan?>(plan);
        public void Add(StudyPlan studyPlan) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken) { Saves++; return Task.CompletedTask; }
    }
    private sealed class SourceStub(LearningContentPlanSource source) : ILearningContentPlanSourceReader
    {
        public Task<LearningContentPlanSource?> FindPublishedAsync(Guid userId, string slug,
            CancellationToken cancellationToken) => Task.FromResult<LearningContentPlanSource?>(source);
        public Task<IReadOnlyList<LearningContentStudyPlanOption>> GetDraftOptionsAsync(Guid userId,
            string slug, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<LearningContentStudyPlanOption>>([]);
    }
    private sealed class UserStub(Guid id) : ICurrentUser { public bool IsAuthenticated => true; public Guid? UserId => id; }
    private sealed class ClockStub : IUtcClock { public DateTimeOffset UtcNow => Now; }
}
