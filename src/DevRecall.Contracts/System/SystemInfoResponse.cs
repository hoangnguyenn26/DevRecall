namespace DevRecall.Contracts.System;

public sealed record SystemInfoResponse(
    string ApplicationName,
    string Version,
    string Environment,
    DateTimeOffset CurrentTimeUtc);
