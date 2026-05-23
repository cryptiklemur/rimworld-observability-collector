using System.Text.Json;
using System.Threading.Tasks;
using Cryptiklemur.RimObs.Collector.Config;
using Cryptiklemur.RimObs.Wire;
using Microsoft.AspNetCore.Http;

namespace Cryptiklemur.RimObs.Collector.Api;

public static class RequestBody {
    // Response envelopes always carry the wire SchemaVersion.Current. The domain version
    // (config/panel registration schema) is only used to validate the inbound body.
    public static async Task<(T? Body, IResult? Error)> ReadValidated<T>(
        HttpContext context,
        int domainVersion,
        Func<T, int> getSchemaVersion,
        string entity)
        where T : class {
        T? incoming;
        try {
            incoming = await context.Request.ReadFromJsonAsync<T>(ConfigJson.Options);
        }
        catch (JsonException) {
            return (null, Results.BadRequest(new { schema_version = SchemaVersion.Current, reason = $"malformed {entity} body" }));
        }

        if (incoming is null) {
            return (null, Results.BadRequest(new { schema_version = SchemaVersion.Current, reason = $"empty {entity} body" }));
        }

        int incomingVersion = getSchemaVersion(incoming);
        if (incomingVersion != domainVersion) {
            return (null, Results.BadRequest(new { schema_version = SchemaVersion.Current, reason = $"unsupported schema_version {incomingVersion}" }));
        }

        return (incoming, null);
    }
}
