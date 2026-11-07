using System.Text.Json;

namespace Mahjong.Core;

public sealed record GameResult(Guid GameId, string Player, DateTimeOffset FinishedAt, string LayoutId,
    string SetId, int TileCount, double Seconds, int Hints, bool ShuffleUsed, int Seed,
    int UndoCount = 0, string? ChallengeDate = null, bool NameConfirmed = true);

public static class ResultStore
{
    static readonly string[] LayoutIds = ["glade", "stairs", "butterfly", "turtle", "fortress", "dragon"];
    static int ExpectedCount(string id) => id switch { "glade" => 36, "stairs" => 72, "butterfly" => 108, _ => 144 };

    public static List<GameResult> Load(string path)
    {
        if (!File.Exists(path)) return [];
        var results = JsonSerializer.Deserialize<List<GameResult>>(Storage.ReadBounded(path, 16 * 1024 * 1024), Storage.Json)
            ?? throw new InvalidDataException("Invalid result history.");
        if (results.Count > 20000 || results.Any(r => r == null || r.GameId == Guid.Empty || r.Player == null || r.Player.Length > 32 || r.Player.Any(char.IsControl)
            || !LayoutIds.Contains(r.LayoutId) || !Catalog.Sets.Contains(r.SetId) || r.TileCount != ExpectedCount(r.LayoutId)
            || !GameTime.Valid(r.Seconds) || r.Hints < 0 || r.UndoCount < 0 || r.FinishedAt == default
            || !GameTime.ValidDate(r.ChallengeDate))
            || results.Select(r => r.GameId).Distinct().Count() != results.Count)
            throw new InvalidDataException("Invalid result history.");
        return results.OrderByDescending(r => r.FinishedAt).ToList();
    }

    public static GameResult SaveFinished(string path, GameState game, string player, DateTimeOffset? finishedAt = null, bool automatic = false)
    {
        ArgumentNullException.ThrowIfNull(player);
        player = player.Trim();
        if (game.Tiles.Count != 0 || game.GameId == Guid.Empty || !GameTime.Valid(game.Seconds) || game.Hints < 0 || game.UndoCount < 0
            || player.Length > 32 || player.Any(char.IsControl) || !LayoutIds.Contains(game.LayoutId) || !Catalog.Sets.Contains(game.SetId)
            || game.Initial.Count != ExpectedCount(game.LayoutId)
            || (game.CompletedAt is { } date && date == default)
            || !GameTime.ValidDate(game.ChallengeDate)
            || !Generator.Replay(new GameState { Positions = game.Positions, Tiles = new(game.Initial) }, game.InitialSolution))
            throw new InvalidOperationException("Only completed games can be recorded.");
        var results = Load(path);
        var existing = results.SingleOrDefault(r => r.GameId == game.GameId);
        if (existing != null)
        {
            if (automatic || existing.NameConfirmed) return existing;
            var named = existing with { Player = player, NameConfirmed = true };
            results[results.IndexOf(existing)] = named; Storage.Atomic(path, results); return named;
        }
        if (results.Count >= 20000) throw new InvalidDataException("Result history is full. Export a backup first.");
        var result = new GameResult(game.GameId, player, game.CompletedAt ?? finishedAt ?? DateTimeOffset.UtcNow, game.LayoutId,
            game.SetId, game.Initial.Count, game.Seconds, game.Hints, game.ShuffleUsed, game.Seed, game.UndoCount, game.ChallengeDate, !automatic);
        results.Insert(0, result);
        Storage.Atomic(path, results);
        return result;
    }
}
