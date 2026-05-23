using System.Net;
using Cryptiklemur.RimObs.Collector.Instrumentation;
using Cryptiklemur.RimObs.Wire;
using Cryptiklemur.RimObs.Wire.Control;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Collector.Tests;

// Uses a minimal in-file HttpListener stub that mirrors ControlServer.Handle's
// secret check and /search round trip. Task 16 will introduce the reusable
// StubControlServer; this is a one-off confined to the Task 14 happy path.
public sealed class ControlClientTests {
    [Fact]
    public async Task Search_proxies_and_returns_decoded_response() {
        using StubServer stub = new("topsecret");
        ControlClient client = new(stub.Port, "topsecret");

        ControlSearchResponse res = await client.SearchAsync(new ControlSearchRequest { Query = "ResolverTargets", Limit = 5 });

        res.Results.Should().NotBeEmpty();
        res.Results.Should().Contain(r => r.MethodName == "Add");
    }

    [Fact]
    public async Task Search_throws_on_wrong_secret() {
        using StubServer stub = new("topsecret");
        ControlClient client = new(stub.Port, "wrong");

        Func<Task> act = () => client.SearchAsync(new ControlSearchRequest { Query = "ResolverTargets", Limit = 5 });

        ControlClientException ex = (await act.Should().ThrowAsync<ControlClientException>()).Which;
        ex.Status.Should().Be(401);
    }

    private sealed class StubServer : IDisposable {
        private readonly HttpListener _listener;
        private readonly string _secret;
        private readonly Task _loop;

        public StubServer(string secret) {
            _secret = secret;
            Port = PickFreeLoopbackPort();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();
            _loop = Task.Run(Loop);
        }

        public int Port { get; }

        private async Task Loop() {
            while (_listener.IsListening) {
                HttpListenerContext ctx;
                try {
                    ctx = await _listener.GetContextAsync();
                }
                catch (HttpListenerException) {
                    return;
                }
                catch (ObjectDisposedException) {
                    return;
                }

                Handle(ctx);
            }
        }

        private void Handle(HttpListenerContext ctx) {
            string? presented = ctx.Request.Headers["X-RimObs-Control"];
            if (presented != _secret) {
                ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                ctx.Response.Close();
                return;
            }

            string path = ctx.Request.Url?.AbsolutePath ?? "/";
            if (ctx.Request.HttpMethod == "POST" && path == "/search") {
                using MemoryStream buffer = new();
                ctx.Request.InputStream.CopyTo(buffer);
                WireCodec.Deserialize<ControlSearchRequest>(buffer.ToArray());

                ControlSearchResponse res = new() {
                    Results = [
                        new ControlMethodDescriptor {
                            TypeFullName = "Cryptiklemur.RimObs.Library.Tests.ResolverTargets",
                            MethodName = "Add",
                            Signature = "Int32 Add(Int32, Int32)",
                            ParamTypeFullNames = ["System.Int32", "System.Int32"],
                            AssemblyName = "RimObs.Library.Tests",
                        },
                    ],
                };
                byte[] body = WireCodec.Serialize(res);
                ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                ctx.Response.ContentType = "application/x-msgpack";
                ctx.Response.ContentLength64 = body.Length;
                ctx.Response.OutputStream.Write(body, 0, body.Length);
                ctx.Response.Close();
                return;
            }

            ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
            ctx.Response.Close();
        }

        public void Dispose() {
            _listener.Stop();
            _listener.Close();
            try {
                _loop.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException) {
                // Loop torn down by listener stop.
            }
        }

        private static int PickFreeLoopbackPort() {
            System.Net.Sockets.TcpListener probe = new(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }
    }
}
