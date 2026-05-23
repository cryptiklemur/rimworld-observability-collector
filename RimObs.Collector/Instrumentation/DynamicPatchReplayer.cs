using System.Threading.Tasks;
using Cryptiklemur.RimObs.Collector.Storage;
using Cryptiklemur.RimObs.Wire.Control;

namespace Cryptiklemur.RimObs.Collector.Instrumentation;

public sealed class DynamicPatchReplayer {
    private readonly DynamicPatchStore _store;

    public DynamicPatchReplayer(DynamicPatchStore store) {
        _store = store;
    }

    public async Task ReplayAsync(ControlClient client) {
        foreach (DynamicPatchRow row in _store.List()) {
            string[] paramTypes = row.ParamTypesJoined.Length == 0
                ? []
                : row.ParamTypesJoined.Split(';');

            try {
                ControlPatchResponse res = await client.PatchAsync(new ControlPatchRequest {
                    TypeFullName = row.TypeFullName,
                    MethodName = row.MethodName,
                    ParamTypeFullNames = paramTypes,
                });
                if (res.Status == "active")
                    _store.UpdateStatus(row.Id, "active", null);
                else
                    _store.UpdateStatus(row.Id, "stale", res.ErrorReason);
            }
            catch (System.Exception ex) {
                _store.UpdateStatus(row.Id, "stale", ex.Message);
            }
        }
    }
}
