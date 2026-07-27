using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Identity;
using DevRecall.Application.Interview;
using DevRecall.Application.Interview.Answers;
using DevRecall.Application.Interview.Answers.Publish;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.Answers;
using FluentAssertions;

namespace DevRecall.Application.Tests.Interview.Answers.Publish;

public sealed class PublishInterviewAnswerVersionHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenAlreadyPublished_ShouldNotSave()
    {
        var userId = Guid.NewGuid();
        var question = InterviewQuestion.Create(
            Guid.NewGuid(), userId, "Question", "Question text", "LINQ",
            InterviewQuestionDifficulty.Medium, null, DateTimeOffset.UtcNow);
        var answer = InterviewAnswerVersion.CreateDraft(
            Guid.NewGuid(), question.Id, 1, "Answer", DateTimeOffset.UtcNow);
        var publishedAtUtc = DateTimeOffset.UtcNow.AddMinutes(1);
        answer.Publish(publishedAtUtc);
        var answerRepository = new FakeAnswerRepository(answer);
        var handler = new PublishInterviewAnswerVersionHandler(
            new FakeQuestionRepository(question), answerRepository,
            new FakeCurrentUser(userId));

        var result = await handler.HandleAsync(
            new PublishInterviewAnswerVersionCommand(question.Id, answer.Id),
            CancellationToken.None);

        answerRepository.SaveChangesCallCount.Should().Be(0);
        result.PublishedAtUtc.Should().Be(publishedAtUtc);
    }

    private sealed class FakeCurrentUser(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
    }

    private sealed class FakeAnswerRepository(InterviewAnswerVersion answer)
        : IInterviewAnswerVersionRepository
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<InterviewAnswerVersion?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult(id == answer.Id ? answer : null);

        public Task<InterviewAnswerVersion?> GetByIdAndQuestionIdAsync(
            Guid id,
            Guid interviewQuestionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(id == answer.Id
                && interviewQuestionId == answer.InterviewQuestionId
                    ? answer
                    : null);

        public Task<bool> HasDraftAsync(
            Guid interviewQuestionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<int> GetNextVersionNumberAsync(
            Guid interviewQuestionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(2);

        public Task<InterviewAnswerVersion?> GetCurrentPublishedAsync(
            Guid interviewQuestionId,
            CancellationToken cancellationToken) =>
            Task.FromResult<InterviewAnswerVersion?>(answer);

        public Task<IReadOnlyList<InterviewAnswerVersion>>
            GetByQuestionIdAsync(
                Guid interviewQuestionId,
                CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<InterviewAnswerVersion>>([answer]);

        public void Add(InterviewAnswerVersion answerVersion) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeQuestionRepository(InterviewQuestion question)
        : IInterviewQuestionRepository
    {
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

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
