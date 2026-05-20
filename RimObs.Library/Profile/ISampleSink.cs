namespace Cryptiklemur.RimObs.Profile;

public interface ISampleSink
{
    void RecordSection(int sectionId, long startTimestamp, long elapsedTicks);
}
