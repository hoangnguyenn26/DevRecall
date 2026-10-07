using DevRecall.Application.Knowledge.SaveLearningContent;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Knowledge.Tags;
using DevRecall.Domain.LearningContent;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevRecall.Infrastructure.Knowledge;

internal sealed class LearningContentKnowledgeRepository(DevRecallDbContext dbContext)
    : ILearningContentKnowledgeRepository
{
    public Task<ExistingLearningContentKnowledge?> GetBySubmissionAsync(Guid userId, Guid submissionId,
        CancellationToken cancellationToken) => (
            from source in dbContext.KnowledgeSources.AsNoTracking()
            join node in dbContext.KnowledgeNodes.AsNoTracking() on source.KnowledgeNodeId equals node.Id
            where source.UserId == userId && source.SubmissionId == submissionId
            select new ExistingLearningContentKnowledge(node.Id, node.Title))
        .SingleOrDefaultAsync(cancellationToken);

    public Task<PublishedLearningContentSource?> GetPublishedContentAsync(string slug,
        CancellationToken cancellationToken) => dbContext.LearningContents.AsNoTracking()
        .Where(content => content.Slug == slug && content.Status == ContentStatus.Published
            && content.ContentType == LearningContentType.Lesson)
        .Select(content => new PublishedLearningContentSource(content.Id, content.Title))
        .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> TopicExistsAsync(Guid userId, Guid topicId, CancellationToken cancellationToken) =>
        dbContext.KnowledgeNodes.AsNoTracking().AnyAsync(node => node.Id == topicId
            && node.UserId == userId && node.Status == KnowledgeNodeStatus.Active, cancellationToken);

    public Task<int> CountAvailableTagsAsync(Guid userId, IReadOnlyCollection<Guid> tagIds,
        CancellationToken cancellationToken) => dbContext.Tags.AsNoTracking().CountAsync(tag =>
            tag.UserId == userId && tag.Status == TagStatus.Active && tagIds.Contains(tag.Id), cancellationToken);

    public async Task<int> GetNextSortOrderAsync(Guid userId, Guid? topicId,
        CancellationToken cancellationToken)
    {
        var maximum = await dbContext.KnowledgeNodes.AsNoTracking()
            .Where(node => node.UserId == userId && node.ParentId == topicId
                && node.Status == KnowledgeNodeStatus.Active)
            .Select(node => (int?)node.SortOrder).MaxAsync(cancellationToken);
        return maximum is null ? 0 : maximum.Value + 1;
    }

    public void Add(KnowledgeNode node, KnowledgeSource source, IReadOnlyCollection<KnowledgeNodeTag> tags)
    {
        dbContext.KnowledgeNodes.Add(node);
        dbContext.KnowledgeSources.Add(source);
        dbContext.KnowledgeNodeTags.AddRange(tags);
    }

    public async Task<Guid?> SaveChangesAsync(Guid userId, Guid submissionId,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_knowledge_sources_user_submission" })
        {
            dbContext.ChangeTracker.Clear();
            return await dbContext.KnowledgeSources.AsNoTracking()
                .Where(source => source.UserId == userId && source.SubmissionId == submissionId)
                .Select(source => (Guid?)source.KnowledgeNodeId).SingleAsync(cancellationToken);
        }
    }
}
