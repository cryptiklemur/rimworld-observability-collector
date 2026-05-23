using Serilog.Events;

namespace Cryptiklemur.RimObs.Collector.Logging;

public sealed record LogEntry(DateTimeOffset Timestamp, LogEventLevel Level, string Message, string? Exception);
