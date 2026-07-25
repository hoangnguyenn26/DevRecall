using DevRecall.Domain.Common.Errors;

namespace DevRecall.Domain.Knowledge.Tags;

public static class TagErrors
{
    public static readonly DomainError NotFound =
        new("TAG_NOT_FOUND", "The tag was not found.");

    public static readonly DomainError NameAlreadyExists =
        new("TAG_NAME_ALREADY_EXISTS", "A tag with this name already exists.");

    public static readonly DomainError Archived =
        new("TAG_ARCHIVED", "The archived tag cannot be modified.");
}
