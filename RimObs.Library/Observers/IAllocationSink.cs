namespace Cryptiklemur.RimObs.Observers;

internal interface IAllocationSink {
    void RecordAllocation(in AllocationSample sample);
}
