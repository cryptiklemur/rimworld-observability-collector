using System;
using System.Net;
using System.Net.Http;
using Cryptiklemur.RimObs.Library.Control;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Library.Tests.Control;

public class ControlServerTests : IDisposable {
    private readonly ControlServer _server;
    private readonly HttpClient _client;

    public ControlServerTests() {
        _server = new ControlServer(secret: "topsecret", frameworkPackageId: "test.pkg");
        _server.Start();
        _client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{_server.Port}/") };
    }

    public void Dispose() {
        _client.Dispose();
        _server.Stop();
    }

    [Fact]
    public async Task Rejects_request_without_secret_header() {
        HttpResponseMessage res = await _client.PostAsync("/search",
            new ByteArrayContent([0]));
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Rejects_request_with_wrong_secret() {
        HttpRequestMessage req = new(HttpMethod.Post, "/search") {
            Content = new ByteArrayContent([0]),
        };
        req.Headers.Add("X-RimObs-Control", "wrong");
        HttpResponseMessage res = await _client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_404_for_unknown_path_with_valid_secret() {
        HttpRequestMessage req = new(HttpMethod.Get, "/nope");
        req.Headers.Add("X-RimObs-Control", "topsecret");
        HttpResponseMessage res = await _client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
