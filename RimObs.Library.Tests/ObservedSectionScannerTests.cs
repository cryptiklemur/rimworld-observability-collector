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
        SectionCatalog.Clear();
        SectionRegistry.Clear();
        OwnerRegistry.Clear();
        OwnerRegistry.RegisterMod(typeof(ObservedSectionScannerTests).Assembly, "test.modid");
    }

    public void Dispose() {
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
}
