using System.Collections.Generic;
using System.Threading;
using Cryptiklemur.RimObs.Metrics;

namespace Cryptiklemur.RimObs.Api;

public static class Diagnostics
{
    private static long s_SamplesDroppedExternal;

    public static long SamplesDroppedExternal => Interlocked.Read(ref s_SamplesDroppedExternal);

    public static void IncrementSamplesDropped(long count = 1)
    {
        Interlocked.Add(ref s_SamplesDroppedExternal, count);
    }

    public static long CardinalityIncidentsTotal
    {
        get
        {
            long total = 0;
            int count = MetricRegistry.Count;
            for (int i = 0; i < count; i++)
            {
                MetricDescriptor? descriptor = MetricRegistry.Get(i);
                if (descriptor == null)
                    continue;
                total += Interlocked.Read(ref descriptor.CardinalityIncidentCount);
            }
            return total;
        }
    }

    public static IEnumerable<MetricCardinalityIncident> MetricsWithIncidents()
    {
        int count = MetricRegistry.Count;
        for (int i = 0; i < count; i++)
        {
            MetricDescriptor? descriptor = MetricRegistry.Get(i);
            if (descriptor == null)
                continue;
            long incidents = Interlocked.Read(ref descriptor.CardinalityIncidentCount);
            if (incidents > 0)
                yield return new MetricCardinalityIncident(
                    descriptor.FullName,
                    descriptor.OwnerPackageId,
                    descriptor.CardinalityLimit,
                    incidents
                );
        }
    }

    public static void Reset()
    {
        Interlocked.Exchange(ref s_SamplesDroppedExternal, 0);
        int count = MetricRegistry.Count;
        for (int i = 0; i < count; i++)
        {
            MetricDescriptor? descriptor = MetricRegistry.Get(i);
            if (descriptor == null)
                continue;
            Interlocked.Exchange(ref descriptor.CardinalityIncidentCount, 0);
        }
    }
}

public readonly struct MetricCardinalityIncident
{
    public MetricCardinalityIncident(string metricName, string ownerPackageId, int cardinalityLimit, long incidentCount)
    {
        MetricName = metricName;
        OwnerPackageId = ownerPackageId;
        CardinalityLimit = cardinalityLimit;
        IncidentCount = incidentCount;
    }

    public string MetricName { get; }
    public string OwnerPackageId { get; }
    public int CardinalityLimit { get; }
    public long IncidentCount { get; }
}
