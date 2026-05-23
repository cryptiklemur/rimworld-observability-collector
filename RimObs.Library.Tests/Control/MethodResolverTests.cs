using Cryptiklemur.RimObs.Library.Control;
using Xunit;
using Cryptiklemur.RimObs.Library.Tests.Control._Fixtures;
using FluentAssertions;

namespace Cryptiklemur.RimObs.Library.Tests.Control;

public class MethodResolverTests {
    [Fact]
    public void Resolves_exact_signature() {
        MethodResolveResult result = MethodResolver.Resolve(
            typeof(ResolverTargets).FullName!, "Add",
            [typeof(int).FullName!, typeof(int).FullName!]);

        result.Refused.Should().BeFalse();
        result.Method!.GetParameters().Should().HaveCount(2);
    }

    [Fact]
    public void Refuses_ambiguous_overload_when_param_types_match_multiple() {
        MethodResolveResult result = MethodResolver.Resolve(
            typeof(ResolverTargets).FullName!, "Add", []);

        result.Refused.Should().BeTrue();
        result.Reason.Should().Contain("ambiguous");
    }

    [Fact]
    public void Refuses_unknown_type() {
        MethodResolveResult result = MethodResolver.Resolve(
            "Nope.Does.Not.Exist", "Anything", []);
        result.Refused.Should().BeTrue();
        result.Reason.Should().Contain("type");
    }

    [Fact]
    public void Refuses_open_generic_method() {
        MethodResolveResult result = MethodResolver.Resolve(
            typeof(ResolverTargets).FullName!, "Identity",
            [typeof(object).FullName!]);
        result.Refused.Should().BeTrue();
        result.Reason.Should().Contain("generic");
    }

    [Fact]
    public void Refuses_abstract_method() {
        MethodResolveResult result = MethodResolver.Resolve(
            typeof(ResolverTargets.Inner).FullName!, "Abstract", []);
        result.Refused.Should().BeTrue();
        result.Reason.Should().Contain("abstract");
    }

    [Fact]
    public void Refuses_self_patch_in_library_namespace() {
        MethodResolveResult result = MethodResolver.Resolve(
            "Cryptiklemur.RimObs.Library.Profile.Profiler", "Start",
            [typeof(int).FullName!]);
        result.Refused.Should().BeTrue();
        result.Reason.Should().Contain("blocklist");
    }
}
