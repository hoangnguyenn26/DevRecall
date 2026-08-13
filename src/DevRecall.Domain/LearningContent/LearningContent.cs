using DevRecall.Domain.LearningProfiles;

namespace DevRecall.Domain.LearningContent;

public sealed class LearningContent
{
    public const int MaximumTechnologies = 20;
    public const int MaximumTopics = 20;
    public const int MaximumObjectives = 10;
    public const int MaximumSections = 50;
    public const int MaximumReviewCandidates = 5;
    public const int MaximumSectionBodyLength = 50_000;

    private readonly List<LearningContentTechnology> _technologies = [];
    private readonly List<LearningContentTopic> _topics = [];
    private readonly List<LearningObjective> _objectives = [];
    private readonly List<LearningContentSection> _sections = [];
    private readonly List<LearningReviewCandidate> _reviewCandidates = [];
    private LearningContent() { }

    public Guid Id { get; private set; }
    public string Slug { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Summary { get; private set; } = null!;
    public LearningContentType ContentType { get; private set; }
    public ContentDifficulty Difficulty { get; private set; }
    public int EstimatedMinutes { get; private set; }
    public ContentStatus Status { get; private set; }
    public ContentSourceType SourceType { get; private set; }
    public string SourceName { get; private set; } = null!;
    public string? SourceUrl { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public IReadOnlyCollection<LearningContentTechnology> Technologies => _technologies.AsReadOnly();
    public IReadOnlyCollection<LearningContentTopic> Topics => _topics.AsReadOnly();
    public IReadOnlyCollection<LearningObjective> Objectives => _objectives.AsReadOnly();
    public IReadOnlyCollection<LearningContentSection> Sections => _sections.AsReadOnly();
    public IReadOnlyCollection<LearningReviewCandidate> ReviewCandidates => _reviewCandidates.AsReadOnly();

    public static LearningContent CreateDraft(Guid id, string slug, string title, string summary,
        LearningContentType contentType, ContentDifficulty difficulty, int estimatedMinutes,
        ContentSourceType sourceType, string sourceName, string? sourceUrl,
        IReadOnlyCollection<Technology> technologies, IReadOnlyCollection<Guid> topicIds,
        IReadOnlyCollection<LearningObjectiveInput> objectives,
        IReadOnlyCollection<LearningContentSectionInput> sections, DateTimeOffset currentUtc,
        IReadOnlyCollection<LearningReviewCandidateInput>? reviewCandidates = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Learning content id cannot be empty.", nameof(id));
        EnsureUtc(currentUtc);
        ValidateEnums(contentType, difficulty, sourceType);
        if (estimatedMinutes is < 1 or > 480)
            throw new ArgumentOutOfRangeException(nameof(estimatedMinutes), "Estimated minutes must be between 1 and 480.");
        var normalizedSourceUrl = ValidateSource(sourceType, sourceUrl);
        ValidateCollections(technologies, topicIds, objectives, sections, reviewCandidates ?? []);

        var content = new LearningContent
        {
            Id = id,
            Slug = LearningContentText.NormalizeSlug(slug),
            Title = LearningContentText.NormalizeRequired(title, nameof(title), 200, 3),
            Summary = LearningContentText.NormalizeRequired(summary, nameof(summary), 500),
            ContentType = contentType,
            Difficulty = difficulty,
            EstimatedMinutes = estimatedMinutes,
            Status = ContentStatus.Draft,
            SourceType = sourceType,
            SourceName = LearningContentText.NormalizeRequired(sourceName, nameof(sourceName), 150),
            SourceUrl = normalizedSourceUrl,
            Version = 1,
            CreatedAtUtc = currentUtc,
            UpdatedAtUtc = currentUtc
        };
        content.ReplaceChildren(technologies, topicIds, objectives, sections, reviewCandidates ?? []);
        return content;
    }

    public void Publish(DateTimeOffset currentUtc)
    {
        EnsureUtc(currentUtc);
        if (Status is not (ContentStatus.Draft or ContentStatus.Archived))
            throw new InvalidOperationException("Only Draft or Archived content can be published.");
        if (Topics.Count == 0) throw new InvalidOperationException("Published content requires at least one topic.");
        if (ContentType == LearningContentType.Lesson && Objectives.Count == 0)
            throw new InvalidOperationException("A published lesson requires at least one objective.");
        if (ContentType == LearningContentType.Lesson && Sections.Count == 0)
            throw new InvalidOperationException("A published lesson requires at least one section.");
        if (ContentType == LearningContentType.ExternalResource && SourceType != ContentSourceType.External)
            throw new InvalidOperationException("External resources require external provenance.");
        Status = ContentStatus.Published;
        PublishedAtUtc ??= currentUtc;
        UpdatedAtUtc = currentUtc;
        Version = checked(Version + 1);
    }

    public void Archive(DateTimeOffset currentUtc)
    {
        EnsureUtc(currentUtc);
        if (Status != ContentStatus.Published)
            throw new InvalidOperationException("Only Published content can be archived.");
        Status = ContentStatus.Archived;
        UpdatedAtUtc = currentUtc;
        Version = checked(Version + 1);
    }

    private void ReplaceChildren(IReadOnlyCollection<Technology> technologies,
        IReadOnlyCollection<Guid> topicIds, IReadOnlyCollection<LearningObjectiveInput> objectives,
        IReadOnlyCollection<LearningContentSectionInput> sections,
        IReadOnlyCollection<LearningReviewCandidateInput> reviewCandidates)
    {
        _technologies.AddRange(technologies.Order().Select(value =>
            new LearningContentTechnology(Guid.NewGuid(), Id, value)));
        _topics.AddRange(topicIds.Order().Select(topicId => new LearningContentTopic(Id, topicId)));
        _objectives.AddRange(objectives.Select((item, position) => new LearningObjective(
            Guid.NewGuid(), Id, position,
            LearningContentText.NormalizeRequired(item.Text, "objective", 300))));
        _sections.AddRange(sections.Select((item, position) => new LearningContentSection(
            Guid.NewGuid(), Id, position, item.SectionType,
            LearningContentText.NormalizeOptional(item.Heading, "heading", 200),
            LearningContentText.NormalizeRequired(item.BodyMarkdown, "bodyMarkdown", MaximumSectionBodyLength))));
        _reviewCandidates.AddRange(reviewCandidates.Select((item, position) => LearningReviewCandidate.Create(
            Guid.NewGuid(), Id, position, item.Key, item.Prompt, item.Answer)));
    }

    private static void ValidateCollections(IReadOnlyCollection<Technology> technologies,
        IReadOnlyCollection<Guid> topicIds, IReadOnlyCollection<LearningObjectiveInput> objectives,
        IReadOnlyCollection<LearningContentSectionInput> sections,
        IReadOnlyCollection<LearningReviewCandidateInput> reviewCandidates)
    {
        ArgumentNullException.ThrowIfNull(technologies);
        ArgumentNullException.ThrowIfNull(topicIds);
        ArgumentNullException.ThrowIfNull(objectives);
        ArgumentNullException.ThrowIfNull(sections);
        ArgumentNullException.ThrowIfNull(reviewCandidates);
        if (technologies.Count > MaximumTechnologies || technologies.Distinct().Count() != technologies.Count
            || technologies.Any(value => !Enum.IsDefined(value)))
            throw new ArgumentException("Technologies must be unique canonical values.", nameof(technologies));
        if (topicIds.Count > MaximumTopics || topicIds.Any(id => id == Guid.Empty)
            || topicIds.Distinct().Count() != topicIds.Count)
            throw new ArgumentException("Topics must be unique non-empty ids.", nameof(topicIds));
        if (objectives.Count > MaximumObjectives)
            throw new ArgumentException("At most ten objectives are allowed.", nameof(objectives));
        if (sections.Count > MaximumSections || sections.Any(item => !Enum.IsDefined(item.SectionType)))
            throw new ArgumentException("Sections must contain at most fifty valid items.", nameof(sections));
        if (reviewCandidates.Count > MaximumReviewCandidates)
            throw new ArgumentException("A lesson can have at most five review candidates.", nameof(reviewCandidates));
        var keys = reviewCandidates.Select(item => LearningReviewCandidate.NormalizeKey(item.Key)).ToArray();
        if (keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
            throw new ArgumentException("Review candidate keys must be unique.", nameof(reviewCandidates));
    }

    private static string? ValidateSource(ContentSourceType sourceType, string? sourceUrl)
    {
        if (sourceType == ContentSourceType.Internal)
        {
            if (!string.IsNullOrWhiteSpace(sourceUrl))
                throw new ArgumentException("Internal content cannot have a source URL.", nameof(sourceUrl));
            return null;
        }
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri)
            || (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("External content requires an HTTP or HTTPS source URL.", nameof(sourceUrl));
        return uri.AbsoluteUri;
    }

    private static void ValidateEnums(LearningContentType contentType,
        ContentDifficulty difficulty, ContentSourceType sourceType)
    {
        if (!Enum.IsDefined(contentType)) throw new ArgumentOutOfRangeException(nameof(contentType));
        if (!Enum.IsDefined(difficulty)) throw new ArgumentOutOfRangeException(nameof(difficulty));
        if (!Enum.IsDefined(sourceType)) throw new ArgumentOutOfRangeException(nameof(sourceType));
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", nameof(value));
    }
}
