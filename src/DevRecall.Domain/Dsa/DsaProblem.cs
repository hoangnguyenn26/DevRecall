namespace DevRecall.Domain.Dsa;

public sealed class DsaProblem
{
    private readonly List<DsaProblemTopic> _topics = [];

    private DsaProblem()
    {
    }

    private DsaProblem(
        Guid id, Guid userId, string title, string description,
        DsaProblemDifficulty difficulty, string? source, string? externalUrl,
        IReadOnlyCollection<DsaProblemTopic> topics,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        Title = title;
        Description = description;
        Difficulty = difficulty;
        Source = source;
        ExternalUrl = externalUrl;
        _topics.AddRange(topics);
        Status = DsaProblemStatus.Active;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public DsaProblemDifficulty Difficulty { get; private set; }
    public string? Source { get; private set; }
    public string? ExternalUrl { get; private set; }
    public IReadOnlyCollection<DsaProblemTopic> Topics => _topics;
    public DsaProblemStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static DsaProblem Create(
        Guid id, Guid userId, string title, string description,
        DsaProblemDifficulty difficulty, string? source, string? externalUrl,
        IEnumerable<string>? topics, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "DSA problem id cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        EnsureValidDifficulty(difficulty);
        var normalizedTitle = DsaProblemText.NormalizeRequiredSingleLine(
            title, nameof(title), DsaProblemText.TitleMaxLength);
        var normalizedDescription = DsaProblemText.NormalizeRequiredMultiline(
            description, nameof(description), DsaProblemText.DescriptionMaxLength);
        var normalizedSource = DsaProblemText.NormalizeOptionalSingleLine(
            source, nameof(source), DsaProblemText.SourceMaxLength);

        return new DsaProblem(
            id, userId, normalizedTitle, normalizedDescription, difficulty,
            normalizedSource, NormalizeExternalUrl(externalUrl),
            NormalizeTopics(topics), createdAtUtc);
    }

    public bool Update(
        string title, string description, DsaProblemDifficulty difficulty,
        string? source, string? externalUrl, IEnumerable<string>? topics,
        DateTimeOffset updatedAtUtc)
    {
        EnsureActive();
        EnsureValidDifficulty(difficulty);
        var normalizedTitle = DsaProblemText.NormalizeRequiredSingleLine(
            title, nameof(title), DsaProblemText.TitleMaxLength);
        var normalizedDescription = DsaProblemText.NormalizeRequiredMultiline(
            description, nameof(description), DsaProblemText.DescriptionMaxLength);
        var normalizedSource = DsaProblemText.NormalizeOptionalSingleLine(
            source, nameof(source), DsaProblemText.SourceMaxLength);
        var normalizedExternalUrl = NormalizeExternalUrl(externalUrl);
        var normalizedTopics = NormalizeTopics(topics);

        if (Title == normalizedTitle && Description == normalizedDescription
            && Difficulty == difficulty && Source == normalizedSource
            && ExternalUrl == normalizedExternalUrl
            && TopicsEqual(normalizedTopics))
        {
            return false;
        }

        Title = normalizedTitle;
        Description = normalizedDescription;
        Difficulty = difficulty;
        Source = normalizedSource;
        ExternalUrl = normalizedExternalUrl;
        _topics.Clear();
        _topics.AddRange(normalizedTopics);
        UpdatedAtUtc = updatedAtUtc;
        return true;
    }

    public bool ReplaceTopics(
        IEnumerable<string>? topics, DateTimeOffset updatedAtUtc)
    {
        EnsureActive();
        var normalizedTopics = NormalizeTopics(topics);
        if (TopicsEqual(normalizedTopics))
        {
            return false;
        }

        _topics.Clear();
        _topics.AddRange(normalizedTopics);
        UpdatedAtUtc = updatedAtUtc;
        return true;
    }

    public bool Archive(DateTimeOffset updatedAtUtc)
    {
        if (Status == DsaProblemStatus.Archived)
        {
            return false;
        }

        Status = DsaProblemStatus.Archived;
        UpdatedAtUtc = updatedAtUtc;
        return true;
    }

    private static string? NormalizeExternalUrl(string? externalUrl)
    {
        if (string.IsNullOrWhiteSpace(externalUrl))
        {
            return null;
        }

        var normalized = externalUrl.Trim();
        if (normalized.Length > DsaProblemText.ExternalUrlMaxLength)
        {
            throw new ArgumentException(
                $"External URL cannot exceed {DsaProblemText.ExternalUrlMaxLength} characters.",
                nameof(externalUrl));
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp
                && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                DsaProblemErrors.InvalidExternalUrl.Message,
                nameof(externalUrl));
        }

        return uri.AbsoluteUri;
    }

    private static List<DsaProblemTopic> NormalizeTopics(
        IEnumerable<string>? topics)
    {
        if (topics is null)
        {
            return [];
        }

        var normalizedTopics = topics
            .Where(topic => !string.IsNullOrWhiteSpace(topic))
            .Select(DsaProblemTopic.Create)
            .GroupBy(topic => topic.NormalizedName, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(topic => topic.NormalizedName, StringComparer.Ordinal)
            .ToList();
        if (normalizedTopics.Count > DsaProblemTopic.MaximumTopicsPerProblem)
        {
            throw new ArgumentException(
                $"A DSA problem cannot have more than {DsaProblemTopic.MaximumTopicsPerProblem} topics.",
                nameof(topics));
        }

        return normalizedTopics;
    }

    private bool TopicsEqual(List<DsaProblemTopic> topics) =>
        _topics.Count == topics.Count
        && _topics.Select(topic => topic.NormalizedName).SequenceEqual(
            topics.Select(topic => topic.NormalizedName),
            StringComparer.Ordinal);

    private void EnsureActive()
    {
        if (Status == DsaProblemStatus.Archived)
        {
            throw new InvalidOperationException(DsaProblemErrors.Archived.Message);
        }
    }

    private static void EnsureValidDifficulty(
        DsaProblemDifficulty difficulty)
    {
        if (!Enum.IsDefined(difficulty))
        {
            throw new ArgumentOutOfRangeException(
                nameof(difficulty),
                DsaProblemErrors.InvalidDifficulty.Message);
        }
    }
}
