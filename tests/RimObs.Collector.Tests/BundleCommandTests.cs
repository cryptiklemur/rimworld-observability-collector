using System.IO;
using Cryptiklemur.RimObs.Collector.Cli;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Collector.Tests;

public class BundleCommandTests {
    [Fact]
    public void ParseArgs_ExtractsExportOptions() {
        BundleCommandOptions? opts = BundleCommand.TryParseExportArgs(new[] {
            "export", "sess-123", "--output", "./out.zip",
            "--include", "allocations", "--include", "gc-events", "--force",
        }, out string? error);

        opts.Should().NotBeNull();
        error.Should().BeNull();
        opts!.SessionId.Should().Be("sess-123");
        opts.OutputPath.Should().Be("./out.zip");
        opts.Includes.Should().BeEquivalentTo(new[] { "allocations", "gc-events" });
        opts.Force.Should().BeTrue();
    }

    [Fact]
    public void ParseArgs_RejectsMissingSessionId() {
        BundleCommandOptions? opts = BundleCommand.TryParseExportArgs(new[] { "export", "--output", "x.zip" }, out string? error);
        opts.Should().BeNull();
        error.Should().Contain("session_id");
    }

    [Fact]
    public void ParseArgs_RejectsMissingOutput() {
        BundleCommandOptions? opts = BundleCommand.TryParseExportArgs(new[] { "export", "sess" }, out string? error);
        opts.Should().BeNull();
        error.Should().Contain("--output");
    }

    [Fact]
    public void RunExport_PrintsErrorWhenNoActiveSession() {
        StringWriter stdout = new StringWriter();
        StringWriter stderr = new StringWriter();
        int code = BundleCommand.RunExport(new[] { "export", "sess-x", "--output", "/tmp/x.zip" }, stdout, stderr);

        code.Should().NotBe(0);
        stderr.ToString().Should().NotBeEmpty();
    }
}
