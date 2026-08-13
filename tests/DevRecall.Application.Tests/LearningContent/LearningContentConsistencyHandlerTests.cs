using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.LearningContent;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using FluentAssertions;
using LearningContentAggregate = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.Application.Tests.LearningContent;

public sealed class LearningContentConsistencyHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Complete_ShouldRejectCompletedProgressWithoutEvidence()
    {
        var content = CreateContent();
        var repository = new RepositoryStub(content)
        {
            Progress = LearningContentProgress.CompleteDirectly(
                Guid.NewGuid(), UserId, content.Id, Now)
        };

        var action = () => new CompleteLearningContentHandler(repository, new UserStub(), new ClockStub())
            .HandleAsync(content.Slug, null, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ConflictException>();
        exception.Which.ErrorCode.Should().Be("LEARNING_CONTENT_COMPLETION_INCONSISTENT");
        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task StartAndComplete_ShouldRejectEvidenceWithoutCompletedProgress()
    {
        var content = CreateContent();
        var repository = new RepositoryStub(content)
        {
            Progress = LearningContentProgress.Start(Guid.NewGuid(), UserId, content.Id, Now.AddMinutes(-5)),
            Evidence = LearningContentCompletionEvidence.Create(
                Guid.NewGuid(), UserId, content.Id, content.Title, Now)
        };

        var start = () => new StartLearningContentHandler(repository, new UserStub(), new ClockStub())
            .HandleAsync(content.Slug, CancellationToken.None);
        var complete = () => new CompleteLearningContentHandler(repository, new UserStub(), new ClockStub())
            .HandleAsync(content.Slug, 1, CancellationToken.None);

        (await start.Should().ThrowAsync<ConflictException>()).Which.ErrorCode
            .Should().Be("LEARNING_CONTENT_COMPLETION_INCONSISTENT");
        (await complete.Should().ThrowAsync<ConflictException>()).Which.ErrorCode
            .Should().Be("LEARNING_CONTENT_COMPLETION_INCONSISTENT");
        repository.SaveCount.Should().Be(0);
    }

    private static LearningContentAggregate CreateContent()
    {
        var topicId = Guid.NewGuid();
        var content = LearningContentAggregate.CreateDraft(Guid.NewGuid(), "consistency-lesson",
            "Consistency lesson", "A valid consistency lesson summary.", LearningContentType.Lesson,
            ContentDifficulty.Beginner, 10, ContentSourceType.Internal, "DevRecall", null,
            [Technology.DotNet], [topicId], [new("Understand consistency.")],
            [new(LearningContentSectionType.Explanation, null, "Consistency body")], Now);
        content.Publish(Now);
        return content;
    }

    private sealed class RepositoryStub(LearningContentAggregate content)
        : ILearningContentProgressRepository
    {
        public LearningContentProgress? Progress { get; init; }
        public LearningContentCompletionEvidence? Evidence { get; init; }
        public int SaveCount { get; private set; }
        public Task<LearningContentAggregate?> GetPublishedContentAsync(string slug,
            CancellationToken cancellationToken) => Task.FromResult<LearningContentAggregate?>(content);
        public Task<LearningContentProgress?> GetAsync(Guid userId, Guid contentId,
            CancellationToken cancellationToken) => Task.FromResult(Progress);
        public Task<LearningContentCompletionEvidence?> GetCompletionEvidenceAsync(Guid userId,
            Guid contentId, CancellationToken cancellationToken) => Task.FromResult(Evidence);
        public void Add(LearningContentProgress progress) { }
        public void Add(LearningContentCompletionEvidence evidence) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class UserStub : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => LearningContentConsistencyHandlerTests.UserId;
    }

    private sealed class ClockStub : IUtcClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
