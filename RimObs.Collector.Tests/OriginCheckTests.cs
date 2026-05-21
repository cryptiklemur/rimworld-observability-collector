using Cryptiklemur.RimObs.Collector.Security;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Collector.Tests;

public sealed class OriginCheckTests {
    [Theory]
    [InlineData("POST", true)]
    [InlineData("PUT", true)]
    [InlineData("PATCH", true)]
    [InlineData("DELETE", true)]
    [InlineData("post", true)]
    [InlineData("Patch", true)]
    [InlineData("GET", false)]
    [InlineData("HEAD", false)]
    [InlineData("OPTIONS", false)]
    [InlineData("", false)]
    public void RequiresCheck_returns_true_only_for_state_changing_methods(string method, bool expected) {
        OriginCheck.RequiresCheck(method).Should().Be(expected);
    }

    [Theory]
    [InlineData("http://127.0.0.1:17654", 17654, true)]
    [InlineData("http://localhost:17654", 17654, true)]
    [InlineData("HTTP://127.0.0.1:17654", 17654, true)]
    [InlineData("http://127.0.0.1:17654/", 17654, false)]
    [InlineData("http://127.0.0.1:17655", 17654, false)]
    [InlineData("https://127.0.0.1:17654", 17654, false)]
    [InlineData("http://evil.example.com", 17654, false)]
    [InlineData("", 17654, false)]
    [InlineData(null, 17654, false)]
    public void IsAllowedOrigin_accepts_only_loopback_with_matching_port(string? origin, int port, bool expected) {
        OriginCheck.IsAllowedOrigin(origin, port).Should().Be(expected);
    }
}
