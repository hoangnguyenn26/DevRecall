using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;

namespace DevRecall.Application.Analytics;

public sealed class AnalyticsDateRangeResolver(IUtcClock utcClock)
{
    public const int DefaultRangeDays = 7;
    public const int MaximumRangeDays = 365;

    public AnalyticsDateRange Resolve(AnalyticsDateRangeInput request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var toUtc = request.ToUtc ?? utcClock.UtcNow;
        var fromUtc = request.FromUtc
            ?? toUtc.AddDays(-DefaultRangeDays);
        ValidateUtc(fromUtc, "fromUtc");
        ValidateUtc(toUtc, "toUtc");
        if (fromUtc >= toUtc)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["fromUtc"] = ["FromUtc must be earlier than ToUtc."]
                });
        }

        if (toUtc - fromUtc > TimeSpan.FromDays(MaximumRangeDays))
        {
            var message =
                $"The analytics range cannot exceed {MaximumRangeDays} days.";
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["fromUtc"] = [message],
                    ["toUtc"] = [message]
                });
        }

        return new AnalyticsDateRange(fromUtc, toUtc);
    }

    private static void ValidateUtc(
        DateTimeOffset value, string fieldName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    [fieldName] = ["Timestamp must use the UTC offset."]
                });
        }
    }
}
