namespace Mahjong.Core;

public sealed record FamilyPlayer(string Name, int Pairs = 0)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public int Points => Pairs * 10;
}

/// <summary>One shared board, one successful pair per turn. Passing never awards points.</summary>
public sealed class FamilyMatch
{
    public List<FamilyPlayer> Players { get; set; } = [];
    public int Turn { get; set; }
    public static FamilyMatch Create(IEnumerable<string> names)
    {
        var match = new FamilyMatch { Players = names.Select(n => new FamilyPlayer(n.Trim())).ToList() };
        if (!match.Valid(0)) throw new ArgumentException("Use two to four distinct names of 1–24 characters.");
        return match;
    }
    public bool Valid(int removedPairs) => Players is { Count: >= 2 and <= 4 } && Turn >= 0 && Turn < Players.Count
        && Players.All(p => p != null && ValidName(p.Name) && p.Pairs is >= 0 and <= 72)
        && Players.Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Players.Count
        && Players.Sum(p => p.Pairs) == removedPairs;
    public static bool ValidName(string? name) => !string.IsNullOrWhiteSpace(name) && name == name.Trim()
        && name.Length <= 24 && !name.Any(char.IsControl);
    public void AwardPair()
    {
        Players[Turn] = Players[Turn] with { Pairs = Players[Turn].Pairs + 1 };
        Pass();
    }
    public void Pass() => Turn = (Turn + 1) % Players.Count;
    public FamilyMatch Restart() => Create(Players.Select(p => p.Name));
    [System.Text.Json.Serialization.JsonIgnore]
    public IEnumerable<FamilyPlayer> Winners => Players.Where(p => p.Pairs == Players.Max(x => x.Pairs));
}
