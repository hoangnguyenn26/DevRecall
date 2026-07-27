using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Identity;
using DevRecall.Application.Interview;
using DevRecall.Application.Interview.Answers;
using DevRecall.Application.Interview.Answers.UpdateDraft;
using DevRecall.Application.Interview.FollowUps;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.Answers;
using DevRecall.Domain.Interview.FollowUps;
using FluentAssertions;

namespace DevRecall.Application.Tests.Interview;

public sealed class InterviewNoOpWriteTests
{
    [Fact]
    public async Task UpdateAnswerDraft_WithEquivalentContent_ShouldNotSave()
    {
        var userId = Guid.NewGuid();
        var question = CreateQuestion(userId);
        var answer = InterviewAnswerVersion.CreateDraft(
            Guid.NewGuid(), question.Id, 1, "Answer content",
            DateTimeOffset.UtcNow);
        var answerRepository = new FakeAnswerRepository(answer);
        var handler = new UpdateInterviewAnswerDraftHandler(
            new FakeQuestionRepository(question), answerRepository,
            new FakeCurrentUser(userId));

        var result = await handler.HandleAsync(
            new UpdateInterviewAnswerDraftCommand(
                question.Id, answer.Id, "  Answer content  "),
            CancellationToken.None);

        answerRepository.SaveChangesCallCount.Should().Be(0);
        result.UpdatedAtUtc.Should().Be(answer.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateFollowUp_WithEquivalentPrompt_ShouldNotSave()
    {
        var userId = Guid.NewGuid();
        var question = CreateQuestion(userId);
        var followUp = InterviewFollowUpQuestion.Create(
            Guid.NewGuid(), question.Id, "What happens?", 0,
            DateTimeOffset.UtcNow);
        var followUpRepository = new FakeFollowUpRepository(followUp);
        var handler = new UpdateInterviewFollowUpHandler(
            new FakeQuestionRepository(question), followUpRepository,
            new FakeCurrentUser(userId));

        var result = await handler.HandleAsync(
            question.Id, followUp.Id, "  What   happens?  ",
            CancellationToken.None);

        followUpRepository.SaveChangesCallCount.Should().Be(0);
        result.UpdatedAtUtc.Should().Be(followUp.UpdatedAtUtc);
    }

    private static InterviewQuestion CreateQuestion(Guid userId) =>
        InterviewQuestion.Create(
            Guid.NewGuid(), userId, "Question", "Question text", "C#",
            InterviewQuestionDifficulty.Medium, null, DateTimeOffset.UtcNow);

    private sealed class FakeCurrentUser(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
    }

    private sealed class FakeQuestionRepository(InterviewQuestion question)
        : IInterviewQuestionRepository
    {
        public Task<InterviewQuestion?> GetByIdAsync(
            Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == question.Id ? question : null);

        public Task<InterviewQuestion?> GetByIdAndUserIdAsync(
            Guid id, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(id == question.Id && userId == question.UserId
                ? question
                : null);

        public Task<PagedReadResult<InterviewQuestionListReadItem>>
            GetActiveListAsync(
                Guid userId, string? topic,
                InterviewQuestionDifficulty? difficulty, int skip, int take,
                CancellationToken cancellationToken) =>
            Task.FromResult(
                new PagedReadResult<InterviewQuestionListReadItem>([], 0));

        public void Add(InterviewQuestion interviewQuestion) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeAnswerRepository(InterviewAnswerVersion answer)
        : IInterviewAnswerVersionRepository
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<InterviewAnswerVersion?> GetByIdAsync(
            Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == answer.Id ? answer : null);

        public Task<InterviewAnswerVersion?> GetByIdAndQuestionIdAsync(
            Guid id, Guid interviewQuestionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(id == answer.Id
                && interviewQuestionId == answer.InterviewQuestionId
                    ? answer
                    : null);

        public Task<bool> HasDraftAsync(
            Guid interviewQuestionId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<int> GetNextVersionNumberAsync(
            Guid interviewQuestionId, CancellationToken cancellationToken) =>
            Task.FromResult(2);

        public Task<InterviewAnswerVersion?> GetCurrentPublishedAsync(
            Guid interviewQuestionId, CancellationToken cancellationToken) =>
            Task.FromResult<InterviewAnswerVersion?>(null);

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

    private sealed class FakeFollowUpRepository(
        InterviewFollowUpQuestion followUp)
        : IInterviewFollowUpQuestionRepository
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<InterviewFollowUpQuestion?> GetByIdAndQuestionIdAsync(
            Guid id, Guid interviewQuestionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(id == followUp.Id
                && interviewQuestionId == followUp.InterviewQuestionId
                    ? followUp
                    : null);

        public Task<IReadOnlyList<InterviewFollowUpQuestion>>
            GetActiveByQuestionIdAsync(
                Guid interviewQuestionId, bool trackChanges,
                CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<InterviewFollowUpQuestion>>(
                [followUp]);

        public Task<int> CountActiveAsync(
            Guid interviewQuestionId, CancellationToken cancellationToken) =>
            Task.FromResult(1);

        public void Add(InterviewFollowUpQuestion item) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
