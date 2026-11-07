namespace Mahjong.Core;

public sealed record RemovedPair(int A, Symbol First, int B, Symbol Second);

public static class GameActions
{
    public static bool Remove(GameState game, int a, int b)
    {
        if (!game.Tiles.TryGetValue(a, out var first) || !game.Tiles.TryGetValue(b, out var second)) return false;
        if (!Rules.Remove(game, a, b)) return false;
        game.Moves.Add(new(a, first, b, second));
        if (game.Tiles.Count == 0) game.CompletedAt ??= DateTimeOffset.UtcNow;
        return true;
    }
    public static bool Undo(GameState game)
    {
        if (game.Tiles.Count == 0 || game.Moves.Count == 0 || game.UndoCount == int.MaxValue) return false;
        var move = game.Moves[^1];
        game.Tiles.Add(move.A, move.First); game.Tiles.Add(move.B, move.Second);
        game.Moves.RemoveAt(game.Moves.Count - 1); game.UndoCount++;
        return true;
    }
    public static bool ValidHistory(GameState game)
    {
        if (game.Moves == null || game.Moves.Count > 72) return false;
        var check = new GameState { Positions = game.Positions, Tiles = new(game.Tiles) };
        foreach (var move in game.Moves.AsEnumerable().Reverse())
        {
            if (move == null || move.A == move.B || !game.Initial.ContainsKey(move.A) || !game.Initial.ContainsKey(move.B)
                || check.Tiles.ContainsKey(move.A) || check.Tiles.ContainsKey(move.B)) return false;
            check.Tiles.Add(move.A, move.First); check.Tiles.Add(move.B, move.Second);
            if (!Rules.Remove(check, move.A, move.B)) return false;
            check.Tiles.Add(move.A, move.First); check.Tiles.Add(move.B, move.Second);
        }
        var deck = game.Initial.Values.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());
        return check.Tiles.Values.All(s => s != null && deck.ContainsKey(s))
            && check.Tiles.Values.GroupBy(s => s).All(g => g.Count() <= deck[g.Key])
            && (game.ShuffleUsed || check.Tiles.All(t => game.Initial[t.Key] == t.Value));
    }
}
