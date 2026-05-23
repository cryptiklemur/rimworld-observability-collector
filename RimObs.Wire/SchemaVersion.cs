namespace Cryptiklemur.RimObs.Wire;

public static class SchemaVersion {
    // The wire/API envelope version. Every collector HTTP response carries this in its
    // schema_version field. Domain bodies (config, panel registration) version their own
    // payloads independently and are validated against their own constants on ingest.
    public const int Current = 1;
}
