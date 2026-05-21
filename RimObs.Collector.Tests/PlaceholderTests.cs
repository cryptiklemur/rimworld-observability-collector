using Cryptiklemur.RimObs.Wire;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Collector.Tests;

public sealed class PlaceholderTests
{
    [Fact]
    public void BuildInfo_revision_is_set()
    {
        BuildInfo.Revision.Should().NotBeNullOrEmpty();
    }
}
