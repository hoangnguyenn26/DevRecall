using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Identity;
using DevRecall.Application.Interview;
using DevRecall.Application.Interview.Archive;
using DevRecall.Domain.Interview;
using FluentAssertions;

namespace DevRecall.Application.Tests.Interview.Archive;

public sealed class ArchiveInterviewQuestionHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenAlreadyArchived_ShouldNotSave()
    {
        var userId = Guid.NewGuid();
        var question = InterviewQuestion.Create(
            Guid.NewGuid(), userId, "Question", "Question text", "LINQ",
            InterviewQuestionDifficulty.Medium, null, DateTimeOffset.UtcNow);
        question.Archive(DateTimeOffset.UtcNow.AddMinutes(1));
        var repository = new FakeRepository(question);
        var handler = new ArchiveInterviewQuestionHandler(
            repository,
            new FakeCurrentUser(userId));

        await handler.HandleAsync(
            new ArchiveInterviewQuestionCommand(question.Id),
            CancellationToken.None);

        repository.SaveChangesCallCount.Should().Be(0);
    }

    private sealed class FakeCurrentUser(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
    }

    private sealed class FakeRepository(InterviewQuestion question)
        : IInterviewQuestionRepository
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<InterviewQuestion?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult(id == question.Id ? question : null);

        public Task<InterviewQuestion?> GetByIdAndUserIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(id == question.Id && userId == question.UserId
                ? question
                : null);

        public Task<PagedReadResult<InterviewQuestionListReadItem>>
            GetActiveListAsync(
                Guid userId,
                string? topic,
                InterviewQuestionDifficulty? difficulty,
                int skip,
                int take,
                CancellationToken cancellationToken) =>
            Task.FromResult(
                new PagedReadResult<InterviewQuestionListReadItem>([], 0));

        public void Add(InterviewQuestion interviewQuestion) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
