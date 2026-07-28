namespace DevRecall.Application.Common.Time;

public interface IUtcClock
{
    DateTimeOffset UtcNow { get; }
}
