using System.Runtime.Serialization;

namespace Cryptiklemur.RimObs.Config;

[DataContract]
internal sealed class CollectorSectionsConfig {
    [DataMember(Name = "disabled")]
    public string[]? Disabled { get; set; }
}
