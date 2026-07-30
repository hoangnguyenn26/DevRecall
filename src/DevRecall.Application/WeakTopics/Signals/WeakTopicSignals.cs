using DevRecall.Domain.WeakTopics;

namespace DevRecall.Application.WeakTopics.Signals;

public sealed record WeakTopicSignalReadModel(
    WeaknessSignalType SignalType, DateTimeOffset OccurredAtUtc);

public interface IWeakTopicSignalReader
{
    Task<IReadOnlyList<WeakTopicSignalReadModel>> ReadAsync(
        Guid userId, WeakTopicResourceType resourceType, Guid resourceId,
        DateTimeOffset fromUtc, DateTimeOffset toUtc,
        CancellationToken cancellationToken);
}
