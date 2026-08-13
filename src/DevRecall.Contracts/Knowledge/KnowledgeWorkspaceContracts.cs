namespace DevRecall.Contracts.Knowledge;

public sealed record GetKnowledgeRequest(
    Guid? TopicId, string? TopicScope, string? Query, string? Sort,
    string? TagIds, int Page = 1, int PageSize = 30);

public sealed record KnowledgeWorkspaceTagResponse(Guid Id, string Name);

public sealed record KnowledgeWorkspaceListItemResponse(
    Guid Id, string Title, string Summary, Guid? TopicId, string? TopicName,
    IReadOnlyList<KnowledgeWorkspaceTagResponse> Tags,
    int TagCount, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, int Version);

public sealed record RelatedKnowledgeResponse(
    Guid Id, string Title, string? TopicName, int SharedTagCount,
    bool SameTopic, DateTimeOffset UpdatedAtUtc);

public sealed record KnowledgeWorkspaceDetailResponse(
    Guid Id, string Title, string Content, string? Description, string? SourceUrl,
    Guid? TopicId, string? TopicName, IReadOnlyList<KnowledgeWorkspaceTagResponse> Tags,
    IReadOnlyList<RelatedKnowledgeResponse> RelatedItems,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, int Version,
    KnowledgeSourceResponse? Source);

public sealed record KnowledgeSourceResponse(string Type, string Title, string? Slug, bool IsAvailable);

public sealed record SaveLearningContentToKnowledgeRequest(string Title, string Content,
    Guid? TopicId, IReadOnlyList<Guid> TagIds, Guid SubmissionId);

public sealed record SavedKnowledgeResponse(Guid Id, string Title, bool AlreadyExisted);

public sealed record UpdateKnowledgeRequest(
    string Title, string Content, Guid? TopicId,
    IReadOnlyList<Guid> TagIds, int ExpectedVersion);

public sealed record KnowledgeTagSummaryResponse(
    Guid Id, string Name, string NormalizedName, int KnowledgeCount);

public sealed record CreateKnowledgeTagRequest(string Name);

public sealed record KnowledgeTopicResponse(
    Guid Id, Guid? ParentId, string Name,
    int DirectKnowledgeCount, int DescendantKnowledgeCount,
    int TotalKnowledgeCount, int ChildCount,
    IReadOnlyList<KnowledgeTopicResponse> Children);

public sealed record KnowledgeTopicTreeResponse(
    IReadOnlyList<KnowledgeTopicResponse> Items,
    int TotalKnowledgeCount, int UncategorizedCount);
