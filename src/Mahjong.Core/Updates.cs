using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mahjong.Core;

public sealed record ReleaseUpdate(Version Version, Uri Download, long Size, string Sha256);

/// <summary>Only stable Windows installers published by this project's GitHub repository.</summary>
public sealed class UpdateService(HttpClient client)
{
    public const string Repository = "https://github.com/zetmar-collab/Mahjong";
    public const string LatestApi = "https://api.github.com/repos/zetmar-collab/Mahjong/releases/latest";
    const long MaxInstallerSize = 256L * 1024 * 1024;
    public static ReleaseUpdate? ParseRelease(string json, Version current)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean()) return null;
        string tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!Regex.IsMatch(tag, @"^v\d+\.\d+\.\d+$") || !Version.TryParse(tag[1..], out var version)) return null;
        var normalized = new Version(current.Major, current.Minor, Math.Max(0, current.Build));
        if (version <= normalized) return null;
        string name = $"Mahjong-Solitaire-{version}-Setup-x64.exe";
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            if (asset.GetProperty("name").GetString() != name || asset.GetProperty("state").GetString() != "uploaded") continue;
            string digest = asset.TryGetProperty("digest", out var hash) ? hash.GetString() ?? "" : "";
            if (!Regex.IsMatch(digest, @"^sha256:[0-9a-fA-F]{64}$")) continue;
            var update = new ReleaseUpdate(version, new Uri(asset.GetProperty("browser_download_url").GetString()!),
                asset.GetProperty("size").GetInt64(), digest[7..]);
            Validate(update); return update;
        }
        return null;
    }
    static void Validate(ReleaseUpdate update)
    {
        string expected = $"{Repository}/releases/download/v{update.Version}/Mahjong-Solitaire-{update.Version}-Setup-x64.exe";
        if (update.Download.AbsoluteUri != expected || update.Size <= 0 || update.Size > MaxInstallerSize
            || !Regex.IsMatch(update.Sha256, @"^[0-9a-fA-F]{64}$")) throw new InvalidDataException("Invalid release asset.");
    }
    public async Task<ReleaseUpdate?> CheckAsync(Version current, CancellationToken token = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestApi);
        request.Headers.UserAgent.ParseAdd("QuietMahjong/" + current);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        using var memory = new MemoryStream();
        await CopyBounded(await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false), memory, 1024 * 1024, null, token).ConfigureAwait(false);
        return ParseRelease(System.Text.Encoding.UTF8.GetString(memory.ToArray()), current);
    }
    static async Task CopyBounded(Stream source, Stream target, long limit, IProgress<double>? progress, CancellationToken token)
    {
        await using (source.ConfigureAwait(false))
        {
            byte[] buffer = new byte[65536]; long total = 0; int read;
            while ((read = await source.ReadAsync(buffer, token).ConfigureAwait(false)) > 0)
            {
                total += read;
                if (total > limit) throw new InvalidDataException("Download exceeds declared size.");
                await target.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
                progress?.Report(total * 100.0 / limit);
            }
        }
    }
    public async Task<string> DownloadAsync(ReleaseUpdate update, string directory, IProgress<double>? progress = null, CancellationToken token = default)
    {
        Validate(update);
        string folder = Path.Combine(directory, "Updates", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "Mahjong-Setup.exe"), partial = path + ".part";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, update.Download);
            request.Headers.UserAgent.ParseAdd("QuietMahjong-Updater/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is long length && length != update.Size) throw new InvalidDataException("Size differs.");
            await using (var file = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, true))
                await CopyBounded(await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false), file, update.Size, progress, token).ConfigureAwait(false);
            if (!await VerifyAsync(partial, update, token).ConfigureAwait(false)) throw new InvalidDataException("Installer checksum differs.");
            token.ThrowIfCancellationRequested();
            File.Move(partial, path); return path;
        }
        catch
        {
            if (File.Exists(partial)) File.Delete(partial);
            throw;
        }
    }
    public static async Task<bool> VerifyAsync(string path, ReleaseUpdate update, CancellationToken token = default)
    {
        Validate(update);
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
        return file.Length == update.Size && string.Equals(Convert.ToHexString(await SHA256.HashDataAsync(file, token).ConfigureAwait(false)), update.Sha256, StringComparison.OrdinalIgnoreCase);
    }
}
