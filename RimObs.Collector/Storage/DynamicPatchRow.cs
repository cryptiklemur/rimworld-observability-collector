using Cryptiklemur.RimObs.Wire.Control;

namespace Cryptiklemur.RimObs.Collector.Storage;

public sealed record DynamicPatchRow(
    long Id,
    string TypeFullName,
    string MethodName,
    string ParamTypesJoined,
    string CreatedUtc,
    PatchStatus LastStatus,
    string? LastError);
