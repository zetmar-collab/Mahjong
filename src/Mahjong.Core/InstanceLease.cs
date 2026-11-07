using System.Security.Cryptography;
using System.Text;

namespace Mahjong.Core;

/// <summary>One writer per data directory, including portable and installed copies.</summary>
public sealed class InstanceLease : IDisposable
{
    readonly FileStream stream;
    public static string Key(string directory) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant())));
    InstanceLease(FileStream stream) => this.stream = stream;
    public static InstanceLease? TryAcquire(string directory)
    {
        Directory.CreateDirectory(directory);
        try { return new(new FileStream(Path.Combine(directory, "instance.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)); }
        catch (IOException) { return null; }
    }
    public void Dispose() => stream.Dispose();
}
