using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Mahjong.Core;

public static class BackupService
{
    static readonly Dictionary<string, int> Limits = new(StringComparer.Ordinal)
    { ["game.json"] = 4 * 1024 * 1024, ["settings.json"] = 65536, ["results.json"] = 16 * 1024 * 1024, ["background.png"] = 32 * 1024 * 1024 };
    sealed record Manifest(int Version, Dictionary<string, string> Hashes);

    public static void Export(string directory, string destination)
    {
        string temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = File.Create(temp))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Dictionary<string, string> hashes = [];
                foreach (var (name, limit) in Limits)
                {
                    string path = Path.Combine(directory, name);
                    if (!File.Exists(path)) continue;
                    using var input = File.OpenRead(path);
                    if (input.Length > limit) throw new InvalidDataException("Data exceeds backup limits.");
                    using var memory = new MemoryStream(); input.CopyTo(memory); var bytes = memory.ToArray();
                    hashes[name] = Convert.ToHexString(SHA256.HashData(bytes));
                    using var output = zip.CreateEntry(name).Open(); output.Write(bytes);
                }
                using var manifest = zip.CreateEntry("manifest.json").Open();
                JsonSerializer.Serialize(manifest, new Manifest(1, hashes), Storage.Json);
            }
            File.Move(temp, destination, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    /// <summary>Validates every entry before touching player files. Missing entries preserve current data.</summary>
    public static string Import(string source, string directory, IReadOnlyList<Layout> layouts, Action<string> validateImage)
    {
        string stage = Path.Combine(directory, "Backups", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        string incoming = Path.Combine(stage, "incoming"); Directory.CreateDirectory(incoming);
        using (var input = File.OpenRead(source))
        {
            if (input.Length > 54L * 1024 * 1024) throw new InvalidDataException("Archive exceeds size limit.");
            using var zip = new ZipArchive(input, ZipArchiveMode.Read);
            if (zip.Entries.Count > 5 || zip.Entries.Count == 0 || zip.Entries.Select(e => e.FullName).Distinct().Count() != zip.Entries.Count)
                throw new InvalidDataException("Invalid archive entries.");
            var metadata = zip.GetEntry("manifest.json") ?? throw new InvalidDataException("Missing backup manifest.");
            if (metadata.Length > 4096) throw new InvalidDataException();
            Manifest manifest;
            using (var stream = metadata.Open())
            using (var memory = new MemoryStream())
            {
                var buffer = new byte[4097]; int read;
                while ((read = stream.Read(buffer)) > 0)
                {
                    if (memory.Length + read > 4096) throw new InvalidDataException();
                    memory.Write(buffer, 0, read);
                }
                manifest = JsonSerializer.Deserialize<Manifest>(memory.ToArray(), Storage.Json) ?? throw new InvalidDataException();
            }
            if (manifest.Version != 1 || manifest.Hashes == null || manifest.Hashes.Count != zip.Entries.Count - 1) throw new InvalidDataException();
            foreach (var entry in zip.Entries.Where(e => e.FullName != "manifest.json"))
            {
                if (!Limits.TryGetValue(entry.FullName, out int limit) || entry.Length > limit || !manifest.Hashes.TryGetValue(entry.FullName, out var hash))
                    throw new InvalidDataException("Unexpected file or size.");
                // Exact whitelist means no extraction of paths, links or arbitrary user files.
                using var stream = entry.Open(); using var memory = new MemoryStream();
                var buffer = new byte[8192]; int read;
                while ((read = stream.Read(buffer)) > 0)
                {
                    if (memory.Length + read > limit) throw new InvalidDataException();
                    memory.Write(buffer, 0, read);
                }
                var bytes = memory.ToArray();
                if (!string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), hash, StringComparison.Ordinal)) throw new InvalidDataException("Checksum differs.");
                File.WriteAllBytes(Path.Combine(incoming, entry.FullName), bytes);
            }
        }
        string gamePath = Path.Combine(incoming, "game.json"), settingsPath = Path.Combine(incoming, "settings.json");
        if (File.Exists(gamePath) && !Storage.Load(gamePath, layouts, out _)) throw new InvalidDataException("Invalid saved game.");
        _ = ResultStore.Load(Path.Combine(incoming, "results.json"));
        if (File.Exists(settingsPath))
        {
            var prefs = JsonSerializer.Deserialize<Preferences>(Storage.ReadBounded(settingsPath, 65536), Storage.Json) ?? throw new InvalidDataException();
            if (prefs.Language is not ("pl" or "en") || prefs.Theme is not ("light" or "dark" or "contrast")
                || prefs.Background is not ("felt" or "wood" or "forest" or "paper" or "minimal" or "custom")
                || !Catalog.Sets.Contains(prefs.Set) || !layouts.Any(l => l.Id == prefs.Layout)
                || !double.IsFinite(prefs.Volume) || prefs.Volume is < 0 or > 1 || !double.IsFinite(prefs.Zoom) || prefs.Zoom is < 1 or > 3)
                throw new InvalidDataException("Invalid settings.");
            if (prefs.Background == "custom" && !File.Exists(Path.Combine(incoming, "background.png"))) throw new InvalidDataException("Missing custom background.");
        }
        if (File.Exists(Path.Combine(incoming, "background.png"))) validateImage(Path.Combine(incoming, "background.png"));
        string previous = Path.Combine(stage, "previous"); Directory.CreateDirectory(previous);
        var names = Directory.GetFiles(incoming).Select(Path.GetFileName).Cast<string>().ToArray();
        foreach (string name in names)
            if (File.Exists(Path.Combine(directory, name))) File.Copy(Path.Combine(directory, name), Path.Combine(previous, name));
        List<string> applied = [];
        try
        {
            foreach (string name in names)
            {
                CopyAtomic(Path.Combine(incoming, name), Path.Combine(directory, name));
                applied.Add(name);
            }
        }
        catch
        {
            foreach (string name in applied.AsEnumerable().Reverse())
            {
                string old = Path.Combine(previous, name), target = Path.Combine(directory, name);
                if (File.Exists(old)) CopyAtomic(old, target);
                else if (File.Exists(target)) File.Move(target, Path.Combine(stage, "failed-" + name));
            }
            throw;
        }
        return previous;
    }
    static void CopyAtomic(string source, string destination)
    {
        string temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var input = File.OpenRead(source))
            using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { input.CopyTo(output); output.Flush(true); }
            File.Move(temp, destination, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
