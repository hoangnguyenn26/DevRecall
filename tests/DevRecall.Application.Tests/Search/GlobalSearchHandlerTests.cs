using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Search;
using FluentAssertions;

namespace DevRecall.Application.Tests.Search;

public sealed class GlobalSearchHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task HandleAsync_ShouldNormalizeQueryAndUseBoundedReader()
    {
        var reader = new ReaderStub(new GlobalSearchReadModel([], false));
        var handler = new GlobalSearchHandler(reader, new CurrentUserStub(UserId));

        var result = await handler.HandleAsync(
            new GlobalSearchQuery("  dependency injection  ", 4),
            CancellationToken.None);

        result.Query.Should().Be("dependency injection");
        reader.UserId.Should().Be(UserId);
        reader.Query.Should().Be("dependency injection");
        reader.TakePerType.Should().Be(4);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    public async Task HandleAsync_WithShortText_ShouldRejectRequest(string text)
    {
        var action = () => CreateHandler().HandleAsync(
            new GlobalSearchQuery(text), CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_WithTextLongerThanMaximum_ShouldRejectRequest()
    {
        var action = () => CreateHandler().HandleAsync(
            new GlobalSearchQuery(new string('a', 201)), CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public async Task HandleAsync_WithInvalidTakePerType_ShouldRejectRequest(int take)
    {
        var action = () => CreateHandler().HandleAsync(
            new GlobalSearchQuery("query", take), CancellationToken.None);
        var exception = await action.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainKey("takePerType");
    }

    [Fact]
    public async Task HandleAsync_ShouldMapTrustedTargetPathsAndPublicTypes()
    {
        var candidates = new[]
        {
            Candidate(GlobalSearchResourceType.Knowledge),
            Candidate(GlobalSearchResourceType.InterviewQuestion),
            Candidate(GlobalSearchResourceType.DsaProblem)
        };
        var handler = new GlobalSearchHandler(
            new ReaderStub(new GlobalSearchReadModel(candidates, true)),
            new CurrentUserStub(UserId));

        var result = await handler.HandleAsync(
            new GlobalSearchQuery("query"), CancellationToken.None);

        result.HasMore.Should().BeTrue();
        result.Items.Select(item => item.ResourceType).Should().Equal(
            "Knowledge", "InterviewQuestion", "DsaProblem");
        result.Items.Select(item => item.TargetPath).Should().Equal(
            $"/app/knowledge/{candidates[0].ResourceId}",
            $"/app/interview/{candidates[1].ResourceId}",
            $"/app/dsa/{candidates[2].ResourceId}");
        result.Items.Should().OnlyContain(item => item.Highlights.Count == 0);
    }

    private static GlobalSearchHandler CreateHandler() =>
        new(new ReaderStub(new GlobalSearchReadModel([], false)),
            new CurrentUserStub(UserId));

    private static GlobalSearchCandidate Candidate(GlobalSearchResourceType type) =>
        new(Guid.NewGuid(), type, $"{type} title", "Safe summary", 1m,
            DateTimeOffset.UtcNow, true, true);

    private sealed class ReaderStub(GlobalSearchReadModel result) : IGlobalSearchReader
    {
        public Guid UserId { get; private set; }
        public string? Query { get; private set; }
        public int TakePerType { get; private set; }

        public Task<GlobalSearchReadModel> SearchAsync(
            Guid userId, string query, int takePerType,
            CancellationToken cancellationToken)
        {
            UserId = userId;
            Query = query;
            TakePerType = takePerType;
            return Task.FromResult(result);
        }
    }

    private sealed class CurrentUserStub(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId { get; } = userId;
    }
}
