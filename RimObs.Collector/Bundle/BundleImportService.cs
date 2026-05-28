using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cryptiklemur.RimObs.Collector.Bundle;

public enum BundleImportStatus {
    Ok,
    InvalidArchive,
    MissingManifest,
}

public sealed class BundleImportResult {
    public BundleImportStatus Status { get; init; }
    public BundleImportEntry? Entry { get; init; }
    public BundleManifest? Manifest { get; init; }
}

public sealed class BundleImportService {
    private readonly BundleImportRegistry _registry;

    public BundleImportService(BundleImportRegistry registry) {
        _registry = registry;
    }

    public async Task<BundleImportResult> ImportAsync(Stream archiveStream) {
        List<string> names = new List<string>();
        BundleImportEntry? entry = null;

        try {
            using ZipArchive zip = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false);

            foreach (ZipArchiveEntry e in zip.Entries) {
                if (string.IsNullOrEmpty(e.Name)) continue;
                if (e.FullName != e.Name) {
                    return new BundleImportResult { Status = BundleImportStatus.InvalidArchive };
                }
                if (e.FullName.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(e.FullName)) {
                    return new BundleImportResult { Status = BundleImportStatus.InvalidArchive };
                }
                names.Add(e.FullName);
            }

            if (!names.Contains("manifest.json"))
                return new BundleImportResult { Status = BundleImportStatus.MissingManifest };

            entry = _registry.Register(names.ToArray());

            foreach (ZipArchiveEntry e in zip.Entries) {
                if (string.IsNullOrEmpty(e.Name)) continue;
                string destPath = Path.Combine(entry.TempDir, e.FullName);
                string? destDir = Path.GetDirectoryName(destPath);
                if (destDir is not null) Directory.CreateDirectory(destDir);
                using Stream src = e.Open();
                using FileStream dst = File.Create(destPath);
                await src.CopyToAsync(dst);
            }

            string manifestPath = Path.Combine(entry.TempDir, "manifest.json");
            using FileStream mfs = File.OpenRead(manifestPath);
            BundleManifest? manifest = await JsonSerializer.DeserializeAsync<BundleManifest>(mfs, BundleManifest.JsonOptions);

            return new BundleImportResult {
                Status = BundleImportStatus.Ok,
                Entry = entry,
                Manifest = manifest,
            };
        }
        catch (InvalidDataException) {
            if (entry is not null) _registry.Remove(entry.Token);
            return new BundleImportResult { Status = BundleImportStatus.InvalidArchive };
        }
    }
}
