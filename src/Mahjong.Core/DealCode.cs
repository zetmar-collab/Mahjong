using System.Security.Cryptography;
using System.Text;

namespace Mahjong.Core;

/// <summary>Versioned complete original deal, independent of solver timing and runtime RNG changes.</summary>
public static class DealCode
{
    public static string Encode(GameState game)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write((byte)1); writer.Write(game.LayoutId); writer.Write(game.SetId); writer.Write(game.Seed);
            var positions = game.Positions.OrderBy(p => p.Id).ToArray();
            var symbols = Catalog.Deck(game.SetId, positions.Length).Distinct().ToArray();
            writer.Write((byte)positions.Length);
            foreach (var p in positions) writer.Write(checked((byte)Array.IndexOf(symbols, game.Initial[p.Id])));
            foreach (var pair in game.InitialSolution)
            {
                writer.Write(checked((byte)Array.FindIndex(positions, p => p.Id == pair.A)));
                writer.Write(checked((byte)Array.FindIndex(positions, p => p.Id == pair.B)));
            }
        }
        var payload = stream.ToArray();
        return "MJ1-" + Convert.ToBase64String(payload.Concat(SHA256.HashData(payload).Take(8)).ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
    public static GameState Decode(string code, IReadOnlyList<Layout> layouts)
    {
        try
        {
            code = code.Trim();
            if (!code.StartsWith("MJ1-") || code.Length > 1024) throw new InvalidDataException("Invalid deal code.");
            string encoded = code[4..].Replace('-', '+').Replace('_', '/');
            var all = Convert.FromBase64String(encoded.PadRight((encoded.Length + 3) / 4 * 4, '='));
            if (all.Length < 10) throw new InvalidDataException();
            var payload = all[..^8];
            if (!SHA256.HashData(payload).Take(8).SequenceEqual(all[^8..])) throw new InvalidDataException("Code checksum differs.");
            using var stream = new MemoryStream(payload);
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            if (reader.ReadByte() != 1) throw new InvalidDataException();
            string layoutId = reader.ReadString();
            var layout = layouts.Single(l => l.Id == layoutId);
            string set = reader.ReadString(); int seed = reader.ReadInt32();
            var positions = layout.Positions.OrderBy(p => p.Id).ToArray();
            if (reader.ReadByte() != positions.Length) throw new InvalidDataException();
            var symbols = Catalog.Deck(set, layout.Count).Distinct().ToArray();
            Dictionary<int, Symbol> tiles = [];
            foreach (var p in positions) tiles[p.Id] = symbols[reader.ReadByte()];
            List<Pair> solution = [];
            for (int i = 0; i < positions.Length / 2; i++) solution.Add(new(positions[reader.ReadByte()].Id, positions[reader.ReadByte()].Id));
            if (stream.Position != stream.Length) throw new InvalidDataException();
            var game = new GameState { LayoutId = layout.Id, SetId = set, Seed = seed, Positions = layout.Positions.ToList(),
                Tiles = tiles, Initial = new(tiles), Solution = solution, InitialSolution = solution.ToList() };
            if (!Storage.Validate(game, layouts, out _)) throw new InvalidDataException();
            return game;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or InvalidOperationException or IndexOutOfRangeException or FormatException)
        { throw new InvalidDataException("Invalid or unsupported deal code.", ex); }
    }
    public static GameState Daily(DateOnly day, IReadOnlyList<Layout> layouts)
    {
        // Version 1: fixed geometry and PRNG; no clock-based fallback.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("MahjongDaily1:" + day.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)));
        int seed = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(hash);
        var layout = layouts.Single(l => l.Id == "turtle");
        var assignment = Generator.Assign(layout.Positions, Catalog.Deck("classic", layout.Count), seed, 100000, int.MaxValue)
            ?? throw new InvalidOperationException("Daily challenge exceeded the search budget.");
        return new() { LayoutId = layout.Id, SetId = "classic", Seed = seed, Positions = layout.Positions.ToList(), Tiles = assignment.Tiles,
            Initial = new(assignment.Tiles), Solution = assignment.Solution, InitialSolution = assignment.Solution.ToList(), ChallengeDate = day.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) };
    }
}
