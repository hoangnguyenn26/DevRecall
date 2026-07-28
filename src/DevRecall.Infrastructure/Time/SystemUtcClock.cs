using DevRecall.Application.Common.Time;

namespace DevRecall.Infrastructure.Time;

internal sealed class SystemUtcClock : IUtcClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
