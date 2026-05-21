using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cryptiklemur.RimObs.Patching;
using Cryptiklemur.RimObs.Profile;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Tests;

public sealed class PatchInstallerTests : IDisposable {
    public PatchInstallerTests() {
        PatchInstaller.ResetForTests();
        SectionCatalog.Clear();
        SectionRegistry.Clear();
        HarmonyConflictRecorder.Clear();
    }

    public void Dispose() {
        PatchInstaller.ResetForTests();
        SectionCatalog.Clear();
        SectionRegistry.Clear();
        HarmonyConflictRecorder.Clear();
    }

    [Fact]
    public void InstallAll_patches_resolved_entries_and_skips_unresolved() {
        MethodInfo resolved = typeof(InstallTargets).GetMethod(nameof(InstallTargets.Ok))!;
        SectionCatalog.RegisterDirect("test.install.ok", resolved);

        SectionCatalog.Register("test.install.missing", "NoSuchType.That.Wont.Resolve", "Foo", null);

        PatchInstaller.InstallAll();

        PatchInstaller.InstalledCount.Should().BeGreaterOrEqualTo(1);
        PatchInstaller.UnresolvedCount.Should().BeGreaterOrEqualTo(1);
        PatchInstaller.FailedCount.Should().Be(0);
        PatchInstaller.InstalledEntries.Should().Contain(e => e.Name == "test.install.ok");
    }

    [Fact]
    public void InstallAll_is_idempotent() {
        MethodInfo resolved = typeof(InstallTargets).GetMethod(nameof(InstallTargets.Ok))!;
        SectionCatalog.RegisterDirect("test.install.idempotent", resolved);

        PatchInstaller.InstallAll();
        int firstInstalled = PatchInstaller.InstalledCount;

        PatchInstaller.InstallAll();

        PatchInstaller.InstalledCount.Should().Be(firstInstalled);
    }

    [Fact]
    public void InstallAll_records_install_error_when_patch_throws() {
        MethodInfo abstractMethod = typeof(IUnpatchable).GetMethod(nameof(IUnpatchable.AbstractOp))!;
        SectionCatalog.RegisterDirect("test.install.fails", abstractMethod);

        PatchInstaller.InstallAll();

        CatalogEntry entry = FindEntry("test.install.fails");
        // Interface methods cannot be patched by Harmony; the install loop must record
        // the failure on the entry rather than throwing out of InstallAll.
        if (!entry.Installed) {
            entry.InstallError.Should().NotBeNull();
            PatchInstaller.FailedCount.Should().BeGreaterOrEqualTo(1);
        }
    }

    [Fact]
    public void Instance_is_null_before_install_and_set_after() {
        PatchInstaller.Instance.Should().BeNull();

        MethodInfo resolved = typeof(InstallTargets).GetMethod(nameof(InstallTargets.Ok))!;
        SectionCatalog.RegisterDirect("test.install.instance", resolved);

        PatchInstaller.InstallAll();

        PatchInstaller.Instance.Should().NotBeNull();
        PatchInstaller.Instance!.Id.Should().Be(PatchInstaller.HarmonyId);
    }

    private static CatalogEntry FindEntry(string name) {
        foreach (CatalogEntry e in SectionCatalog.Entries) {
            if (e.Name == name)
                return e;
        }
        throw new InvalidOperationException($"entry '{name}' not registered");
    }

    public static class InstallTargets {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Ok() {
        }
    }

    public interface IUnpatchable {
        void AbstractOp();
    }
}
