using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Search;
using FluentAssertions;

namespace DevRecall.Application.Tests.Search;

public sealed class GlobalSearchHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task HandleAsync_ShouldNormalizeModulesAndPageResults()
    {
        var reader = new ReaderStub(
            Enumerable.Range(1, 5).Select(index => Candidate(index)).ToList(), 5);
        var handler = new GlobalSearchHandler(reader, new CurrentUserStub(UserId));

        var result = await handler.HandleAsync(
            new GlobalSearchQuery("  dependency injection  ", ["knowledge"], 2, 2),
            CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].Title.Should().Be("Result 3");
        result.TotalPages.Should().Be(3);
        reader.Text.Should().Be("dependency injection");
        reader.Modules.Should().BeEquivalentTo("knowledge");
        reader.CandidateLimit.Should().Be(4);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    public async Task HandleAsync_WithShortText_ShouldRejectRequest(string text)
    {
        var handler = CreateHandler();
        var action = () => handler.HandleAsync(
            new GlobalSearchQuery(text, null), CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_WithUnsupportedModule_ShouldRejectRequest()
    {
        var handler = CreateHandler();
        var action = () => handler.HandleAsync(
            new GlobalSearchQuery("query", ["projectStory"]),
            CancellationToken.None);
        var exception = await action.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainKey("modules");
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public async Task HandleAsync_WithInvalidPagination_ShouldRejectRequest(
        int page, int pageSize)
    {
        var handler = CreateHandler();
        var action = () => handler.HandleAsync(
            new GlobalSearchQuery("query", null, page, pageSize),
            CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }

    private static GlobalSearchHandler CreateHandler() =>
        new(new ReaderStub([], 0), new CurrentUserStub(UserId));

    private static GlobalSearchCandidate Candidate(int index) =>
        new("Knowledge", Guid.NewGuid(), $"Result {index}", null,
            1d / index, new Dictionary<string, string>());

    private sealed class ReaderStub(
        IReadOnlyList<GlobalSearchCandidate> items, int totalCount)
        : IGlobalSearchReader
    {
        public string? Text { get; private set; }
        public IReadOnlySet<string>? Modules { get; private set; }
        public int CandidateLimit { get; private set; }

        public Task<GlobalSearchReadResult> SearchAsync(
            Guid userId, string text, IReadOnlySet<string> modules,
            int candidateLimit, CancellationToken cancellationToken)
        {
            userId.Should().Be(UserId);
            Text = text; Modules = modules; CandidateLimit = candidateLimit;
            return Task.FromResult(new GlobalSearchReadResult(items, totalCount));
        }
    }

    private sealed class CurrentUserStub(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId { get; } = userId;
    }
}
