namespace Cryptiklemur.RimObs.Collector.Logging;

public sealed record LogEntry(DateTimeOffset Timestamp, string Level, string Message, string? Exception);
