using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Mahjong.Core;
using Microsoft.Win32;

namespace Mahjong.App;

public partial class MainWindow
{
    string resultLayout = "all", resultSet = "all", resultHelp = "all", resultOrder = "newest";
    bool dailyOnly;
    double scrollX, scrollY;
    Guid? viewportGame;

    void SetZoom(double value)
    {
        Vm.Preferences.Zoom = Math.Clamp(value, 1, 3); Vm.SavePreferences(); DrawBoard();
    }
    void AddBoardViewport(Canvas canvas)
    {
        if (viewportGame != Vm.Game!.GameId) { viewportGame = Vm.Game.GameId; scrollX = scrollY = 0; }
        if (Vm.Preferences.Zoom == 1)
        {
            boardHost!.Children.Add(new Viewbox { Child = canvas, Margin = new(22, 15, 22, 15), Stretch = Stretch.Uniform });
            return;
        }
        var view = new Viewbox { Child = canvas, Stretch = Stretch.Uniform };
        var scroll = new ScrollViewer { Content = view, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        void Resize()
        {
            view.Width = Math.Max(1, boardHost!.ActualWidth - 36) * Vm.Preferences.Zoom;
            view.Height = Math.Max(1, boardHost.ActualHeight - 36) * Vm.Preferences.Zoom;
        }
        scroll.SizeChanged += (_, _) => Resize();
        scroll.Loaded += (_, _) => { Resize(); scroll.ScrollToHorizontalOffset(scrollX); scroll.ScrollToVerticalOffset(scrollY); };
        scroll.ScrollChanged += (_, e) => { if (e.HorizontalChange != 0) scrollX = scroll.HorizontalOffset; if (e.VerticalChange != 0) scrollY = scroll.VerticalOffset; };
        boardHost!.Children.Add(scroll);
        if (scrollX == 0 && scrollY == 0) scroll.Loaded += (_, _) => { scroll.ScrollToHorizontalOffset(scroll.ScrollableWidth / 2); scroll.ScrollToVerticalOffset(scroll.ScrollableHeight / 2); };
    }
    void ShowDealCode()
    {
        if (Vm.Game == null) return;
        var dialog = new Window { Owner = this, Title = T("dealCode"), Width = 650, SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = R("Surface"), ResizeMode = ResizeMode.NoResize };
        var panel = new StackPanel { Margin = new(24) };
        panel.Children.Add(Text(T("shareHelp")));
        var input = new TextBox { Text = DealCode.Encode(Vm.Game), IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Margin = new(0, 16, 0, 16) };
        AutomationProperties.SetName(input, T("dealCode"));panel.Children.Add(input);
        panel.Children.Add(Button(T("copyCode"), () =>
        {
            try { Clipboard.SetText(input.Text); input.SelectAll(); }
            catch (System.Runtime.InteropServices.COMException) { input.Focus(); input.SelectAll(); }
        }, true));
        var close = Button(T("back"), () => dialog.Close()); close.IsCancel = true; panel.Children.Add(close);
        dialog.Content = panel; dialog.ShowDialog();
    }
    void ExportData()
    {
        if (Vm.Busy) return;
        var dialog = new SaveFileDialog { Title = T("exportData"), Filter = "Mahjong backup (*.mahjongbackup)|*.mahjongbackup", FileName = "Mahjong-" + DateTime.Now.ToString("yyyy-MM-dd") + ".mahjongbackup" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            Vm.Tick();
            Vm.FlushSaves();
            if (Vm.Game != null) Storage.Atomic(Vm.SavePath, Vm.Game);
            Storage.Atomic(Path.Combine(Vm.DataDirectory, "settings.json"), Vm.Preferences);
            BackupService.Export(Vm.DataDirectory, dialog.FileName); Vm.SetStatus("exported");
        }
        catch (Exception ex) when (DataError(ex)) { Vm.SetStatus("backupError"); }
    }
    void ImportData()
    {
        if (Vm.Busy) return;
        var dialog = new OpenFileDialog { Title = T("importData"), Filter = "Mahjong backup (*.mahjongbackup)|*.mahjongbackup", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        if (MessageBox.Show(this, T("importConfirm"), T("importData"), MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
        try
        {
            Vm.Tick();
            Vm.FlushSaves();
            if (Vm.Game != null) Storage.Atomic(Vm.SavePath, Vm.Game);
            Storage.Atomic(Path.Combine(Vm.DataDirectory, "settings.json"), Vm.Preferences);
            BackupService.Import(dialog.FileName, Vm.DataDirectory, Vm.Layouts, path =>
            {
                var candidate = new CustomBackground(Path.GetDirectoryName(path)!);
                if (!candidate.Load()) throw new InvalidDataException("Invalid background.");
            });
            var previous = Vm;
            var next = new GameViewModel(previous.DataDirectory);
            previous.BoardChanged -= DrawBoard; previous.ScreenChanged -= RenderScreen; previous.Close(false);
            Vm = next; DataContext = Vm; Vm.BoardChanged += DrawBoard; Vm.ScreenChanged += RenderScreen;
            Vm.SetStatus("imported"); ApplyTheme(); RenderScreen();
        }
        catch (Exception ex) when (DataError(ex)) { Vm.SetStatus("backupError"); }
    }
    static bool DataError(Exception ex) => ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or InvalidDataException or ArgumentException or System.Runtime.InteropServices.COMException;

    sealed record ResultRow(string Summary, string Details, string Metadata);
    void RenderResults()
    {
        var root = new Grid { Margin = new(24, 4, 24, 12) };
        root.RowDefinitions.Add(new() { Height = GridLength.Auto }); root.RowDefinitions.Add(new()); root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var header = new StackPanel(); header.Children.Add(TitleText(T("results")));
        var filters = new WrapPanel();
        void AddFilter(string label,ComboBox combo) {var field=Field(T(label),combo);field.Width=176;field.Margin=new(0,0,12,0);filters.Children.Add(field);}
        AddFilter("layoutFilter",Combo(["all", .. Vm.Layouts.Select(l => l.Id)], resultLayout, id => { resultLayout = id; RenderScreen(); }));
        AddFilter("tileSet",Combo(["all", .. Catalog.Sets], resultSet, id => { resultSet = id; RenderScreen(); }));
        AddFilter("helpFilter",Combo(["all", "unassisted", "assisted"], resultHelp, id => { resultHelp = id; RenderScreen(); }));
        AddFilter("sortFilter",Combo(["newest", "fastest"], resultOrder, id => { resultOrder = id; RenderScreen(); }));
        var daily = new CheckBox { Content = T("dailyOnly"), IsChecked = dailyOnly, Margin = new(12), Foreground = R("Text") };
        daily.Checked += (_, _) => { dailyOnly = true; RenderScreen(); }; daily.Unchecked += (_, _) => { dailyOnly = false; RenderScreen(); }; filters.Children.Add(daily);
        header.Children.Add(filters);
        var rows = ResultQuery.Filter(Vm.Results, resultLayout, resultSet, resultHelp, resultOrder, dailyOnly).ToList();
        // Records are compared only within the same layout, set and help category.
        var records = rows.GroupBy(r => (r.LayoutId, r.SetId, Help: ResultQuery.Assisted(r)))
            .ToDictionary(g => g.Key, g => g.Min(r => r.Seconds));
        header.Children.Add(Text(rows.Count == 0 ? T("noResults") : T("resultsHelp"), 13, "Muted")); root.Children.Add(header);
        var culture = CultureInfo.GetCultureInfo(Vm.Text.Language == "pl" ? "pl-PL" : "en-GB");
        var list = new ListBox { Background = Brushes.Transparent, BorderThickness = new(0), Foreground = R("Text"), HorizontalContentAlignment = HorizontalAlignment.Stretch };
        ScrollViewer.SetCanContentScroll(list, true); ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
        VirtualizingPanel.SetIsVirtualizing(list, true); VirtualizingPanel.SetVirtualizationMode(list, VirtualizationMode.Recycling);
        var template = new DataTemplate(typeof(ResultRow));
        var stack = new FrameworkElementFactory(typeof(StackPanel));
        foreach (var (property, size) in new[] { ("Summary", 20d), ("Details", 14d), ("Metadata", 13d) })
        {
            var text = new FrameworkElementFactory(typeof(TextBlock)); text.SetBinding(TextBlock.TextProperty, new Binding(property));
            text.SetValue(TextBlock.FontSizeProperty, size); text.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap); stack.AppendChild(text);
            text.SetValue(MarginProperty,new Thickness(0,2,0,2));
            text.SetResourceReference(TextBlock.ForegroundProperty,property=="Metadata"?"Muted":"Text");
        }
        template.VisualTree = stack; list.ItemTemplate = template;
        list.ItemsSource = rows.Select(r => new ResultRow(
            (string.IsNullOrWhiteSpace(r.Player) ? T("playerDefault") : r.Player) + " · " + GameTime.Format(r.Seconds)
                + (r.Seconds == records[(r.LayoutId, r.SetId, ResultQuery.Assisted(r))] ? " · " + T("record") : ""),
            T(r.LayoutId) + " / " + T(r.SetId) + " · " + T(ResultQuery.Assisted(r) ? "assisted" : "unassisted"),
            r.FinishedAt.ToLocalTime().ToString("g", culture) + " · " + T("hints") + ": " + r.Hints + " · " + T("undo") + ": " + r.UndoCount
                + " · " + T("shuffleUsed") + ": " + T(r.ShuffleUsed ? "yes" : "no") + (r.ChallengeDate == null ? "" : " · " + T("daily") + " " + r.ChallengeDate))).ToArray();
        Grid.SetRow(list, 1); root.Children.Add(list);
        var footer = new StackPanel(); var status = Text("", 13, "Muted"); Bind(status, TextBlock.TextProperty, nameof(Vm.Status)); footer.Children.Add(status);
        var back=Button(T("back"), () => Vm.Navigate(Vm.Game?.Tiles.Count == 0 ? "game" : "menu"));back.HorizontalAlignment=HorizontalAlignment.Right;footer.Children.Add(back); Grid.SetRow(footer, 2); root.Children.Add(footer); ContentArea.Children.Add(root);
    }

    int tutorialStep;
    GameState? tutorialGame;
    int? tutorialSelected;
    string tutorialMessage = "";
    void RenderTutorial()
    {
        var panel = Page(T("tutorial"), T("tutorialIntro"));
        panel.Children.Add(TitleText($"{tutorialStep + 1}/3 · {T("lesson" + tutorialStep)}", 25));
        panel.Children.Add(Text(T("lessonText" + tutorialStep), 18));
        tutorialGame ??= CreateLesson(tutorialStep);
        var canvas = new Canvas { Width = 480, Height = 230, Margin = new(0, 20, 0, 12) };
        var active = Rules.Active(tutorialGame);
        foreach (var p in active.OrderBy(p => p.Z))
        {
            var b = Button("", () =>
            {
                if (!Rules.Free(p, Rules.Active(tutorialGame))) tutorialMessage = "blocked";
                else if (tutorialSelected is int a && a != p.Id && Rules.Remove(tutorialGame, a, p.Id))
                { tutorialSelected = null; tutorialMessage = tutorialGame.Tiles.Count == 0 ? "lessonDone" : "ready"; }
                else { tutorialSelected = p.Id; tutorialMessage = "lessonSelect"; }
                RenderScreen();
            });
            b.Content = new TileArt { Symbol = tutorialGame.Tiles[p.Id], Blocked = !Rules.Free(p, active), Selected = tutorialSelected == p.Id };
            b.Padding = new(0); b.Margin = new(0); AutomationProperties.SetName(b, SymbolName(tutorialGame.Tiles[p.Id]));
            Canvas.SetLeft(b, p.X * 38 + p.Z * 6); Canvas.SetTop(b, 80 - p.Z * 55); canvas.Children.Add(b);
        }
        panel.Children.Add(canvas); panel.Children.Add(Text(T(tutorialMessage == "" ? "lessonSelect" : tutorialMessage), 16, "Accent"));
        var next = Button(T(tutorialStep == 2 ? "finishTutorial" : "nextLesson"), () =>
        {
            if (tutorialStep == 2) Vm.Navigate("help");
            else { tutorialStep++; tutorialGame = null; tutorialSelected = null; tutorialMessage = ""; RenderScreen(); }
        }, true);
        next.IsEnabled = tutorialGame.Tiles.Count == 0; panel.Children.Add(next);
        panel.Children.Add(Button(T("back"), () => { tutorialGame = null; tutorialSelected = null; tutorialMessage = ""; Vm.Navigate("help"); }));
    }
    internal static GameState CreateLesson(int step)
    {
        var a = new Symbol("geometry1", "geometry1", "geometry", 1); var b = new Symbol("geometry2", "geometry2", "geometry", 2);
        if (step == 0) return new() { Positions = [new(0, 0, 0, 0), new(1, 2, 0, 0), new(2, 4, 0, 0), new(3, 8, 0, 0)], Tiles = new() { [0] = a, [1] = b, [2] = a, [3] = b } };
        if (step == 1) return new() { Positions = [new(0, 0, 0, 0), new(1, 2, 0, 0), new(2, 0, 0, 1), new(3, 2, 0, 1)], Tiles = new() { [0] = a, [1] = a, [2] = b, [3] = b } };
        return new() { Positions = [new(0, 0, 0, 0), new(1, 3, 0, 0), new(2, 6, 0, 0), new(3, 9, 0, 0)], Tiles = new()
        { [0] = new("flower1", "flower", "flower", 1), [1] = new("season1", "season", "season", 1), [2] = new("flower4", "flower", "flower", 4), [3] = new("season4", "season", "season", 4) } };
    }
}
