using DevRecall.Domain.Common.Errors;

namespace DevRecall.Application.Analytics;

public static class AnalyticsErrors
{
    public static readonly DomainError InvalidDateRange = new(
        "ANALYTICS_INVALID_DATE_RANGE",
        "The analytics date range is invalid.");
    public static readonly DomainError DateRangeTooLarge = new(
        "ANALYTICS_DATE_RANGE_TOO_LARGE",
        "The analytics date range cannot exceed 365 days.");
    public static readonly DomainError DateRangeMustBeUtc = new(
        "ANALYTICS_DATE_RANGE_MUST_BE_UTC",
        "Analytics date-range timestamps must use UTC.");
}
