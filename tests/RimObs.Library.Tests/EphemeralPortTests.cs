using Cryptiklemur.RimObs.Transport;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Tests;

public sealed class EphemeralPortTests {
    [Fact]
    public void Allocate_returns_a_port_in_the_dynamic_range() {
        int port = EphemeralPort.Allocate();
        port.Should().BeGreaterThan(0);
        port.Should().BeLessThanOrEqualTo(65535);
    }

    [Fact]
    public void Allocate_can_be_called_repeatedly() {
        int first = EphemeralPort.Allocate();
        int second = EphemeralPort.Allocate();
        first.Should().BeGreaterThan(0);
        second.Should().BeGreaterThan(0);
    }
}
