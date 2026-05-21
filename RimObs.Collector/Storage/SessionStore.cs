using System.Globalization;
using Cryptiklemur.RimObs.Wire;
using Microsoft.Data.Sqlite;

namespace Cryptiklemur.RimObs.Collector.Storage;

public sealed class SessionStore : IDisposable
{
    public const int SchemaVersion = 1;
    private const string SchemaVersionPragma = "user_version";

    private readonly SqliteConnection _connection;
    private bool _disposed;

    private SessionStore(SqliteConnection connection)
    {
        _connection = connection;
    }

    public string DatabasePath => _connection.DataSource;

    public static SessionStore Open(string dbPath)
    {
        if (string.IsNullOrWhiteSpace(dbPath))
            throw new ArgumentException("dbPath must be a non-empty path", nameof(dbPath));

        string? directory = Path.GetDirectoryName(Path.GetFullPath(dbPath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        SqliteConnectionStringBuilder csb = new()
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Default,
        };
        SqliteConnection connection = new(csb.ConnectionString);
        connection.Open();

        SetWalMode(connection);

        int existing = ReadSchemaVersion(connection);
        if (existing != SchemaVersion)
        {
            DropAllTables(connection);
            CreateSchema(connection);
            WriteSchemaVersion(connection, SchemaVersion);
        }

        return new SessionStore(connection);
    }

    public void WriteSessionMeta(SessionMeta meta)
    {
        ArgumentNullException.ThrowIfNull(meta);
        ThrowIfDisposed();

        using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = @"
INSERT INTO session_meta (session_id, started_utc_ticks, stopwatch_frequency, anchor_timestamp, library_version, game_version)
VALUES ($id, $started, $freq, $anchor, $lib, $game)
ON CONFLICT(session_id) DO UPDATE SET
    started_utc_ticks = excluded.started_utc_ticks,
    stopwatch_frequency = excluded.stopwatch_frequency,
    anchor_timestamp = excluded.anchor_timestamp,
    library_version = excluded.library_version,
    game_version = excluded.game_version;
";
        cmd.Parameters.AddWithValue("$id", meta.SessionId);
        cmd.Parameters.AddWithValue("$started", meta.StartedUtcTicks);
        cmd.Parameters.AddWithValue("$freq", meta.StopwatchFrequency);
        cmd.Parameters.AddWithValue("$anchor", meta.AnchorTimestamp);
        cmd.Parameters.AddWithValue("$lib", meta.LibraryVersion ?? string.Empty);
        cmd.Parameters.AddWithValue("$game", meta.GameVersion ?? string.Empty);
        cmd.ExecuteNonQuery();
    }

    public SessionMeta? ReadSessionMeta(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("sessionId must be non-empty", nameof(sessionId));
        ThrowIfDisposed();

        using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = @"
SELECT session_id, started_utc_ticks, stopwatch_frequency, anchor_timestamp, library_version, game_version
FROM session_meta WHERE session_id = $id;
";
        cmd.Parameters.AddWithValue("$id", sessionId);

        using SqliteDataReader reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new SessionMeta
        {
            SessionId = reader.GetString(0),
            StartedUtcTicks = reader.GetInt64(1),
            StopwatchFrequency = reader.GetInt64(2),
            AnchorTimestamp = reader.GetInt64(3),
            LibraryVersion = reader.GetString(4),
            GameVersion = reader.GetString(5),
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _connection.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SessionStore));
    }

    private static void SetWalMode(SqliteConnection connection)
    {
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL;";
        cmd.ExecuteNonQuery();
    }

    private static int ReadSchemaVersion(SqliteConnection connection)
    {
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA {SchemaVersionPragma};";
        object? result = cmd.ExecuteScalar();
        if (result is long l)
            return (int)l;
        if (result is int i)
            return i;
        return 0;
    }

    private static void WriteSchemaVersion(SqliteConnection connection, int version)
    {
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = string.Create(
            CultureInfo.InvariantCulture,
            $"PRAGMA {SchemaVersionPragma} = {version};");
        cmd.ExecuteNonQuery();
    }

    private static void DropAllTables(SqliteConnection connection)
    {
        using SqliteCommand listCmd = connection.CreateCommand();
        listCmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';";
        List<string> tables = [];
        using (SqliteDataReader reader = listCmd.ExecuteReader())
        {
            while (reader.Read())
                tables.Add(reader.GetString(0));
        }

        foreach (string table in tables)
        {
            using SqliteCommand dropCmd = connection.CreateCommand();
            dropCmd.CommandText = $"DROP TABLE IF EXISTS \"{table}\";";
            dropCmd.ExecuteNonQuery();
        }
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE session_meta (
    session_id TEXT PRIMARY KEY NOT NULL,
    started_utc_ticks INTEGER NOT NULL,
    stopwatch_frequency INTEGER NOT NULL,
    anchor_timestamp INTEGER NOT NULL,
    library_version TEXT NOT NULL,
    game_version TEXT NOT NULL
) WITHOUT ROWID;
";
        cmd.ExecuteNonQuery();
    }
}
