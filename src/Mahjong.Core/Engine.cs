using System.Diagnostics;
using System.Text.Json;

namespace Mahjong.Core;

public record Position(int Id, double X, double Y, int Z);
public record Layout(string Id, int Count, List<Position> Positions);
public record Symbol(string Id, string Match, string Family, int Rank);
public record Pair(int A, int B);
public sealed class GameState
{
    public int Version { get; set; } = 1;
    public Guid GameId { get; set; } = Guid.NewGuid();
    public string LayoutId { get; set; } = "glade";
    public string SetId { get; set; } = "geometry";
    public int Seed { get; set; }
    public List<Position> Positions { get; set; } = [];
    public Dictionary<int, Symbol> Initial { get; set; } = [];
    public Dictionary<int, Symbol> Tiles { get; set; } = [];
    public List<Pair> Solution { get; set; } = [];
    public List<Pair> InitialSolution { get; set; } = [];
    public double Seconds { get; set; }
    public int Hints { get; set; }
    public bool ShuffleUsed { get; set; }
}

public static class Rules
{
    const double Epsilon = 0.00001;
    public static bool Overlap(double a, double b) => a < b + 2 - Epsilon && b < a + 2 - Epsilon;
    public static bool Free(Position p, IEnumerable<Position> active)
    {
        bool left = false, right = false;
        foreach (var q in active)
        {
            if (p.Id == q.Id) continue;
            if (q.Z > p.Z && Overlap(p.X, q.X) && Overlap(p.Y, q.Y)) return false;
            if (q.Z != p.Z || !Overlap(p.Y, q.Y)) continue;
            left |= Math.Abs(q.X + 2 - p.X) < Epsilon;
            right |= Math.Abs(p.X + 2 - q.X) < Epsilon;
        }
        return !left || !right;
    }
    public static bool Matches(Symbol a, Symbol b) => a.Match == b.Match;
    public static List<Position> Active(GameState g) => g.Positions.Where(p => g.Tiles.ContainsKey(p.Id)).ToList();
    public static List<Pair> Pairs(GameState g)
    {
        var active = Active(g);
        var free = active.Where(p => Free(p, active)).ToArray();
        List<Pair> pairs = [];
        for (int i = 0; i < free.Length; i++)
            for (int j = i + 1; j < free.Length; j++)
                if (Matches(g.Tiles[free[i].Id], g.Tiles[free[j].Id])) pairs.Add(new(free[i].Id, free[j].Id));
        return pairs;
    }
    public static bool Remove(GameState g, int a, int b)
    {
        if (a == b || !g.Tiles.TryGetValue(a, out var sa) || !g.Tiles.TryGetValue(b, out var sb) || !Matches(sa, sb)) return false;
        var active = Active(g);
        if (!Free(active.Single(p => p.Id == a), active) || !Free(active.Single(p => p.Id == b), active)) return false;
        g.Tiles.Remove(a); g.Tiles.Remove(b);
        return true;
    }
    public static void Validate(Layout l)
    {
        if (l.Positions.Count != l.Count || l.Count % 2 != 0 || l.Count < 2 || l.Count > 144 || l.Positions.Select(p => p.Id).Distinct().Count() != l.Count)
            throw new InvalidDataException("Invalid layout size or identifiers.");
        foreach (var p in l.Positions)
        {
            if (!double.IsFinite(p.X) || !double.IsFinite(p.Y) || p.Z < 0 || p.Z > 8) throw new InvalidDataException("Invalid coordinates.");
            foreach (var q in l.Positions.Where(q => q.Id != p.Id && q.Z == p.Z))
                if (Overlap(p.X, q.X) && Overlap(p.Y, q.Y)) throw new InvalidDataException("Tiles intersect on one layer.");
            if (p.Z > 0)
            {
                // A half-unit grid verifies complete support, including half-tile offsets.
                for (double x = p.X + .25; x < p.X + 2; x += .5)
                    for (double y = p.Y + .25; y < p.Y + 2; y += .5)
                        if (!l.Positions.Any(q => q.Z == p.Z - 1 && x >= q.X && x < q.X + 2 && y >= q.Y && y < q.Y + 2))
                            throw new InvalidDataException("Unsupported tile.");
            }
        }
    }
}

