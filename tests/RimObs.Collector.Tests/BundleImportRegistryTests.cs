using System;
using System.IO;
using Cryptiklemur.RimObs.Collector.Bundle;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Collector.Tests;

public class BundleImportRegistryTests : IDisposable {
    private readonly string _baseDir;

    public BundleImportRegistryTests() {
        _baseDir = Path.Combine(Path.GetTempPath(), $"rimobs-import-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_baseDir);
    }

    public void Dispose() {
        if (Directory.Exists(_baseDir))
            Directory.Delete(_baseDir, recursive: true);
    }

    [Fact]
    public void Register_ReturnsToken_WithDirectoryUnderBase() {
        BundleImportRegistry registry = new BundleImportRegistry(_baseDir, TimeSpan.FromMinutes(30));
        BundleImportEntry entry = registry.Register(new[] { "manifest.json" });

        entry.Token.Should().NotBeNullOrEmpty();
        entry.TempDir.Should().StartWith(_baseDir);
        Directory.Exists(entry.TempDir).Should().BeTrue();
    }

    [Fact]
    public void TryGet_ReturnsRegisteredEntry() {
        BundleImportRegistry registry = new BundleImportRegistry(_baseDir, TimeSpan.FromMinutes(30));
        BundleImportEntry entry = registry.Register(new[] { "manifest.json" });

        registry.TryGet(entry.Token, out BundleImportEntry? found).Should().BeTrue();
        found!.Token.Should().Be(entry.Token);
    }

    [Fact]
    public void Remove_DeletesTempDir() {
        BundleImportRegistry registry = new BundleImportRegistry(_baseDir, TimeSpan.FromMinutes(30));
        BundleImportEntry entry = registry.Register(new[] { "manifest.json" });
        File.WriteAllText(Path.Combine(entry.TempDir, "manifest.json"), "{}");

        registry.Remove(entry.Token).Should().BeTrue();
        Directory.Exists(entry.TempDir).Should().BeFalse();
        registry.TryGet(entry.Token, out _).Should().BeFalse();
    }

    [Fact]
    public void SweepIdle_RemovesEntriesPastTimeout() {
        BundleImportRegistry registry = new BundleImportRegistry(_baseDir, TimeSpan.FromMilliseconds(1));
        BundleImportEntry entry = registry.Register(new[] { "manifest.json" });
        System.Threading.Thread.Sleep(20);

        int removed = registry.SweepIdle();

        removed.Should().Be(1);
        Directory.Exists(entry.TempDir).Should().BeFalse();
    }

    [Fact]
    public void Touch_ResetsLastAccess() {
        BundleImportRegistry registry = new BundleImportRegistry(_baseDir, TimeSpan.FromMilliseconds(50));
        BundleImportEntry entry = registry.Register(new[] { "manifest.json" });
        System.Threading.Thread.Sleep(30);
        registry.Touch(entry.Token);
        System.Threading.Thread.Sleep(30);

        int removed = registry.SweepIdle();

        removed.Should().Be(0);
        Directory.Exists(entry.TempDir).Should().BeTrue();
    }

    [Fact]
    public void RemoveAll_WipesEverything() {
        BundleImportRegistry registry = new BundleImportRegistry(_baseDir, TimeSpan.FromMinutes(30));
        BundleImportEntry a = registry.Register(new[] { "manifest.json" });
        BundleImportEntry b = registry.Register(new[] { "manifest.json" });

        registry.RemoveAll();

        Directory.Exists(a.TempDir).Should().BeFalse();
        Directory.Exists(b.TempDir).Should().BeFalse();
        registry.TryGet(a.Token, out _).Should().BeFalse();
        registry.TryGet(b.Token, out _).Should().BeFalse();
    }
}
