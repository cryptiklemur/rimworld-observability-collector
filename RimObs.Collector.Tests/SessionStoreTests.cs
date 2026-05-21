using Cryptiklemur.RimObs.Collector.Storage;
using Cryptiklemur.RimObs.Wire;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Cryptiklemur.RimObs.Collector.Tests;

public sealed class SessionStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dbPath;

    public SessionStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "rimobs-store-" + Guid.NewGuid().ToString("N"));
        _dbPath = Path.Combine(_tempDir, "session.db");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch (IOException)
            {
                // SQLite may briefly retain file handles via the connection pool on Windows.
            }
        }
    }

    [Fact]
    public void Open_creates_directory_and_initializes_schema()
    {
        using SessionStore store = SessionStore.Open(_dbPath);

        Directory.Exists(_tempDir).Should().BeTrue();
        File.Exists(_dbPath).Should().BeTrue();

        using SqliteConnection probe = new($"Data Source={_dbPath}");
        probe.Open();
        using SqliteCommand cmd = probe.CreateCommand();
        cmd.CommandText = "PRAGMA user_version;";
        object? result = cmd.ExecuteScalar();
        Convert.ToInt32(result).Should().Be(SessionStore.SchemaVersion);
    }

    [Fact]
    public void Open_rejects_empty_path()
    {
        Action act = () => SessionStore.Open("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WriteSessionMeta_then_Read_returns_same_values()
    {
        SessionMeta meta = new()
        {
            SessionId = "abc-123",
            StartedUtcTicks = 638_500_000_000_000_000L,
            StopwatchFrequency = 10_000_000L,
            AnchorTimestamp = 42L,
            LibraryVersion = "0.1.0",
            GameVersion = "1.6.4633",
        };

        using (SessionStore store = SessionStore.Open(_dbPath))
        {
            store.WriteSessionMeta(meta);
        }

        using SessionStore reopened = SessionStore.Open(_dbPath);
        SessionMeta? round = reopened.ReadSessionMeta("abc-123");
        round.Should().NotBeNull();
        round!.SessionId.Should().Be(meta.SessionId);
        round.StartedUtcTicks.Should().Be(meta.StartedUtcTicks);
        round.StopwatchFrequency.Should().Be(meta.StopwatchFrequency);
        round.AnchorTimestamp.Should().Be(meta.AnchorTimestamp);
        round.LibraryVersion.Should().Be(meta.LibraryVersion);
        round.GameVersion.Should().Be(meta.GameVersion);
    }

    [Fact]
    public void WriteSessionMeta_is_idempotent_on_same_session_id()
    {
        SessionMeta first = new() { SessionId = "s1", StartedUtcTicks = 1, StopwatchFrequency = 2, AnchorTimestamp = 3, LibraryVersion = "old", GameVersion = "1.6" };
        SessionMeta second = new() { SessionId = "s1", StartedUtcTicks = 10, StopwatchFrequency = 20, AnchorTimestamp = 30, LibraryVersion = "new", GameVersion = "1.6" };

        using SessionStore store = SessionStore.Open(_dbPath);
        store.WriteSessionMeta(first);
        store.WriteSessionMeta(second);

        SessionMeta? round = store.ReadSessionMeta("s1");
        round.Should().NotBeNull();
        round!.LibraryVersion.Should().Be("new");
        round.StartedUtcTicks.Should().Be(10);
    }

    [Fact]
    public void ReadSessionMeta_returns_null_for_unknown_session()
    {
        using SessionStore store = SessionStore.Open(_dbPath);
        store.ReadSessionMeta("nope").Should().BeNull();
    }

    [Fact]
    public void Open_with_mismatched_schema_drops_and_recreates_tables()
    {
        Directory.CreateDirectory(_tempDir);
        using (SqliteConnection setup = new($"Data Source={_dbPath}"))
        {
            setup.Open();
            using SqliteCommand create = setup.CreateCommand();
            create.CommandText = "CREATE TABLE stale_table (id INTEGER); PRAGMA user_version = 0;";
            create.ExecuteNonQuery();
        }
        SqliteConnection.ClearAllPools();

        using SessionStore store = SessionStore.Open(_dbPath);
        using SqliteConnection probe = new($"Data Source={_dbPath}");
        probe.Open();
        using SqliteCommand cmd = probe.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='stale_table';";
        object? result = cmd.ExecuteScalar();
        result.Should().BeNull("stale table should have been dropped on schema mismatch");

        using SqliteCommand versionCmd = probe.CreateCommand();
        versionCmd.CommandText = "PRAGMA user_version;";
        Convert.ToInt32(versionCmd.ExecuteScalar()).Should().Be(SessionStore.SchemaVersion);
    }

    [Fact]
    public void Open_uses_WAL_journal_mode()
    {
        using SessionStore store = SessionStore.Open(_dbPath);

        using SqliteConnection probe = new($"Data Source={_dbPath}");
        probe.Open();
        using SqliteCommand cmd = probe.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode;";
        string mode = ((string?)cmd.ExecuteScalar() ?? string.Empty).ToLowerInvariant();
        mode.Should().Be("wal");
    }

    [Fact]
    public void Disposed_store_rejects_writes()
    {
        SessionStore store = SessionStore.Open(_dbPath);
        store.Dispose();

        Action act = () => store.WriteSessionMeta(new SessionMeta { SessionId = "x" });
        act.Should().Throw<ObjectDisposedException>();
    }
}
