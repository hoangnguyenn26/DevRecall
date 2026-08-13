using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Knowledge.Tags;

namespace DevRecall.Application.Knowledge.SaveLearningContent;

public sealed record SaveLearningContentToKnowledgeCommand(string Slug, string Title,
    string Content, Guid? TopicId, IReadOnlyCollection<Guid> TagIds, Guid SubmissionId);

public sealed record SavedKnowledgeReadModel(Guid Id, string Title, bool AlreadyExisted);
public sealed record PublishedLearningContentSource(Guid Id, string Title);
public sealed record ExistingLearningContentKnowledge(Guid KnowledgeNodeId, string Title);

public interface ILearningContentKnowledgeRepository
{
    Task<ExistingLearningContentKnowledge?> GetBySubmissionAsync(Guid userId, Guid submissionId,
        CancellationToken cancellationToken);
    Task<PublishedLearningContentSource?> GetPublishedContentAsync(string slug,
        CancellationToken cancellationToken);
    Task<bool> TopicExistsAsync(Guid userId, Guid topicId, CancellationToken cancellationToken);
    Task<int> CountAvailableTagsAsync(Guid userId, IReadOnlyCollection<Guid> tagIds,
        CancellationToken cancellationToken);
    Task<int> GetNextSortOrderAsync(Guid userId, Guid? topicId, CancellationToken cancellationToken);
    void Add(KnowledgeNode node, KnowledgeSource source, IReadOnlyCollection<KnowledgeNodeTag> tags);
    Task<Guid?> SaveChangesAsync(Guid userId, Guid submissionId, CancellationToken cancellationToken);
}

public sealed class SaveLearningContentToKnowledgeHandler(ILearningContentKnowledgeRepository repository,
    ICurrentUser currentUser, IUtcClock utcClock)
{
    public async Task<SavedKnowledgeReadModel> HandleAsync(SaveLearningContentToKnowledgeCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.IsAuthenticated && currentUser.UserId.HasValue
            ? currentUser.UserId.Value
            : throw new UnauthorizedException("IDENTITY_UNAUTHENTICATED", "Authentication is required.");
        Validate(command);

        var existing = await repository.GetBySubmissionAsync(userId, command.SubmissionId, cancellationToken);
        if (existing is not null)
            return new(existing.KnowledgeNodeId, existing.Title, true);

        var sourceContent = await repository.GetPublishedContentAsync(command.Slug.Trim().ToLowerInvariant(), cancellationToken)
            ?? throw new NotFoundException("LEARNING_CONTENT_NOT_FOUND", "Learning content was not found.");
        if (command.TopicId.HasValue
            && !await repository.TopicExistsAsync(userId, command.TopicId.Value, cancellationToken))
            throw new NotFoundException("KNOWLEDGE_TOPIC_NOT_FOUND", "The selected knowledge topic was not found.");

        var tagIds = command.TagIds.Distinct().Order().ToArray();
        if (tagIds.Length > 0
            && await repository.CountAvailableTagsAsync(userId, tagIds, cancellationToken) != tagIds.Length)
            throw new NotFoundException("KNOWLEDGE_TAG_NOT_FOUND", "One or more selected tags are not available.");

        var now = utcClock.UtcNow;
        var knowledgeId = Guid.NewGuid();
        var node = KnowledgeNode.Create(knowledgeId, userId, command.TopicId, command.Title,
            await repository.GetNextSortOrderAsync(userId, command.TopicId, cancellationToken), now);
        node.UpdateContent(command.Content, now);
        var source = KnowledgeSource.FromLearningContent(knowledgeId, userId, sourceContent.Id,
            command.SubmissionId, sourceContent.Title, now);
        repository.Add(node, source, tagIds.Select(tagId => KnowledgeNodeTag.Create(knowledgeId, tagId, now)).ToArray());
        var racedKnowledgeId = await repository.SaveChangesAsync(userId, command.SubmissionId, cancellationToken);
        if (racedKnowledgeId.HasValue)
        {
            var raced = await repository.GetBySubmissionAsync(userId, command.SubmissionId, cancellationToken)
                ?? throw new InvalidOperationException("The idempotent Knowledge submission could not be reloaded.");
            return new(raced.KnowledgeNodeId, raced.Title, true);
        }
        return new(node.Id, node.Title, false);
    }

    private static void Validate(SaveLearningContentToKnowledgeCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.Title)) errors["title"] = ["Title is required."];
        else if (command.Title.Trim().Length > 200) errors["title"] = ["Title cannot exceed 200 characters."];
        if (command.Content?.Trim().Length > 100_000) errors["content"] = ["Content cannot exceed 100000 characters."];
        if (command.SubmissionId == Guid.Empty) errors["submissionId"] = ["Submission ID cannot be empty."];
        if (command.TopicId == Guid.Empty) errors["topicId"] = ["Topic ID cannot be empty."];
        if (command.TagIds.Count > 10) errors["tagIds"] = ["A knowledge item can have at most 10 tags."];
        else if (command.TagIds.Any(id => id == Guid.Empty)) errors["tagIds"] = ["Tag IDs cannot be empty."];
        if (errors.Count > 0) throw new ValidationException(errors);
    }
}
