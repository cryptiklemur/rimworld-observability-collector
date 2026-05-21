namespace Cryptiklemur.RimObs.Observers;

internal interface IGcEventSink {
    void RecordGcEvent(in GcEventSample sample);
}
