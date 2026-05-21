namespace Cryptiklemur.RimObs.Profile;

internal interface ISampleSink {
    void RecordSection(int sectionId, long startTimestamp, long elapsedTicks);
}
