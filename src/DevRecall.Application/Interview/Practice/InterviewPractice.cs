using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.Interview.Answers;
using DevRecall.Application.Interview.FollowUps;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.Practice;

namespace DevRecall.Application.Interview.Practice;

public sealed record InterviewPracticeReferenceAnswer(Guid AnswerId, string Content, int Version);
public sealed record InterviewPracticeFollowUp(Guid FollowUpId, string Question, string? ReferenceAnswer);
public sealed record GetInterviewPracticeResult(
    Guid QuestionId, string Question, string? Category, string? Difficulty,
    InterviewPracticeReferenceAnswer? ReferenceAnswer,
    IReadOnlyList<InterviewPracticeFollowUp> FollowUps, int QuestionVersion);
public sealed record InterviewFollowUpAttemptInput(Guid FollowUpId, string Answer);
public sealed record CompleteInterviewPracticeCommand(
    Guid QuestionId, string Answer, string SelfRating,
    IReadOnlyList<InterviewFollowUpAttemptInput> FollowUps,
    DateTimeOffset StartedAtUtc, Guid SubmissionId,
    Guid? ReferenceAnswerId = null);
public sealed record CompleteInterviewPracticeResult(
    Guid AttemptId, Guid QuestionId, string SelfRating,
    int FollowUpsAnswered, int FollowUpsSkipped, int DurationSeconds,
    DateTimeOffset CompletedAtUtc);
public interface IInterviewPracticeAttemptRepository
{
    Task<InterviewPracticeAttempt?> GetBySubmissionIdAsync(Guid userId, Guid submissionId, CancellationToken cancellationToken);
    void Add(InterviewPracticeAttempt attempt);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class GetInterviewPracticeHandler(
    IInterviewQuestionRepository questionRepository,
    IInterviewAnswerVersionRepository answerRepository,
    IInterviewFollowUpQuestionRepository followUpRepository,
    ICurrentUser currentUser)
{
    public async Task<GetInterviewPracticeResult> HandleAsync(Guid questionId, CancellationToken cancellationToken)
    {
        var userId = InterviewHandlerSupport.GetCurrentUserId(currentUser);
        var question = await questionRepository.GetByIdAndUserIdAsync(questionId, userId, cancellationToken);
        if (question is null || question.Status != InterviewQuestionStatus.Active)
            throw new NotFoundException(InterviewQuestionErrors.NotFound.Code, InterviewQuestionErrors.NotFound.Message);

        var answer = await answerRepository.GetCurrentPublishedAsync(questionId, cancellationToken);
        var followUps = await followUpRepository.GetActiveByQuestionIdAsync(questionId, false, cancellationToken);
        return new GetInterviewPracticeResult(
            question.Id, question.Question, question.Topic, question.Difficulty.ToString(),
            answer is null ? null : new InterviewPracticeReferenceAnswer(answer.Id, answer.Content, answer.VersionNumber),
            followUps.Select(item => new InterviewPracticeFollowUp(item.Id, item.Prompt, null)).ToList(), 1);
    }
}

public sealed class CompleteInterviewPracticeHandler(
    IInterviewQuestionRepository questionRepository,
    IInterviewAnswerVersionRepository answerRepository,
    IInterviewFollowUpQuestionRepository followUpRepository,
    IInterviewPracticeAttemptRepository attemptRepository,
    ICurrentUser currentUser,
    IUtcClock clock)
{
    public async Task<CompleteInterviewPracticeResult> HandleAsync(
        CompleteInterviewPracticeCommand command, CancellationToken cancellationToken)
    {
        Validate(command);
        var userId = InterviewHandlerSupport.GetCurrentUserId(currentUser);
        var existing = await attemptRepository.GetBySubmissionIdAsync(userId, command.SubmissionId, cancellationToken);
        if (existing is not null)
        {
            if (existing.QuestionId != command.QuestionId)
                throw new ConflictException("INTERVIEW_PRACTICE_SUBMISSION_CONFLICT", "The submission ID belongs to another question.");
            return Map(existing);
        }

        var question = await questionRepository.GetByIdAndUserIdAsync(command.QuestionId, userId, cancellationToken);
        if (question is null || question.Status != InterviewQuestionStatus.Active)
            throw new NotFoundException(InterviewQuestionErrors.NotFound.Code, InterviewQuestionErrors.NotFound.Message);
        var availableFollowUps = await followUpRepository.GetActiveByQuestionIdAsync(question.Id, false, cancellationToken);
        var referenceAnswer = command.ReferenceAnswerId is Guid referenceAnswerId
            ? await answerRepository.GetByIdAndQuestionIdAsync(referenceAnswerId, question.Id, cancellationToken)
            : await answerRepository.GetCurrentPublishedAsync(question.Id, cancellationToken);
        var followUpMap = availableFollowUps.ToDictionary(item => item.Id);
        if (command.FollowUps.Any(item => !followUpMap.ContainsKey(item.FollowUpId)) || command.FollowUps.Select(item => item.FollowUpId).Distinct().Count() != command.FollowUps.Count)
            throw Validation("followUps", "Follow-up answers must reference unique, active follow-up questions.");

        var rating = Enum.Parse<InterviewSelfRating>(command.SelfRating);
        var completedAtUtc = new DateTimeOffset(clock.UtcNow.Ticks / 10 * 10, TimeSpan.Zero);
        if (command.StartedAtUtc > completedAtUtc || completedAtUtc - command.StartedAtUtc > TimeSpan.FromDays(1))
            throw Validation("startedAtUtc", "Started time must be within the last 24 hours.");
        var snapshots = command.FollowUps
            .Where(item => !string.IsNullOrWhiteSpace(item.Answer))
            .Select(item => (item.FollowUpId, followUpMap[item.FollowUpId].Prompt, item.Answer));
        var attempt = InterviewPracticeAttempt.Create(
            Guid.NewGuid(), userId, question.Id, command.SubmissionId,
            question.Question, 1, command.Answer, referenceAnswer?.Content, rating,
            command.StartedAtUtc, completedAtUtc, snapshots, availableFollowUps.Count);
        attemptRepository.Add(attempt);
        await attemptRepository.SaveChangesAsync(cancellationToken);
        return Map(attempt);
    }

    private static void Validate(CompleteInterviewPracticeCommand command)
    {
        if (command.SubmissionId == Guid.Empty) throw Validation("submissionId", "Submission ID is required.");
        if (command.StartedAtUtc.Offset != TimeSpan.Zero) throw Validation("startedAtUtc", "Started time must be in UTC.");
        if (!Enum.TryParse<InterviewSelfRating>(command.SelfRating, false, out _) || int.TryParse(command.SelfRating, out _))
            throw Validation("selfRating", "Self-rating must be NeedsWork, Fair, Good, or Strong.");
        try { _ = Domain.Interview.Answers.InterviewAnswerContent.Normalize(command.Answer); }
        catch (ArgumentException exception) { throw Validation("answer", exception.Message); }
    }

    private static ValidationException Validation(string key, string message) =>
        new(new Dictionary<string, string[]> { [key] = [message] });

    private static CompleteInterviewPracticeResult Map(InterviewPracticeAttempt attempt) =>
        new(attempt.Id, attempt.QuestionId, attempt.SelfRating.ToString(), attempt.FollowUps.Count,
            attempt.FollowUpsSkipped, attempt.DurationSeconds, attempt.CompletedAtUtc);
}
