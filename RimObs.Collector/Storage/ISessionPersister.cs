using Cryptiklemur.RimObs.Wire;

namespace Cryptiklemur.RimObs.Collector.Storage;

public interface ISessionPersister : IDisposable
{
    void WriteSessionMeta(SessionMeta meta);
}