public static class Catalog
{
    public static readonly string[] Sets = ["geometry", "nature", "classic"];
    public static List<Layout> LoadLayouts(string path)
    {
        var layouts = JsonSerializer.Deserialize<List<Layout>>(File.ReadAllText(path), Storage.Json) ?? throw new InvalidDataException();
        foreach (var l in layouts) Rules.Validate(l);
        return layouts;
    }
    public static List<Symbol> Deck(string set, int count)
    {
        if (!Sets.Contains(set) || count % 2 != 0 || count > 144) throw new ArgumentException("Invalid deck.");
        List<Symbol> deck = [];
        if (set == "classic")
        {
            foreach (var family in new[] { "circles", "bamboo", "characters" })
                for (int rank = 1; rank <= 9; rank++)
                    for (int copy = 0; copy < 4; copy++) deck.Add(new($"{family}{rank}", $"{family}{rank}", family, rank));
            foreach (var (family, ranks) in new[] { ("wind", 4), ("dragon", 3) })
                for (int rank = 1; rank <= ranks; rank++)
                    for (int copy = 0; copy < 4; copy++) deck.Add(new($"{family}{rank}", $"{family}{rank}", family, rank));
            foreach (var family in new[] { "flower", "season" })
                for (int rank = 1; rank <= 4; rank++) deck.Add(new($"{family}{rank}", family, family, rank));
        }
        else
        {
            for (int rank = 1; rank <= 36; rank++)
                for (int copy = 0; copy < 4; copy++) deck.Add(new($"{set}{rank}", $"{set}{rank}", set, rank));
        }
        // Select whole matching pairs, including distinct flowers / seasons in the full deck.
        return deck.GroupBy(s => s.Match).SelectMany(g => g.Chunk(2)).Take(count / 2).SelectMany(g => g).ToList();
    }
}

public static class Generator
{
    public static List<Pair>? Sequence(List<Position> positions, int seed, int maxNodes = 100000, int milliseconds = 2500)
    {
        if (positions.Count % 2 != 0) return null;
        var random = new Random(seed);
        var watch = Stopwatch.StartNew();
        int nodes = 0;
        HashSet<string> dead = [];
        List<Pair> path = [];
        bool Search(List<Position> active)
        {
            if (active.Count == 0) return true;
            if (++nodes > maxNodes || watch.ElapsedMilliseconds > milliseconds) return false;
            string key = string.Join(',', active.Select(p => p.Id));
            if (dead.Contains(key)) return false;
            var free = active.Where(p => Rules.Free(p, active)).OrderBy(_ => random.Next()).ToArray();
            for (int i = 0; i < free.Length; i++)
                for (int j = i + 1; j < free.Length; j++)
                {
                    path.Add(new(free[i].Id, free[j].Id));
                    if (Search(active.Where(p => p.Id != free[i].Id && p.Id != free[j].Id).ToList())) return true;
                    path.RemoveAt(path.Count - 1);
                    if (nodes > maxNodes || watch.ElapsedMilliseconds > milliseconds) return false;
                }
            dead.Add(key);
            return false;
        }
        return Search(positions.OrderBy(p => p.Id).ToList()) ? path : null;
    }
    public static (Dictionary<int, Symbol> Tiles, List<Pair> Solution)? Assign(List<Position> positions, IEnumerable<Symbol> symbols, int seed)
    {
        var deck = symbols.ToList();
        if (deck.Count != positions.Count || deck.GroupBy(s => s.Match).Any(g => g.Count() % 2 != 0)) return null;
        var sequence = Sequence(positions, seed);
        if (sequence == null) return null;
        var random = new Random(seed ^ 0x539);
        var pairs = deck.GroupBy(s => s.Match).SelectMany(g => g.OrderBy(_ => random.Next()).Chunk(2)).OrderBy(_ => random.Next()).ToArray();
        Dictionary<int, Symbol> tiles = [];
        for (int i = 0; i < sequence.Count; i++)
        {
            tiles[sequence[i].A] = pairs[i][0]; tiles[sequence[i].B] = pairs[i][1];
        }
        return (tiles, sequence);
    }
    public static GameState New(Layout layout, string set, int seed)
    {
        Rules.Validate(layout);
        var assigned = Assign(layout.Positions, Catalog.Deck(set, layout.Count), seed);
        // Deterministic, verified fallback; never label a failed construction as solvable.
        assigned ??= Assign(layout.Positions, Catalog.Deck(set, layout.Count), 0);
        if (assigned == null) throw new InvalidOperationException("Generation failed within the search budget.");
        return new() { LayoutId = layout.Id, SetId = set, Seed = seed, Positions = layout.Positions.ToList(), Tiles = assigned.Value.Tiles,
            Initial = new(assigned.Value.Tiles), Solution = assigned.Value.Solution, InitialSolution = assigned.Value.Solution.ToList() };
    }
    public static bool Shuffle(GameState state, int seed, int maxNodes = 100000)
    {
        if (state.ShuffleUsed || state.Tiles.Count == 0) return false;
        var active = Rules.Active(state);
        // A failed construction is transactional: no field of state has been changed.
        if (maxNodes == 0) return false;
        var result = Assign(active, state.Tiles.Values, seed);
        if (result == null) return false;
        state.Tiles = result.Value.Tiles;
        state.Solution = result.Value.Solution;
        state.ShuffleUsed = true;
        return true;
    }
    public static bool Replay(GameState state, IEnumerable<Pair> solution)
    {
        var copy = new GameState { Positions = state.Positions, Tiles = new(state.Tiles) };
        return solution.All(pair => Rules.Remove(copy, pair.A, pair.B)) && copy.Tiles.Count == 0;
    }
}

