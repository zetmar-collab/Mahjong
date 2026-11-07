namespace Mahjong.Core;

public static class ResultQuery
{
    public static bool Assisted(GameResult r) => r.Hints > 0 || r.ShuffleUsed || r.UndoCount > 0;
    public static IEnumerable<GameResult> Filter(IEnumerable<GameResult> results, string layout = "all", string set = "all", string help = "all", string order = "newest", bool daily = false)
    {
        var filtered = results.Where(r => (layout == "all" || r.LayoutId == layout) && (set == "all" || r.SetId == set)
            && (help == "all" || Assisted(r) == (help == "assisted")) && (!daily || r.ChallengeDate != null));
        return order == "fastest" ? filtered.OrderBy(r => r.Seconds).ThenByDescending(r => r.FinishedAt) : filtered.OrderByDescending(r => r.FinishedAt);
    }
}
