namespace Cryptiklemur.RimObs.Observers;

public interface IGcEventSink
{
    void RecordGcEvent(in GcEventSample sample);
}
