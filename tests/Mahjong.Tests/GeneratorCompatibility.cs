using System.Diagnostics;
using Mahjong.Core;

internal static class GeneratorCompatibility
{
    // Reference algorithm from 1.2.2. Compare complete paths, including half-offset layouts,
    // so optimization cannot silently change existing seeds and daily challenges.
    static List<Pair>? Reference(List<Position> positions, int seed)
    {
        uint state = unchecked((uint)seed) ^ 0xA341316Cu;
        int Next() { state += 0x9E3779B9u; uint z = state; z = (z ^ (z >> 16)) * 0x21F0AAADu; z = (z ^ (z >> 15)) * 0x735A2D97u; return (int)((z ^ (z >> 15)) & 0x7FFFFFFF); }
        List<Pair> path = []; HashSet<string> dead = []; int nodes = 0;
        bool Search(List<Position> active)
        {
            if (active.Count == 0) return true;
            if (++nodes > 100000) return false;
            string key = string.Join(',', active.Select(p => p.Id));
            if (dead.Contains(key)) return false;
            var free = active.Where(p => Rules.Free(p, active)).OrderBy(_ => Next()).ToArray();
            for (int i = 0; i < free.Length; i++) for (int j = i + 1; j < free.Length; j++)
            {
                path.Add(new(free[i].Id, free[j].Id));
                if (Search(active.Where(p => p.Id != free[i].Id && p.Id != free[j].Id).ToList())) return true;
                path.RemoveAt(path.Count - 1); if (nodes > 100000) return false;
            }
            dead.Add(key); return false;
        }
        return Search(positions.OrderBy(p => p.Id).ToList()) ? path : null;
    }
    public static void Verify(List<Layout> layouts)
    {
        long originalTicks = 0, indexedTicks = 0;
        foreach (var layout in layouts) foreach (int seed in new[] { 0, 1, -127, 20261115, int.MaxValue })
        {
            var timer = Stopwatch.StartNew(); var original = Reference(layout.Positions, seed); originalTicks += timer.ElapsedTicks;
            timer.Restart(); var indexed = Generator.Sequence(layout.Positions, seed, milliseconds: int.MaxValue); indexedTicks += timer.ElapsedTicks;
            if (original == null || indexed == null || !original.SequenceEqual(indexed)) throw new Exception("Seed changed: " + layout.Id + "/" + seed);
        }
        Console.WriteLine($"BENCHMARK: 40 paths; reference {originalTicks * 1000.0 / Stopwatch.Frequency:F1} ms, indexed {indexedTicks * 1000.0 / Stopwatch.Frequency:F1} ms (same machine; indicative).");
    }
}
