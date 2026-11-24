using Microsoft.Data.Sqlite;
using PES2017Editor.Cpk;

namespace PES2017Editor.Database;

public sealed class SqliteIndexStore
{
    private readonly string _databasePath;

    public SqliteIndexStore(string? databasePath = null)
    {
        _databasePath = databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PES2017EDITOR", "cache", "index.db");
    }

    public string DatabasePath => _databasePath;

    public async Task SaveAsync(CpkIndexResult result, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync(cancellationToken);
        var journal = connection.CreateCommand();
        journal.CommandText = "PRAGMA journal_mode=WAL;";
        await journal.ExecuteNonQueryAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var schema = connection.CreateCommand();
        schema.Transaction = transaction;
        schema.CommandText = """
            CREATE TABLE IF NOT EXISTS cpk_archives (
                path TEXT PRIMARY KEY,
                file_name TEXT NOT NULL,
                file_size INTEGER NOT NULL,
                native_index INTEGER NOT NULL,
                status TEXT NOT NULL,
                warnings TEXT NULL,
                indexed_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS cpk_entries (
                archive_path TEXT NOT NULL,
                internal_path TEXT NOT NULL,
                file_offset INTEGER NOT NULL,
                stored_size INTEGER NOT NULL,
                original_size INTEGER NOT NULL,
                codec INTEGER NOT NULL,
                file_id TEXT NULL,
                PRIMARY KEY (archive_path, internal_path, file_offset)
            );
            CREATE INDEX IF NOT EXISTS idx_cpk_entries_path ON cpk_entries(internal_path);
            """;
        await schema.ExecuteNonQueryAsync(cancellationToken);

        foreach (var archive in result.Archives)
        {
            var archiveCommand = connection.CreateCommand();
            archiveCommand.Transaction = transaction;
            archiveCommand.CommandText = """
                INSERT INTO cpk_archives(path, file_name, file_size, native_index, status, warnings, indexed_at)
                VALUES ($path, $file_name, $file_size, $native_index, $status, $warnings, $indexed_at)
                ON CONFLICT(path) DO UPDATE SET
                    file_name = excluded.file_name,
                    file_size = excluded.file_size,
                    native_index = excluded.native_index,
                    status = excluded.status,
                    warnings = excluded.warnings,
                    indexed_at = excluded.indexed_at;
                """;
            archiveCommand.Parameters.AddWithValue("$path", archive.Path);
            archiveCommand.Parameters.AddWithValue("$file_name", archive.FileName);
            archiveCommand.Parameters.AddWithValue("$file_size", archive.FileSize);
            archiveCommand.Parameters.AddWithValue("$native_index", archive.HasNativeIndex ? 1 : 0);
            archiveCommand.Parameters.AddWithValue("$status", archive.FailureReason is null ? "indexed" : "partial");
            archiveCommand.Parameters.AddWithValue("$warnings", string.Join(Environment.NewLine, archive.Warnings));
            archiveCommand.Parameters.AddWithValue("$indexed_at", DateTimeOffset.UtcNow.ToString("O"));
            await archiveCommand.ExecuteNonQueryAsync(cancellationToken);

            var deleteEntries = connection.CreateCommand();
            deleteEntries.Transaction = transaction;
            deleteEntries.CommandText = "DELETE FROM cpk_entries WHERE archive_path = $path";
            deleteEntries.Parameters.AddWithValue("$path", archive.Path);
            await deleteEntries.ExecuteNonQueryAsync(cancellationToken);
            foreach (var entry in archive.Entries)
            {
                var entryCommand = connection.CreateCommand();
                entryCommand.Transaction = transaction;
                entryCommand.CommandText = """
                    INSERT INTO cpk_entries(archive_path, internal_path, file_offset, stored_size, original_size, codec, file_id)
                    VALUES ($archive_path, $internal_path, $file_offset, $stored_size, $original_size, $codec, $file_id)
                    """;
                entryCommand.Parameters.AddWithValue("$archive_path", entry.ArchivePath);
                entryCommand.Parameters.AddWithValue("$internal_path", entry.InternalPath);
                entryCommand.Parameters.AddWithValue("$file_offset", entry.FileOffset);
                entryCommand.Parameters.AddWithValue("$stored_size", entry.StoredSize);
                entryCommand.Parameters.AddWithValue("$original_size", entry.OriginalSize);
                entryCommand.Parameters.AddWithValue("$codec", entry.Codec);
                entryCommand.Parameters.AddWithValue("$file_id", (object?)entry.FileId ?? DBNull.Value);
                await entryCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        await transaction.CommitAsync(cancellationToken);
    }
}
