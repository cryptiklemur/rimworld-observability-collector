namespace Cryptiklemur.RimObs.Wire;

public enum BatchType : byte
{
    Sections = 0,
    Counters = 1,
    Gauges = 2,
    Histograms = 3,
    GcEvents = 4,
    Allocations = 5,
    CallTree = 6,
    SessionMeta = 7,
    SectionRegistrations = 8,
    Ping = 255,
}
