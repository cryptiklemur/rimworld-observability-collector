using Cryptiklemur.RimObs.Collector.Aggregation;
using Cryptiklemur.RimObs.Wire;

namespace Cryptiklemur.RimObs.Collector.Storage;

public interface ISessionPersister : IDisposable {
    void WriteSessionMeta(SessionMeta meta);

    void WriteSectionsSnapshot(string sessionId, IReadOnlyCollection<SectionStats> sections);

    void WriteMetricsSnapshot(string sessionId, IReadOnlyCollection<MetricStats> metrics);

    void ReplaceGcEventsSnapshot(string sessionId, GcEventRecord[] events);
}
