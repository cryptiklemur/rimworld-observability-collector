using System;
using System.Reflection;
using Cryptiklemur.RimObs.Patching;
using Cryptiklemur.RimObs.Profile;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Tests;

public sealed class SectionCatalogTests {
    [Fact]
    public void RegisterDirect_AcceptsSubsystemAndSetsOnEntryAndRegistry() {
        SectionCatalog.Clear();
        SectionRegistry.Clear();
        MethodBase target = typeof(SectionCatalogTests).GetMethod(
            nameof(RegisterDirect_AcceptsSubsystemAndSetsOnEntryAndRegistry))!;

        CatalogEntry entry = SectionCatalog.RegisterDirect("test.direct", target, subsystem: "ui");

        entry.Subsystem.Should().Be("ui");
        SectionRegistry.GetSubsystem(entry.SectionId).Should().Be("ui");
    }
}
