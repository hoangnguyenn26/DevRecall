using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Knowledge.Workspace;
using FluentAssertions;

namespace DevRecall.Application.Tests.Knowledge.Workspace;

public sealed class KnowledgeWorkspaceHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task List_ShouldNormalizeQueryAndUseDefaults()
    {
        var reader = new ReaderStub();
        var handler = new GetKnowledgeListHandler(reader, new CurrentUserStub());

        await handler.HandleAsync(new GetKnowledgeListQuery(
            null, null, "  dependency injection  ", null), CancellationToken.None);

        reader.Filter.Should().Be(new KnowledgeListFilter(
            null, false, "dependency injection", KnowledgeListSort.Updated, 1, 30, []));
    }

    [Fact]
    public async Task List_WithTopicAndUncategorized_ShouldReturnStableBadRequest()
    {
        var handler = new GetKnowledgeListHandler(new ReaderStub(), new CurrentUserStub());

        var action = () => handler.HandleAsync(new GetKnowledgeListQuery(
            Guid.NewGuid(), "uncategorized", null, null), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<BadRequestException>();
        exception.Which.ErrorCode.Should().Be("KNOWLEDGE_INVALID_TOPIC_FILTER");
    }

    [Fact]
    public async Task List_WithUnknownTopic_ShouldNotLeakOwnership()
    {
        var reader = new ReaderStub { TopicExists = false };
        var handler = new GetKnowledgeListHandler(reader, new CurrentUserStub());

        var action = () => handler.HandleAsync(new GetKnowledgeListQuery(
            Guid.NewGuid(), null, null, "title"), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<NotFoundException>();
        exception.Which.ErrorCode.Should().Be("KNOWLEDGE_TOPIC_NOT_FOUND");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("descending")]
    public async Task List_WithUnsupportedSort_ShouldRejectIt(string sort)
    {
        var handler = new GetKnowledgeListHandler(new ReaderStub(), new CurrentUserStub());
        var action = () => handler.HandleAsync(new GetKnowledgeListQuery(
            null, null, null, sort), CancellationToken.None);
        (await action.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().ContainKey("sort");
    }

    private sealed class CurrentUserStub : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => KnowledgeWorkspaceHandlerTests.UserId;
    }

    private sealed class ReaderStub : IKnowledgeWorkspaceReader
    {
        public bool TopicExists { get; init; } = true;
        public KnowledgeListFilter? Filter { get; private set; }
        public Task<bool> TopicExistsAsync(Guid userId, Guid topicId, CancellationToken cancellationToken) => Task.FromResult(TopicExists);
        public Task<KnowledgeListReadResult> GetListAsync(Guid userId, KnowledgeListFilter filter, CancellationToken cancellationToken)
        {
            userId.Should().Be(UserId); Filter = filter;
            return Task.FromResult(new KnowledgeListReadResult([], filter.Page, filter.PageSize, 0, 0));
        }
        public Task<KnowledgeDetailReadModel?> GetDetailAsync(Guid userId, Guid knowledgeId, CancellationToken cancellationToken) => Task.FromResult<KnowledgeDetailReadModel?>(null);
        public Task<KnowledgeTopicTreeReadModel> GetTopicTreeAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(new KnowledgeTopicTreeReadModel([], 0, 0));
    }
}
