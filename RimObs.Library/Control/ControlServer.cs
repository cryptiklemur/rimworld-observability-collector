using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Cryptiklemur.RimObs.Library.Control;

internal sealed class ControlServer {
    private readonly string _secret;
    private readonly string _frameworkPackageId;
    private readonly HttpListener _listener;
    private Thread? _thread;
    private volatile bool _running;

    public ControlServer(string secret, string frameworkPackageId) {
        _secret = secret;
        _frameworkPackageId = frameworkPackageId;
        Port = PickFreeLoopbackPort();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
    }

    public int Port { get; }

    public void Start() {
        _listener.Start();
        _running = true;
        _thread = new Thread(Loop) { IsBackground = true, Name = "RimObs-Control" };
        _thread.Start();
    }

    public void Stop() {
        _running = false;
        _listener.Stop();
        _thread?.Join(System.TimeSpan.FromSeconds(2));
    }

    private void Loop() {
        while (_running) {
            HttpListenerContext ctx;
            try { ctx = _listener.GetContext(); }
            catch { return; }
            try { Handle(ctx); }
            catch { /* swallow per-request errors; never crash the listener */ }
            finally { ctx.Response.Close(); }
        }
    }

    private void Handle(HttpListenerContext ctx) {
        string? presented = ctx.Request.Headers["X-RimObs-Control"];
        if (!ConstantTimeEquals(presented, _secret)) {
            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }

        ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
    }

    private static bool ConstantTimeEquals(string? a, string b) {
        if (a is null) return false;
        if (a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }

    private static int PickFreeLoopbackPort() {
        TcpListener probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }
}
