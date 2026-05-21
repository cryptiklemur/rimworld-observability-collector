namespace Cryptiklemur.RimObs.Wire;

public enum BatchType : byte
{
    Sections = 0,
    GcEvents = 4,
    Allocations = 5,
    SessionMeta = 7,
    SectionRegistrations = 8,
    Ping = 255,
}