public sealed class Preferences
{
    public string Language { get; set; } = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pl" ? "pl" : "en";
    public string Theme { get; set; } = "dark";
    public string Background { get; set; } = "felt";
    public string Set { get; set; } = "geometry";
    public string Layout { get; set; } = "turtle";
    public bool Muted { get; set; }
    public double Volume { get; set; } = .35;
    public bool ReducedMotion { get; set; }
    public bool Labels { get; set; } = true;
}

public static class Storage
{
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    public static void Atomic<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temp = path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            JsonSerializer.Serialize(stream, value, Json); stream.Flush(true);
        }
        File.Move(temp, path, true);
    }
    public static bool Load(string path, IReadOnlyList<Layout> layouts, out GameState? state)
    {
        state = null;
        try
        {
            var s = JsonSerializer.Deserialize<GameState>(File.ReadAllText(path), Json);
            if (s == null || s.Version != 1 || s.GameId == Guid.Empty || !Catalog.Sets.Contains(s.SetId) || !double.IsFinite(s.Seconds) || s.Seconds < 0 || s.Hints < 0) return false;
            var layout = layouts.SingleOrDefault(l => l.Id == s.LayoutId);
            if (layout == null || !s.Positions.SequenceEqual(layout.Positions)) return false;
            Rules.Validate(new(s.LayoutId, s.Positions.Count, s.Positions));
            if (s.Initial.Count != layout.Count || s.Initial.Keys.Any(id => !layout.Positions.Any(p => p.Id == id))) return false;
            var deck = Catalog.Deck(s.SetId, layout.Count).GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.Count());
            var initial = s.Initial.Values.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.Count());
            if (deck.Count != initial.Count || deck.Any(g => !initial.TryGetValue(g.Key, out int count) || g.Value != count)) return false;
            var allowed = Catalog.Deck(s.SetId, layout.Count).Distinct().ToHashSet();
            if (s.Initial.Values.Any(x => !allowed.Contains(x)) || s.Tiles.Values.Any(x => !allowed.Contains(x))) return false;
            if (s.Tiles.Keys.Any(id => !s.Initial.ContainsKey(id)) || s.Tiles.Count % 2 != 0 || s.Tiles.Values.GroupBy(x => x.Match).Any(g => g.Count() % 2 != 0)) return false;
            if (s.Tiles.Values.GroupBy(x => x.Id).Any(g => !deck.TryGetValue(g.Key, out int count) || g.Count() > count)) return false;
            if (!s.ShuffleUsed && s.Tiles.Any(p => s.Initial[p.Key] != p.Value)) return false;
            if (!Generator.Replay(new GameState { Positions = s.Positions, Tiles = new(s.Initial) }, s.InitialSolution)) return false;
            state = s;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or ArgumentException or NullReferenceException or InvalidDataException) { return false; }
    }
}

public sealed class Localizer
{
    public Dictionary<string, string> Polish { get; }
    public Dictionary<string, string> English { get; }
    public string Language { get; set; }
    public Localizer(string directory, string language)
    {
        Polish = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(directory, "pl.json")))!;
        English = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(directory, "en.json")))!;
        if (!Polish.Keys.Order().SequenceEqual(English.Keys.Order())) throw new InvalidDataException("Translation keys differ.");
        Language = language;
    }
    public string this[string key] => (Language == "pl" ? Polish : English).GetValueOrDefault(key, key);
    public string Number(int n, string kind)
    {
        if (Language != "pl") return $"{n} {this[kind + (n == 1 ? "One" : "Many")]}";
        string suffix = n == 1 ? "One" : n % 10 is >= 2 and <= 4 && n % 100 is not (>= 12 and <= 14) ? "Few" : "Many";
        return $"{n} {this[kind + suffix]}";
    }
}
