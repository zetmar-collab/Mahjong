using System.IO;
using System.Text.Json;
using Mahjong.Core;

namespace Mahjong.App;

/// <summary>Immutable snapshots written in order; flush before backup, import and exit.</summary>
internal sealed class SaveQueue(Action<Exception> failed)
{
    readonly object gate = new();
    Task pending = Task.CompletedTask;
    public void Enqueue<T>(string path, T data)
    {
        // Freeze mutable game state before dispatching it to the writer.
        var snapshot = JsonSerializer.SerializeToElement(data, Storage.Json);
        lock (gate)
            pending = pending.ContinueWith(_ =>
            {
                try { Storage.Atomic(path, snapshot); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { failed(ex); }
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }
    public void Flush() { Task current; lock (gate) current = pending; current.GetAwaiter().GetResult(); }
}
