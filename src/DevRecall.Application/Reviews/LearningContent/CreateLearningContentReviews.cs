using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews.LearningContent;

public sealed record CreateLearningContentReviewsCommand(string Slug,
    IReadOnlyCollection<string> CandidateKeys, Guid SubmissionId);
public sealed record LearningReviewCandidateSource(Guid Id, string Key, string Prompt, string Answer);
public sealed record LearningReviewContentSource(Guid Id, string Title,
    IReadOnlyList<LearningReviewCandidateSource> Candidates);
public sealed record ExistingLearningReview(Guid CandidateId, Guid ReviewItemId);
public sealed record LearningContentReviewBatchItem(string CandidateKey, Guid ReviewItemId, bool WasCreated);
public sealed record LearningContentReviewBatchResult(int CreatedCount, int ExistingCount,
    IReadOnlyList<LearningContentReviewBatchItem> Items);

public interface ILearningContentReviewRepository
{
    Task<LearningContentReviewBatchResult?> GetSubmissionAsync(Guid userId, Guid submissionId,
        CancellationToken cancellationToken);
    Task<LearningReviewContentSource?> GetPublishedContentAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExistingLearningReview>> GetActiveReviewsAsync(Guid userId,
        IReadOnlyCollection<Guid> candidateIds, CancellationToken cancellationToken);
    void AddBatch(LearningContentReviewSubmission submission,
        IReadOnlyCollection<LearningContentReviewSubmissionItem> submissionItems,
        IReadOnlyCollection<ReviewItem> reviewItems,
        IReadOnlyCollection<ReviewLearningContentSource> sources);
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class CreateLearningContentReviewsHandler(ILearningContentReviewRepository repository,
    ICurrentUser currentUser, IUtcClock utcClock)
{
    public async Task<LearningContentReviewBatchResult> HandleAsync(CreateLearningContentReviewsCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.IsAuthenticated && currentUser.UserId.HasValue
            ? currentUser.UserId.Value
            : throw new UnauthorizedException("IDENTITY_UNAUTHENTICATED", "Authentication is required.");
        var keys = Validate(command);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var previous = await repository.GetSubmissionAsync(userId, command.SubmissionId, cancellationToken);
            if (previous is not null) return previous;

            var content = await repository.GetPublishedContentAsync(command.Slug.Trim().ToLowerInvariant(), cancellationToken)
                ?? throw new NotFoundException("LEARNING_CONTENT_NOT_FOUND", "Learning content was not found.");
            var candidatesByKey = content.Candidates.ToDictionary(candidate => candidate.Key, StringComparer.Ordinal);
            var unknown = keys.Where(key => !candidatesByKey.ContainsKey(key)).ToArray();
            if (unknown.Length > 0) throw new ValidationException(new Dictionary<string, string[]>
            { ["candidateKeys"] = ["One or more review candidates are not available for this lesson."] });

            var candidates = keys.Select(key => candidatesByKey[key]).ToArray();
            var existing = (await repository.GetActiveReviewsAsync(userId,
                candidates.Select(candidate => candidate.Id).ToArray(), cancellationToken))
                .ToDictionary(item => item.CandidateId);
            var now = utcClock.UtcNow;
            var reviewItems = new List<ReviewItem>();
            var sources = new List<ReviewLearningContentSource>();
            var items = new List<LearningContentReviewBatchItem>();
            foreach (var candidate in candidates)
            {
                if (existing.TryGetValue(candidate.Id, out var active))
                {
                    items.Add(new(candidate.Key, active.ReviewItemId, false));
                    continue;
                }
                var reviewItemId = Guid.NewGuid();
                reviewItems.Add(ReviewItem.Create(reviewItemId, userId, ReviewResourceType.LearningContent,
                    candidate.Id, now, now));
                sources.Add(ReviewLearningContentSource.Create(reviewItemId, userId, content.Id,
                    candidate.Id, candidate.Key, content.Title, candidate.Prompt, candidate.Answer, now));
                items.Add(new(candidate.Key, reviewItemId, true));
            }
            var createdCount = items.Count(item => item.WasCreated);
            var submission = LearningContentReviewSubmission.Create(userId, command.SubmissionId,
                content.Id, createdCount, items.Count - createdCount, now);
            var submissionItems = items.Select(item => LearningContentReviewSubmissionItem.Create(
                userId, command.SubmissionId, item.ReviewItemId, item.CandidateKey, item.WasCreated)).ToArray();
            repository.AddBatch(submission, submissionItems, reviewItems, sources);
            if (await repository.TrySaveChangesAsync(cancellationToken))
                return new(createdCount, items.Count - createdCount, items);
        }
        throw new ConflictException("REVIEW_LEARNING_CONTENT_CONFLICT",
            "The selected review concepts changed while they were being added. Please retry.");
    }

    private static string[] Validate(CreateLearningContentReviewsCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        var keys = command.CandidateKeys.Select(key => key?.Trim() ?? string.Empty).ToArray();
        if (command.SubmissionId == Guid.Empty) errors["submissionId"] = ["Submission ID cannot be empty."];
        if (keys.Length is < 1 or > 5) errors["candidateKeys"] = ["Select between one and five review candidates."];
        else if (keys.Any(string.IsNullOrWhiteSpace) || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
            errors["candidateKeys"] = ["Review candidate keys must be non-empty and unique."];
        if (errors.Count > 0) throw new ValidationException(errors);
        return keys;
    }
}
