namespace Cryptiklemur.RimObs.Wire;

public enum BatchType : byte {
    Sections = 0,
    Metrics = 1,
    MetricRegistrations = 2,
    GcEvents = 4,
    Allocations = 5,
    SessionMeta = 7,
    SectionRegistrations = 8,
    Pong = 254,
    Ping = 255,
}
