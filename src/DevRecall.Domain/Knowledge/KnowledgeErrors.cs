using DevRecall.Domain.Common.Errors;

namespace DevRecall.Domain.Knowledge;

public static class KnowledgeErrors
{
    public static readonly DomainError NodeNotFound = new(
        "KNOWLEDGE_NODE_NOT_FOUND",
        "The knowledge node was not found.");

    public static readonly DomainError NodeArchived = new(
        "KNOWLEDGE_NODE_ARCHIVED",
        "An archived knowledge node cannot be modified.");

    public static readonly DomainError ParentNotFound = new(
        "KNOWLEDGE_PARENT_NOT_FOUND",
        "The parent knowledge node was not found.");

    public static readonly DomainError InvalidParent = new(
        "KNOWLEDGE_INVALID_PARENT",
        "The selected parent is invalid.");

    public static readonly DomainError CircularHierarchy = new(
        "KNOWLEDGE_CIRCULAR_HIERARCHY",
        "The operation would create a circular hierarchy.");

    public static readonly DomainError ConcurrentUpdate = new(
        "KNOWLEDGE_CONCURRENT_UPDATE",
        "The knowledge node was modified by another operation.");

    public static readonly DomainError InvalidOrder = new(
        "KNOWLEDGE_INVALID_ORDER",
        "The target knowledge node position is invalid.");
}
