using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Knowledge.Tags;
using DevRecall.Application.Knowledge.Tags.Archive;
using DevRecall.Application.Knowledge.Tags.Create;
using DevRecall.Application.Knowledge.Tags.GetList;
using DevRecall.Application.Knowledge.Tags.Rename;
using DevRecall.Domain.Knowledge.Tags;
using FluentAssertions;

namespace DevRecall.Application.Tests.Knowledge.Tags;

public sealed class TagHandlerTests
{
    [Fact]
    public async Task Create_ShouldUseCurrentUserAndNormalizedName()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeTagRepository();
        var handler = new CreateTagHandler(repository, CurrentUser(userId));

        var result = await handler.HandleAsync(
            new CreateTagCommand("  ASP.NET   Core  "),
            CancellationToken.None);

        result.Name.Should().Be("ASP.NET Core");
        repository.AddedTag!.UserId.Should().Be(userId);
        repository.CheckedNormalizedName.Should().Be("ASP.NET CORE");
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Create_WhenNameExists_ShouldThrowConflict()
    {
        var repository = new FakeTagRepository { DuplicateExists = true };
        var handler = new CreateTagHandler(repository, CurrentUser(Guid.NewGuid()));

        var action = () => handler.HandleAsync(
            new CreateTagCommand("Interview"),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ConflictException>();
        exception.Which.ErrorCode.Should().Be("TAG_NAME_ALREADY_EXISTS");
    }

    [Fact]
    public async Task Rename_ForAnotherUser_ShouldThrowNotFound()
    {
        var tag = CreateTag(Guid.NewGuid(), "Interview");
        var repository = new FakeTagRepository(tag);
        var handler = new RenameTagHandler(repository, CurrentUser(Guid.NewGuid()));

        var action = () => handler.HandleAsync(
            new RenameTagCommand(tag.Id, "Backend"),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<NotFoundException>();
        exception.Which.ErrorCode.Should().Be("TAG_NOT_FOUND");
    }

    [Fact]
    public async Task Rename_ArchivedTag_ShouldThrowConflict()
    {
        var userId = Guid.NewGuid();
        var tag = CreateTag(userId, "Interview");
        tag.Archive(DateTimeOffset.UtcNow);
        var handler = new RenameTagHandler(
            new FakeTagRepository(tag),
            CurrentUser(userId));

        var action = () => handler.HandleAsync(
            new RenameTagCommand(tag.Id, "Backend"),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ConflictException>();
        exception.Which.ErrorCode.Should().Be("TAG_ARCHIVED");
    }

    [Fact]
    public async Task Archive_WhenAlreadyArchived_ShouldNotSaveAgain()
    {
        var userId = Guid.NewGuid();
        var tag = CreateTag(userId, "Interview");
        tag.Archive(DateTimeOffset.UtcNow);
        var repository = new FakeTagRepository(tag);
        var handler = new ArchiveTagHandler(repository, CurrentUser(userId));

        await handler.HandleAsync(
            new ArchiveTagCommand(tag.Id),
            CancellationToken.None);

        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task GetList_ShouldMapRepositoryTags()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeTagRepository(
            CreateTag(userId, "Backend"),
            CreateTag(userId, "Interview"));
        var handler = new GetTagsHandler(repository, CurrentUser(userId));

        var result = await handler.HandleAsync(CancellationToken.None);

        result.Select(tag => tag.Name).Should().Equal("Backend", "Interview");
    }

    private static FakeCurrentUser CurrentUser(Guid userId) =>
        new FakeCurrentUser(userId);

    private static Tag CreateTag(Guid userId, string name) =>
        Tag.Create(Guid.NewGuid(), userId, name, DateTimeOffset.UtcNow);

    private sealed class FakeCurrentUser(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
    }

    private sealed class FakeTagRepository(params Tag[] tags) : ITagRepository
    {
        public bool DuplicateExists { get; init; }
        public string? CheckedNormalizedName { get; private set; }
        public Tag? AddedTag { get; private set; }
        public int SaveCount { get; private set; }

        public Task<bool> ExistsByNormalizedNameAsync(
            Guid userId,
            string normalizedName,
            Guid? excludedTagId,
            CancellationToken cancellationToken)
        {
            CheckedNormalizedName = normalizedName;
            return Task.FromResult(DuplicateExists);
        }

        public Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(tags.SingleOrDefault(tag => tag.Id == id));

        public Task<IReadOnlyList<Tag>> GetActiveByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Tag>>(
                tags.Where(tag => tag.UserId == userId
                    && tag.Status == TagStatus.Active)
                    .ToList());

        public void Add(Tag tag) => AddedTag = tag;

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
