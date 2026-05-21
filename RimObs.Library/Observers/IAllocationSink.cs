namespace Cryptiklemur.RimObs.Observers;

public interface IAllocationSink
{
    void RecordAllocation(in AllocationSample sample);
}
