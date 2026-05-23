namespace Cryptiklemur.RimObs.Observers;

internal interface ITpsFpsSink {
    void RecordTpsFps(in TpsFpsSample sample);
}
