using Cryptiklemur.RimObs.Collector.Security;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Collector.Tests;

public sealed class CollectorTokenTests
{
    [Fact]
    public void Generate_returns_distinct_tokens_on_repeat()
    {
        string a = CollectorToken.Generate();
        string b = CollectorToken.Generate();
        a.Should().NotBeNullOrEmpty();
        b.Should().NotBeNullOrEmpty();
        a.Should().NotBe(b);
    }

    [Fact]
    public void FromExplicitValue_rejects_empty()
    {
        Action act = () => CollectorToken.FromExplicitValue("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateFromEnvOrGenerate_uses_env_when_set()
    {
        using EnvVarScope _ = EnvVarScope.Set(CollectorToken.EnvVarName, "test-env-token");
        CollectorToken token = CollectorToken.CreateFromEnvOrGenerate();
        token.Value.Should().Be("test-env-token");
        token.FromEnv.Should().BeTrue();
    }

    [Fact]
    public void CreateFromEnvOrGenerate_generates_when_env_unset()
    {
        using EnvVarScope _ = EnvVarScope.Set(CollectorToken.EnvVarName, null);
        CollectorToken token = CollectorToken.CreateFromEnvOrGenerate();
        token.Value.Should().NotBeNullOrWhiteSpace();
        token.FromEnv.Should().BeFalse();
    }

    [Fact]
    public void CreateFromEnvOrGenerate_treats_whitespace_env_as_unset()
    {
        using EnvVarScope _ = EnvVarScope.Set(CollectorToken.EnvVarName, "   ");
        CollectorToken token = CollectorToken.CreateFromEnvOrGenerate();
        token.FromEnv.Should().BeFalse();
    }

    [Theory]
    [InlineData("abc", "abc", true)]
    [InlineData("abc", "abd", false)]
    [InlineData("abc", "", false)]
    [InlineData("abc", null, false)]
    [InlineData("abc", "abcd", false)]
    public void Matches_is_constant_time_equal(string stored, string? presented, bool expected)
    {
        CollectorToken token = CollectorToken.FromExplicitValue(stored);
        token.Matches(presented).Should().Be(expected);
    }

    private sealed class EnvVarScope : IDisposable
    {
        private readonly string _name;
        private readonly string? _previous;

        private EnvVarScope(string name)
        {
            _name = name;
            _previous = Environment.GetEnvironmentVariable(name);
        }

        public static EnvVarScope Set(string name, string? value)
        {
            EnvVarScope scope = new(name);
            Environment.SetEnvironmentVariable(name, value);
            return scope;
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_name, _previous);
        }
    }
}
