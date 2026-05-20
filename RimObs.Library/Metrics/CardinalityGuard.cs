using System;
using System.Text;
using System.Threading;

namespace Cryptiklemur.RimObs.Metrics;

public static class CardinalityGuard
{
    public static MetricLabelEntry ResolveLabelEntry(MetricDescriptor descriptor, string canonicalLabel)
    {
        if (descriptor.LabeledEntries.TryGetValue(canonicalLabel, out MetricLabelEntry? existing))
            return existing;

        if (descriptor.LabeledEntries.Count >= descriptor.CardinalityLimit)
        {
            Interlocked.Increment(ref descriptor.CardinalityIncidentCount);
            return descriptor.LabeledEntries.GetOrAdd(MetricDescriptor.OverflowLabel, key => new MetricLabelEntry(key));
        }

        return descriptor.LabeledEntries.GetOrAdd(canonicalLabel, key => new MetricLabelEntry(key));
    }

    public static string Canonicalize(string key, string? value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Label key must not be empty.", nameof(key));
        return key + "=" + (value ?? string.Empty);
    }

    public static string Canonicalize(string k1, string? v1, string k2, string? v2)
    {
        StringBuilder sb = new(k1.Length + (v1?.Length ?? 0) + k2.Length + (v2?.Length ?? 0) + 3);
        AppendPair(sb, k1, v1);
        sb.Append(',');
        AppendPair(sb, k2, v2);
        return sb.ToString();
    }

    public static string Canonicalize(string k1, string? v1, string k2, string? v2, string k3, string? v3)
    {
        StringBuilder sb = new(k1.Length + (v1?.Length ?? 0) + k2.Length + (v2?.Length ?? 0) + k3.Length + (v3?.Length ?? 0) + 5);
        AppendPair(sb, k1, v1);
        sb.Append(',');
        AppendPair(sb, k2, v2);
        sb.Append(',');
        AppendPair(sb, k3, v3);
        return sb.ToString();
    }

    public static string Canonicalize(params (string Key, string Value)[] labels)
    {
        if (labels == null || labels.Length == 0)
            return string.Empty;

        int capacity = 0;
        for (int i = 0; i < labels.Length; i++)
        {
            capacity += labels[i].Key.Length + (labels[i].Value?.Length ?? 0) + 2;
        }

        StringBuilder sb = new(capacity);
        for (int i = 0; i < labels.Length; i++)
        {
            if (i > 0)
                sb.Append(',');
            AppendPair(sb, labels[i].Key, labels[i].Value);
        }
        return sb.ToString();
    }

    private static void AppendPair(StringBuilder sb, string key, string? value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Label key must not be empty.", nameof(key));
        sb.Append(key);
        sb.Append('=');
        if (!string.IsNullOrEmpty(value))
            sb.Append(value);
    }
}
