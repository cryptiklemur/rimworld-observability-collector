using Cryptiklemur.RimObs.Collector.Instrumentation;
using Cryptiklemur.RimObs.Collector.Tests.Stubs;
using Cryptiklemur.RimObs.Wire.Control;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Collector.Tests;

public sealed class ControlClientTests {
    [Fact]
    public async Task Search_proxies_and_returns_decoded_response() {
        using StubControlServer stub = new("topsecret");
        stub.OnSearch = _ => new ControlSearchResponse {
            Results = [
                new ControlMethodDescriptor {
                    TypeFullName = "Cryptiklemur.RimObs.Library.Tests.ResolverTargets",
                    MethodName = "Add",
                    Signature = "Int32 Add(Int32, Int32)",
                    ParamTypeFullNames = ["System.Int32", "System.Int32"],
                    AssemblyName = "RimObs.Library.Tests",
                },
            ],
        };
        stub.Start();
        ControlClient client = new(stub.Port, "topsecret");

        ControlSearchResponse res = await client.SearchAsync(new ControlSearchRequest { Query = "ResolverTargets", Limit = 5 });

        res.Results.Should().NotBeEmpty();
        res.Results.Should().Contain(r => r.MethodName == "Add");
    }

    [Fact]
    public async Task Search_throws_on_wrong_secret() {
        using StubControlServer stub = new("topsecret");
        stub.Start();
        ControlClient client = new(stub.Port, "wrong");

        Func<Task> act = () => client.SearchAsync(new ControlSearchRequest { Query = "ResolverTargets", Limit = 5 });

        ControlClientException ex = (await act.Should().ThrowAsync<ControlClientException>()).Which;
        ex.Status.Should().Be(401);
    }

    [Fact]
    public async Task Patch_proxies_and_returns_decoded_response() {
        using StubControlServer stub = new("topsecret");
        stub.OnPatch = _ => new ControlPatchResponse {
            PatchId = 42,
            SectionId = 7,
            SectionName = "MyMod.MyType.MyMethod",
            Status = "active",
        };
        stub.Start();
        ControlClient client = new(stub.Port, "topsecret");

        ControlPatchResponse res = await client.PatchAsync(new ControlPatchRequest {
            TypeFullName = "MyMod.MyType",
            MethodName = "MyMethod",
            ParamTypeFullNames = [],
        });

        res.PatchId.Should().Be(42);
        res.SectionName.Should().Be("MyMod.MyType.MyMethod");
        res.Status.Should().Be("active");
    }

    [Fact]
    public async Task List_proxies_and_returns_decoded_response() {
        using StubControlServer stub = new("topsecret");
        stub.OnList = () => new ControlPatchListResponse {
            Patches = [
                new ControlPatchEntry {
                    PatchId = 1,
                    Signature = "MyMod.MyType.MyMethod()",
                    SectionId = 7,
                    Status = "active",
                },
            ],
        };
        stub.Start();
        ControlClient client = new(stub.Port, "topsecret");

        ControlPatchListResponse res = await client.ListAsync();

        res.Patches.Should().HaveCount(1);
        res.Patches[0].PatchId.Should().Be(1);
        res.Patches[0].Signature.Should().Be("MyMod.MyType.MyMethod()");
    }

    [Fact]
    public async Task Unpatch_returns_false_when_stub_reports_not_found() {
        using StubControlServer stub = new("topsecret");
        stub.OnUnpatch = _ => false;
        stub.Start();
        ControlClient client = new(stub.Port, "topsecret");

        bool ok = await client.UnpatchAsync(123);

        ok.Should().BeFalse();
    }
}
