using DevRecall.Application.Common.Time;

namespace DevRecall.Application.Tests.Common.Time;

public sealed class FakeUtcClock(DateTimeOffset utcNow) : IUtcClock
{
    public DateTimeOffset UtcNow { get; private set; } = EnsureUtc(utcNow);

    public void Set(DateTimeOffset utcNow) =>
        UtcNow = EnsureUtc(utcNow);

    public void Advance(TimeSpan duration) =>
        UtcNow = UtcNow.Add(duration);

    private static DateTimeOffset EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Clock time must be UTC.", nameof(value));
        }

        return value;
    }
}
