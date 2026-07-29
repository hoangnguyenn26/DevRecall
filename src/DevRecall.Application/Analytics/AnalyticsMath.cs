namespace DevRecall.Application.Analytics;

internal static class AnalyticsMath
{
    public static decimal Percentage(int numerator, int denominator) =>
        denominator <= 0
            ? 0m
            : Math.Round(
                numerator / (decimal)denominator * 100m,
                2, MidpointRounding.AwayFromZero);

    public static decimal RoundAverage(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
