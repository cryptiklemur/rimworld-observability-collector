using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cryptiklemur.RimObs.Patching;
using Cryptiklemur.RimObs.Profile;
using FluentAssertions;
using HarmonyLib;
using Xunit;

namespace Cryptiklemur.RimObs.Tests;

public sealed class MethodTransplanterTests : IDisposable
{
    private readonly RecordingSink _sink;
    private readonly Harmony _harmony;
    private readonly List<MethodBase> _patched = new();

    public MethodTransplanterTests()
    {
        _sink = new RecordingSink();
        Profiler.SetSink(_sink);
        Profiler.Enabled = true;
        _harmony = new Harmony($"cryptiklemur.rimobs.tests.{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        foreach (MethodBase m in _patched)
        {
            try
            {
                _harmony.Unpatch(m, HarmonyPatchType.All, _harmony.Id);
            }
            catch
            {
            }
        }
        Profiler.SetSink(null);
    }

    private void Patch(MethodInfo target, string sectionName)
    {
        SectionCatalog.RegisterDirect(sectionName, target);
        HarmonyMethod transpiler = new(MethodTransplanter.TranspilerMethod);
        _harmony.Patch(target, transpiler: transpiler);
        _patched.Add(target);
    }

    [Fact]
    public void Patches_void_method_and_records_one_sample()
    {
        Patch(typeof(Targets).GetMethod(nameof(Targets.VoidNoOp))!, "test.void_noop");

        Targets.VoidNoOp();

        _sink.Samples.Should().HaveCount(1);
        _sink.Samples[0].SectionId.Should().BeGreaterOrEqualTo(0);
        _sink.Samples[0].ElapsedTicks.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Patches_returning_method_and_preserves_return_value()
    {
        Patch(typeof(Targets).GetMethod(nameof(Targets.Add))!, "test.add");

        int result = Targets.Add(7, 35);

        result.Should().Be(42);
        _sink.Samples.Should().HaveCount(1);
    }

    [Fact]
    public void Records_sample_even_when_method_throws()
    {
        Patch(typeof(Targets).GetMethod(nameof(Targets.ThrowsAlways))!, "test.throws");

        Action act = () => Targets.ThrowsAlways();

        act.Should().Throw<InvalidOperationException>().WithMessage("nope");
        _sink.Samples.Should().HaveCount(1);
    }

    [Fact]
    public void Inactive_section_does_not_record_sample()
    {
        MethodInfo target = typeof(Targets).GetMethod(nameof(Targets.ReturnsConst))!;
        CatalogEntry entry = SectionCatalog.RegisterDirect("test.const_inactive", target);
        SectionRegistry.SetActive(entry.SectionId, false);

        HarmonyMethod transpiler = new(MethodTransplanter.TranspilerMethod);
        _harmony.Patch(target, transpiler: transpiler);
        _patched.Add(target);

        int result = Targets.ReturnsConst();

        result.Should().Be(99);
        _sink.Samples.Should().BeEmpty();
    }

    [Fact]
    public void Branchy_method_with_multiple_returns_still_records_once()
    {
        Patch(typeof(Targets).GetMethod(nameof(Targets.MultipleReturns))!, "test.multi_returns");

        Targets.MultipleReturns(0).Should().Be("zero");
        Targets.MultipleReturns(1).Should().Be("one");
        Targets.MultipleReturns(2).Should().Be("two");
        Targets.MultipleReturns(99).Should().Be("other");

        _sink.Samples.Should().HaveCount(4);
    }

    public static class Targets
    {
        public static int Counter;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void VoidNoOp()
        {
            Counter++;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Add(int a, int b)
        {
            return a + b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ReturnsConst()
        {
            return 99;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowsAlways()
        {
            throw new InvalidOperationException("nope");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string MultipleReturns(int x)
        {
            if (x == 0)
                return "zero";
            if (x == 1)
                return "one";
            if (x == 2)
                return "two";
            return "other";
        }
    }

    private sealed class RecordingSink : ISampleSink
    {
        public readonly List<Sample> Samples = new();
        private readonly object _lock = new();

        public void RecordSection(int sectionId, long startTimestamp, long elapsedTicks)
        {
            lock (_lock)
            {
                Samples.Add(new Sample(sectionId, startTimestamp, elapsedTicks));
            }
        }
    }

    private readonly record struct Sample(int SectionId, long StartTimestamp, long ElapsedTicks);
}
