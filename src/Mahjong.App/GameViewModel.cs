using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Media;
using Mahjong.Core;

namespace Mahjong.App;

public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => execute();
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
}

public sealed class GameViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? BoardChanged;
    public event Action? ScreenChanged;
    public string DataDirectory { get; }
    public string AssetDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "Assets");
    public string SavePath => Path.Combine(DataDirectory, "game.json");
    public string ResultsPath => Path.Combine(DataDirectory, "results.json");
    public List<GameResult> Results { get; private set; } = [];
    public CustomBackground CustomBackground { get; }
    public bool ResultSaved => Game != null && Results.Any(r => r.GameId == Game.GameId);
    public bool CanSaveResult => Game?.Tiles.Count == 0 && !ResultSaved;
    public Preferences Preferences { get; }
    public Localizer Text { get; }
    public List<Layout> Layouts { get; }
    public GameState? Game { get; private set; }
    public string Screen { get; private set; } = "menu";
    public int? Selected { get; private set; }
    public Pair? Highlight { get; private set; }
    public bool Paused { get; private set; }
    public bool Busy { get; private set; }
    public bool HasSave => Game != null;
    public bool Playing => Screen == "game" && Game != null && !Paused && !Busy && Game.Tiles.Count > 0;
    public string Status { get; private set; } = "";
    string statusKey = "ready";
    public string Clock => TimeSpan.FromSeconds(Game?.Seconds ?? 0).ToString(@"hh\:mm\:ss");
    public string Remaining => Text.Number(Game?.Tiles.Count ?? 0, "tile");
    public string PairCount => Text.Number(Game == null ? 0 : Rules.Pairs(Game).Count, "pair");
    public string HintCount => (Game?.Hints ?? 0).ToString();
    public string ShuffleLabel => $"{Text["shuffle"]} ({(Game?.ShuffleUsed == true ? 0 : 1)})";
    public string HintLabel => Text["hint"] + "   H";
    public string PauseLabel => Text[Paused ? "resume" : "pause"];
    public bool CanShuffle => Playing && Game?.ShuffleUsed == false;
    int hintIndex;
    readonly Stopwatch clock = Stopwatch.StartNew();
    long lastTick;
    readonly MediaPlayer player = new();
    public ICommand HintCommand { get; }
    public ICommand ShuffleCommand { get; }
    public ICommand PauseCommand { get; }
    public GameViewModel(string? dataDirectory = null)
    {
        DataDirectory = dataDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuietMahjong");
        CustomBackground = new(DataDirectory);
        Layouts = Catalog.LoadLayouts(Path.Combine(AssetDirectory, "layouts.json"));
        try { Preferences = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(Path.Combine(DataDirectory, "settings.json")), Storage.Json) ?? new(); }
        catch { Preferences = new(); }
        if (Preferences.Language is not ("pl" or "en")) Preferences.Language = "en";
        if (Preferences.Theme is not ("dark" or "light" or "contrast")) Preferences.Theme = "dark";
        if (Preferences.Background is not ("felt" or "wood" or "forest" or "paper" or "minimal" or "custom")) Preferences.Background = "felt";
        if (!Catalog.Sets.Contains(Preferences.Set)) Preferences.Set = "geometry";
        if (!Layouts.Any(l => l.Id == Preferences.Layout)) Preferences.Layout = "turtle";
        Preferences.Volume = double.IsFinite(Preferences.Volume) ? Math.Clamp(Preferences.Volume, 0, 1) : .35;
        Text = new(AssetDirectory, Preferences.Language);
        if (!CustomBackground.Load() && Preferences.Background == "custom")
        {
            Preferences.Background = "felt"; SetStatus("customBackgroundMissing");
        }
        LoadResults();
        if (File.Exists(SavePath))
        {
            if (Storage.Load(SavePath, Layouts, out var saved)) Game = saved;
            else SetStatus("loadError");
        }
        HintCommand = new RelayCommand(Hint, () => Playing);
        ShuffleCommand = new RelayCommand(async () => await Shuffle(), () => CanShuffle);
        PauseCommand = new RelayCommand(TogglePause, () => Screen == "game" && Game != null && !Busy);
        player.MediaFailed += (_, _) => { };
    }
    public void Notify([CallerMemberName] string? property = null) => PropertyChanged?.Invoke(this, new(property));
    public void Refresh()
    {
        foreach (var key in new[] { nameof(Clock), nameof(Remaining), nameof(PairCount), nameof(HintCount), nameof(ShuffleLabel), nameof(HintLabel), nameof(PauseLabel), nameof(Status), nameof(HasSave), nameof(CanShuffle) }) Notify(key);
        CommandManager.InvalidateRequerySuggested();
    }
    public void Tick()
    {
        long now = clock.ElapsedMilliseconds;
        if (Playing) Game!.Seconds += (now - lastTick) / 1000.0;
        lastTick = now;
        Notify(nameof(Clock));
    }
    public void SetStatus(string key) { statusKey = key; Status = Text[key]; Notify(nameof(Status)); }
    public void Navigate(string screen)
    {
        Tick();
        if (Screen == "game") Save();
        Screen = screen;
        if (screen == "game") { Paused = false; lastTick = clock.ElapsedMilliseconds; }
        ScreenChanged?.Invoke(); Refresh();
        if (screen == "game") BoardChanged?.Invoke();
    }
    public async Task NewGame(bool replay = false)
    {
        if (Busy) return;
        Busy = true; SetStatus("working"); Refresh();
        try
        {
            GameState next;
            if (replay && Game != null)
            {
                var old = Game;
                next = new() { LayoutId = old.LayoutId, SetId = old.SetId, Seed = old.Seed, Positions = old.Positions.ToList(),
                    Initial = new(old.Initial), Tiles = new(old.Initial), InitialSolution = old.InitialSolution.ToList() };
                next.Solution = next.InitialSolution.ToList();
            }
            else
            {
                var layout = Layouts.Single(l => l.Id == Preferences.Layout);
                var set = Preferences.Set;
                int seed = Random.Shared.Next();
                next = await Task.Run(() => Generator.New(layout, set, seed));
            }
            Game = next; Selected = null; Highlight = null; hintIndex = 0; Paused = false;
            SetStatus("ready"); Save(); Navigate("game");
        }
        catch { SetStatus("generationError"); }
        finally { Busy = false; lastTick = clock.ElapsedMilliseconds; Refresh(); BoardChanged?.Invoke(); }
    }
    public void Choose(int id)
    {
        if (!Playing || !Game!.Tiles.ContainsKey(id)) return;
        Tick();
        var active = Rules.Active(Game);
        if (!Rules.Free(active.Single(p => p.Id == id), active)) { SetStatus("blocked"); Play("blocked"); return; }
        Highlight = null;
        if (Selected == id) Selected = null;
        else if (Selected is int first && Rules.Remove(Game, first, id))
        {
            Selected = null; hintIndex = 0; Play("remove"); CheckState(); Save();
        }
        else { Selected = id; SetStatus("ready"); Play("select"); }
        Refresh(); BoardChanged?.Invoke();
    }
    void CheckState()
    {
        if (Game!.Tiles.Count == 0) { SetStatus("won"); Play("win"); }
        else if (Rules.Pairs(Game).Count == 0) { SetStatus(Game.ShuffleUsed ? "noMovesEnd" : "noMovesShuffle"); Play("stuck"); }
        else SetStatus("ready");
    }
    public void Hint()
    {
        if (!Playing) return;
        var pairs = Rules.Pairs(Game!);
        if (pairs.Count == 0) { CheckState(); return; }
        Selected = null; Highlight = pairs[hintIndex++ % pairs.Count]; Game!.Hints++;
        SetStatus("hintStatus"); Play("hint"); Save(); Refresh(); BoardChanged?.Invoke();
    }
    public async Task Shuffle()
    {
        if (!CanShuffle) return;
        Tick(); Busy = true; SetStatus("working"); Refresh();
        try
        {
            // Work on a private snapshot: UI never observes a partially changed board.
            var copy = JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(Game, Storage.Json), Storage.Json)!;
            bool success = await Task.Run(() => Generator.Shuffle(copy, Random.Shared.Next()));
            if (success) { Game = copy; Selected = null; Highlight = null; hintIndex = 0; SetStatus("shuffleDone"); Play("shuffle"); Save(); }
            else SetStatus("shuffleFailed");
        }
        catch { SetStatus("shuffleFailed"); }
        finally { Busy = false; lastTick = clock.ElapsedMilliseconds; Refresh(); BoardChanged?.Invoke(); }
    }
    public void TogglePause()
    {
        if (Busy || Screen != "game") return;
        Tick(); Paused = !Paused; lastTick = clock.ElapsedMilliseconds;
        Save(); Refresh(); BoardChanged?.Invoke();
    }
    public void ChangeLanguage(string language)
    {
        Preferences.Language = language; Text.Language = language;
        Status = Text[statusKey]; SavePreferences(); Refresh(); ScreenChanged?.Invoke(); BoardChanged?.Invoke();
    }
    public void Save()
    {
        if (Game == null) return;
        try { Storage.Atomic(SavePath, Game); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { SetStatus("saveError"); }
    }
    public void SavePreferences()
    {
        try { Storage.Atomic(Path.Combine(DataDirectory, "settings.json"), Preferences); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { SetStatus("saveError"); }
    }
    public void LoadResults()
    {
        try { Results = ResultStore.Load(ResultsPath); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        { SetStatus("resultsLoadError"); }
    }
    public bool SaveResult(string player)
    {
        if (!CanSaveResult) return false;
        try
        {
            ResultStore.SaveFinished(ResultsPath, Game!, player);
            LoadResults(); SetStatus("resultSaved"); Refresh(); return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or InvalidDataException)
        { SetStatus("resultSaveError"); return false; }
    }
    public async Task<bool> ImportBackground(string path)
    {
        if (Busy) return false;
        Tick(); Busy = true; Refresh();
        try
        {
            await Task.Run(() => CustomBackground.Import(path));
            Preferences.Background = "custom"; SavePreferences(); SetStatus("customBackgroundLoaded");
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or InvalidOperationException or System.Runtime.InteropServices.COMException or FileFormatException or InvalidDataException)
        { SetStatus("customBackgroundError"); return false; }
        finally { Busy = false; lastTick = clock.ElapsedMilliseconds; Refresh(); }
    }
    public void Play(string name)
    {
        if (Preferences.Muted || Preferences.Volume <= 0) return;
        try { player.Stop(); player.Open(new Uri(Path.Combine(AssetDirectory, "audio", name + ".wav"))); player.Volume = Preferences.Volume; player.Play(); }
        catch { /* Unavailable audio must not affect play. */ }
    }
    public void Close() { Tick(); Save(); SavePreferences(); player.Close(); }
}
