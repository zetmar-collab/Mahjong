using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Mahjong.Core;

internal static class UpdateTests
{
    static void Check(bool value) { if (!value) throw new Exception("Update assertion failed."); }
    static readonly byte[] Payload = Enumerable.Range(0, 4096).Select(i => (byte)i).ToArray();
    static readonly string Hash = Convert.ToHexString(SHA256.HashData(Payload));
    static string Manifest(string version = "1.4.0", string? digest = null, string? url = null, bool prerelease = false, bool draft = false) => JsonSerializer.Serialize(new {
        tag_name = "v" + version, draft, prerelease,
        assets = new[] { new { name = $"Mahjong-Solitaire-{version}-Setup-x64.exe", state = "uploaded", size = Payload.Length,
            digest = digest ?? "sha256:" + Hash, browser_download_url = url ?? $"{UpdateService.Repository}/releases/download/v{version}/Mahjong-Solitaire-{version}-Setup-x64.exe" } }
    });
    sealed class Reply(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => reply(request, token);
    }
    static HttpClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) => new(new Reply(reply));
    static HttpResponseMessage Bytes(byte[] bytes) => new(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
    sealed class StreamingPayload(byte[] bytes, Action? afterRead = null) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            int count = await base.ReadAsync(buffer, token); afterRead?.Invoke(); return count;
        }
    }
    public static void Register(Action<string, Action> test)
    {
        test("Updates: stable versions, numeric ordering, drafts, missing hashes and pinned origin", () => {
            Check(UpdateService.ParseRelease(Manifest(), new(1, 3, 0))!.Version == new Version(1, 4, 0));
            Check(UpdateService.ParseRelease(Manifest("1.10.0"), new(1, 9, 0)) != null);
            foreach (var current in new[] { new Version(1, 4, 0), new Version(1, 4, 0, 0), new Version(2, 0, 0) })
                Check(UpdateService.ParseRelease(Manifest(), current) == null);
            Check(UpdateService.ParseRelease(Manifest(prerelease: true), new(1, 0, 0)) == null);
            Check(UpdateService.ParseRelease(Manifest(draft: true), new(1, 0, 0)) == null);
            Check(UpdateService.ParseRelease(Manifest("1.4.0-beta"), new(1, 0, 0)) == null);
            Check(UpdateService.ParseRelease(Manifest(digest: ""), new(1, 0, 0)) == null);
            foreach (string url in new[] { "https://evil.example/setup.exe", "http://github.com/zetmar-collab/Mahjong/releases/download/v1.4.0/Mahjong-Solitaire-1.4.0-Setup-x64.exe",
                UpdateService.Repository + "/releases/download/v1.4.0/other.exe", "https://github.com/other/Mahjong/releases/download/v1.4.0/Mahjong-Solitaire-1.4.0-Setup-x64.exe" })
            {
                bool refused = false; try { UpdateService.ParseRelease(Manifest(url: url), new(1, 0, 0)); } catch (InvalidDataException) { refused = true; }
                Check(refused);
            }
        });
        test("Updates: bounded API request, no-release response, offline and cancellation", () => {
            using var http = Client((request, token) => {
                Check(request.RequestUri!.AbsoluteUri == UpdateService.LatestApi && request.Headers.UserAgent.Count > 0);
                return Task.FromResult(Bytes(System.Text.Encoding.UTF8.GetBytes(Manifest())));
            });
            Check(new UpdateService(http).CheckAsync(new(1, 3, 0)).GetAwaiter().GetResult() != null);
            using var absent = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
            Check(new UpdateService(absent).CheckAsync(new(1, 3, 0)).GetAwaiter().GetResult() == null);
            using var huge = Client((_, _) => Task.FromResult(Bytes(new byte[1024 * 1024 + 1])));
            bool bounded = false; try { new UpdateService(huge).CheckAsync(new(1, 3, 0)).GetAwaiter().GetResult(); } catch (InvalidDataException) { bounded = true; } Check(bounded);
            using var offline = Client((_, _) => throw new HttpRequestException("Offline"));
            bool failed = false; try { new UpdateService(offline).CheckAsync(new(1, 3, 0)).GetAwaiter().GetResult(); } catch (HttpRequestException) { failed = true; } Check(failed);
            using var cancelled = Client((_, token) => Task.FromCanceled<HttpResponseMessage>(token));
            using var stop = new CancellationTokenSource(); stop.Cancel();
            bool canceled = false; try { new UpdateService(cancelled).CheckAsync(new(1, 3, 0), stop.Token).GetAwaiter().GetResult(); } catch (OperationCanceledException) { canceled = true; } Check(canceled);
        });
        test("Updates: download hash, size, failed-file cleanup and recheck before installation", () => {
            string directory = Path.Combine(Path.GetTempPath(), "MahjongUpdates-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var release = UpdateService.ParseRelease(Manifest(), new(1, 3, 0))!;
                using var http = Client((_, _) => Task.FromResult(Bytes(Payload)));
                var service = new UpdateService(http);
                string downloaded = service.DownloadAsync(release, directory).GetAwaiter().GetResult();
                Check(File.ReadAllBytes(downloaded).SequenceEqual(Payload));
                Check(UpdateService.VerifyAsync(downloaded, release).GetAwaiter().GetResult());
                File.WriteAllBytes(downloaded, new byte[Payload.Length]);
                Check(!UpdateService.VerifyAsync(downloaded, release).GetAwaiter().GetResult());
                foreach (var invalid in new[] { release with { Sha256 = new string('0', 64) }, release with { Size = Payload.Length - 1 }, release with { Size = Payload.Length + 1 } })
                {
                    bool refused = false; try { service.DownloadAsync(invalid, directory).GetAwaiter().GetResult(); } catch (InvalidDataException) { refused = true; } Check(refused);
                }
                using var chunked = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StreamingPayload(Payload)) }));
                foreach (long size in new[] { Payload.Length - 1L, Payload.Length + 1L })
                {
                    bool refused = false; try { new UpdateService(chunked).DownloadAsync(release with { Size = size }, directory).GetAwaiter().GetResult(); } catch (InvalidDataException) { refused = true; } Check(refused);
                }
                using var midwayStop = new CancellationTokenSource();
                using var interrupted = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StreamingPayload(Payload, midwayStop.Cancel)) }));
                bool interruptedCleanly = false;
                try { new UpdateService(interrupted).DownloadAsync(release, directory, token: midwayStop.Token).GetAwaiter().GetResult(); } catch (OperationCanceledException) { interruptedCleanly = true; } Check(interruptedCleanly);
                using var stop = new CancellationTokenSource(); stop.Cancel();
                bool cancelled = false; try { service.DownloadAsync(release, directory, token: stop.Token).GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; } Check(cancelled);
                Check(!Directory.GetFiles(directory, "*.part", SearchOption.AllDirectories).Any());
                Check(Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories).Length == 1);
            }
            finally { Directory.Delete(directory, true); }
        });
    }
}
