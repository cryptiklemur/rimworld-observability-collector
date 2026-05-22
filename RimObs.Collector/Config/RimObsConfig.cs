using System.Text.Json.Serialization;

namespace Cryptiklemur.RimObs.Collector.Config;

public sealed class RimObsConfig {
    public const int Version = 1;

    public int SchemaVersion { get; set; } = Version;
    public CollectorOptions Collector { get; set; } = new();
    public SessionOptions Session { get; set; } = new();
    public StorageOptions Storage { get; set; } = new();
    public SamplingOptions Sampling { get; set; } = new();
    public CaptureOptions Capture { get; set; } = new();
    public TransportOptions Transport { get; set; } = new();
    public AttributionOptions Attribution { get; set; } = new();
    public PrivacyOptions Privacy { get; set; } = new();
    public SecurityOptions Security { get; set; } = new();
    public PanelOptions Panels { get; set; } = new();

    [JsonPropertyName("i18n")]
    public I18nOptions I18n { get; set; } = new();

    public ExporterOptions Exporters { get; set; } = new();
}
