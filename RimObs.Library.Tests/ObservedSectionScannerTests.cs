using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cryptiklemur.RimObs.Api;
using Cryptiklemur.RimObs.Patching;
using Cryptiklemur.RimObs.Profile;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Tests;

public class ObservedSectionScannerTests : System.IDisposable {
    public ObservedSectionScannerTests() {
        PatchInstaller.ResetForTests();
        SectionCatalog.Clear();
        SectionRegistry.Clear();
        OwnerRegistry.Clear();
        OwnerRegistry.RegisterMod(typeof(ObservedSectionScannerTests).Assembly, "test.modid");
    }

    public void Dispose() {
        PatchInstaller.ResetForTests();
        SectionCatalog.Clear();
        SectionRegistry.Clear();
        OwnerRegistry.Clear();
    }

    public class Target_BareAttribute {
        [ObservedSection]
        public void Tick() { }
    }

    public class Target_NamedAttribute {
        [ObservedSection("custom_name")]
        public void Run() { }
    }

    public class Target_Subsystem {
        [ObservedSection("with_sub", Subsystem = "jobs")]
        public void Run() { }
    }

    [Fact]
    public void Scan_BareAttribute_RegistersWithFullTypeAndMethodName() {
        IReadOnlyList<Assembly> asms = [typeof(Target_BareAttribute).Assembly];
        ObservedSectionScanner.ScanResult result = ObservedSectionScanner.Scan(
            [("test.modid", asms)]);

        result.AttributesFound.Should().BeGreaterOrEqualTo(1);
        result.Registered.Should().BeGreaterOrEqualTo(1);

        string expected = $"test.modid.{typeof(Target_BareAttribute).FullName}.Tick";
        SectionCatalog.Entries.Should().Contain(e => e.Name == expected);
    }

    [Fact]
    public void Scan_NamedAttribute_UsesProvidedName() {
        IReadOnlyList<Assembly> asms = [typeof(Target_NamedAttribute).Assembly];
        ObservedSectionScanner.Scan([("test.modid", asms)]);

        SectionCatalog.Entries.Should().Contain(e => e.Name == "test.modid.custom_name");
    }

    [Fact]
    public void Scan_SubsystemAttribute_PropagatesToEntryAndRegistry() {
        IReadOnlyList<Assembly> asms = [typeof(Target_Subsystem).Assembly];
        ObservedSectionScanner.Scan([("test.modid", asms)]);

        CatalogEntry entry = SectionCatalog.Entries.First(e => e.Name == "test.modid.with_sub");
        entry.Subsystem.Should().Be("jobs");
        SectionRegistry.GetSubsystem(entry.SectionId).Should().Be("jobs");
    }

    public abstract class Target_Abstract {
        [ObservedSection]
        public abstract void Tick();
    }

    public class Target_GenericOpen<T> {
        [ObservedSection]
        public void Tick(T item) { }
    }

    public class Target_Async {
        [ObservedSection]
        public async System.Threading.Tasks.Task DoAsync() { await System.Threading.Tasks.Task.Yield(); }
    }

    public class Target_Iterator {
        [ObservedSection]
        public System.Collections.Generic.IEnumerable<int> Yield() { yield return 1; }
    }

    [Theory]
    [InlineData(typeof(Target_Abstract), nameof(Target_Abstract.Tick))]
    [InlineData(typeof(Target_GenericOpen<>), "Tick")]
    [InlineData(typeof(Target_Async), nameof(Target_Async.DoAsync))]
    [InlineData(typeof(Target_Iterator), nameof(Target_Iterator.Yield))]
    public void Scan_UnsupportedMethod_Skipped(System.Type type, string methodName) {
        IReadOnlyList<Assembly> asms = new[] { type.Assembly };
        ObservedSectionScanner.ScanResult result = ObservedSectionScanner.Scan(
            new[] { ("test.modid", asms) });

        result.SkippedUnsupported.Should().BeGreaterOrEqualTo(1);
        string typeName = type.FullName!;
        SectionCatalog.Entries.Should().NotContain(
            e => e.TypeName == typeName && e.MethodName == methodName);
    }

    public class Target_Dedup {
        [ObservedSection("dup_name")]
        public void TargetMethod() { }
    }

    [Fact]
    public void Scan_DuplicateAgainstExistingCatalog_Skipped() {
        MethodBase target = typeof(Target_Dedup).GetMethod(nameof(Target_Dedup.TargetMethod))!;
        SectionCatalog.RegisterDirect("test.modid.core_owned", target);

        IReadOnlyList<Assembly> asms = new[] { typeof(Target_Dedup).Assembly };
        ObservedSectionScanner.ScanResult result = ObservedSectionScanner.Scan(
            new[] { ("test.modid", asms) });

        result.SkippedDuplicate.Should().BeGreaterOrEqualTo(1);
        SectionCatalog.Entries
            .Where(e => ReferenceEquals(e.Resolved, target))
            .Should().HaveCount(1, "scanner must not register a second entry for the same MethodBase");
    }

    public static class Target_PatchedAtScan {
        [ObservedSection("patched_by_scan")]
        public static int Echo(int x) => x;
    }

    [Fact]
    public void Scan_RegistersAndPatches() {
        int beforeInstalled = PatchInstaller.InstalledCount;
        IReadOnlyList<Assembly> asms = new[] { typeof(Target_PatchedAtScan).Assembly };
        ObservedSectionScanner.Scan(new[] { ("test.modid", asms) });

        PatchInstaller.InstalledCount.Should().BeGreaterOrEqualTo(beforeInstalled + 1);
        CatalogEntry entry = SectionCatalog.Entries.First(e => e.Name == "test.modid.patched_by_scan");
        entry.Installed.Should().BeTrue();
    }
}
