using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Mahjong.Core;
using Microsoft.Win32;
using System.Globalization;

namespace Mahjong.App;

public partial class MainWindow : Window
{
    internal readonly GameViewModel Vm;
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    readonly Dictionary<int, Button> tileButtons = [];
    Grid? boardHost;
    string settingsReturn = "menu";
    bool confirming;
    Guid? celebratedGame;
    string T(string key) => Vm.Text[key];
    Brush R(string key) => (Brush)FindResource(key);
    Brush BoardBackground() => Vm.Preferences.Background == "custom" ? Vm.CustomBackground.Brush ?? BackgroundArt.Create("felt") : BackgroundArt.Create(Vm.Preferences.Background);
    public MainWindow()
    {
        InitializeComponent();
        string? smokePath = Environment.GetCommandLineArgs().Contains("--smoke") ? Path.Combine(AppContext.BaseDirectory, "SmokeData") : null;
        if(smokePath != null) { ShowActivated=false; ShowInTaskbar=false; WindowStartupLocation=WindowStartupLocation.Manual; Left=-10000; Top=-10000; }
        Vm = new(smokePath); DataContext = Vm;
        Vm.BoardChanged += DrawBoard;
        Vm.ScreenChanged += RenderScreen;
        timer.Tick += (_, _) => Vm.Tick(); timer.Start();
        ApplyTheme(); RenderScreen();
        Loaded += async (_, _) => { if (smokePath != null) await Smoke.Run(this, Vm); };
    }
    TextBlock Text(string value, double size = 16, string brush = "Text") => new() { Text = value, FontSize = size, Foreground = R(brush), TextWrapping = TextWrapping.Wrap };
    TextBlock TitleText(string value, double size = 34) => new() { Text = value, FontSize = size, FontFamily = new("Georgia"), Foreground = R("Text"), Margin = new(0, 8, 0, 12) };
    Button Button(string label, Action action, bool primary = false)
    {
        var b = new Button { Content = label, HorizontalContentAlignment = HorizontalAlignment.Center };
        AutomationProperties.SetName(b, label);
        if (primary) { b.Background = R("Accent"); b.Foreground = new SolidColorBrush(Color.FromRgb(24, 46, 38)); b.FontWeight = FontWeights.SemiBold; }
        b.Click += (_, _) => action(); return b;
    }
    static void Bind(DependencyObject obj, DependencyProperty property, string path) => BindingOperations.SetBinding(obj, property, new Binding(path));
    internal void RefreshAppearance() { ApplyTheme(); RenderScreen(); }
    void ApplyTheme()
    {
        string[] values = Vm.Preferences.Theme switch
        {
            "light" => ["#F1EFE6", "#FFFFFF", "#203B33", "#57685E", "#C5CEC2", "#C9A75E"],
            "contrast" => ["#000000", "#101010", "#FFFFFF", "#FFFFFF", "#FFFFFF", "#FFE15B"],
            _ => ["#152D2B", "#1D3835", "#F5F1E5", "#AABEB4", "#36514A", "#EBCB88"]
        };
        string[] keys = ["Surface", "Panel", "Text", "Muted", "Line", "Accent"];
        for(int i=0;i<keys.Length;i++) Application.Current.Resources[keys[i]] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(values[i]));
    }
    void RenderScreen()
    {
        SettingsButton.Content = T("settings"); Footer.Text = T("footer");
        AutomationProperties.SetName(FullscreenButton, T("fullscreen"));
        AutomationProperties.SetName(SettingsButton, T("settings"));
        LanguageButton.Content = Vm.Preferences.Language == "pl" ? "PL / EN" : "EN / PL";
        ContentArea.Children.Clear(); boardHost = null; tileButtons.Clear();
        switch(Vm.Screen)
        {
            case "game": RenderGame(); break;
            case "new": RenderNew(); break;
            case "settings": RenderSettings(); break;
            case "help": RenderHelp(); break;
            case "results": RenderResults(); break;
            default: RenderMenu(); break;
        }
    }
    void RenderMenu()
    {
        var g = new Grid(); g.ColumnDefinitions.Add(new(){Width=new GridLength(5,GridUnitType.Star)}); g.ColumnDefinitions.Add(new(){Width=new GridLength(6,GridUnitType.Star)});
        var left = new StackPanel { VerticalAlignment=VerticalAlignment.Center, Margin=new(40,0,45,0), MaxWidth=410 };
        left.Children.Add(Text(T("solitaire"),12,"Accent")); left.Children.Add(TitleText(T("subtitle"),49));
        left.Children.Add(new Border { Height=20 });
        left.Children.Add(Button(T("newGame"),()=>Vm.Navigate("new"),true));
        var resume=Button(T("continue"),()=>Vm.Navigate("game")); resume.IsEnabled=Vm.HasSave; left.Children.Add(resume);
        left.Children.Add(Button(T("settings"),()=>OpenSettings()));
        left.Children.Add(Button(T("help"),()=>Vm.Navigate("help")));
        left.Children.Add(Button(T("results"),()=>{Vm.LoadResults();Vm.Navigate("results");}));
        left.Children.Add(Button(T("exit"),Close));
        var message=Text("",13,"Muted"); Bind(message,TextBlock.TextProperty,nameof(Vm.Status)); message.Margin=new(5,18,0,0); left.Children.Add(message);
        g.Children.Add(left);
        var art = new Grid { Margin=new(15,28,15,28) }; Grid.SetColumn(art,1);
        art.Children.Add(new Border { Background=BoardBackground(),CornerRadius=new(22) });
        var canvas=new Canvas { Width=400,Height=390 };
        var symbols=Catalog.Deck(Vm.Preferences.Set,144).Distinct().Take(12).ToArray();
        for(int y=0;y<3;y++) for(int x=0;x<4;x++) { var tile=new TileArt {Symbol=symbols[(x+y*4)%symbols.Length]}; Canvas.SetLeft(tile,x*76+10);Canvas.SetTop(tile,y*94+35);canvas.Children.Add(tile); }
        var decoration=new Viewbox { Child=canvas,Margin=new(30) }; art.Children.Add(decoration);g.Children.Add(art);ContentArea.Children.Add(g);
    }
    StackPanel Page(string title, string subtitle="")
    {
        var scroll=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
        var panel=new StackPanel {Margin=new(24,4,24,24)};
        panel.Children.Add(TitleText(title)); if(subtitle!="") panel.Children.Add(Text(subtitle,15,"Muted"));
        scroll.Content=panel;ContentArea.Children.Add(scroll);return panel;
    }
    void RenderNew()
    {
        var panel=Page(T("choose"),T("chooseSub"));
        var cards=new System.Windows.Controls.Primitives.UniformGrid {Columns=3,Margin=new(0,18,0,6)};
        foreach(var l in Vm.Layouts)
        {
            var stack=new StackPanel();
            var thumbnail=new Canvas {Width=290,Height=82};
            double maxX=l.Positions.Max(p=>p.X)+2, minX=l.Positions.Min(p=>p.X), maxY=l.Positions.Max(p=>p.Y)+2;
            double scale=Math.Min(270/(maxX-minX),75/maxY);
            foreach(var p in l.Positions.OrderBy(p=>p.Z))
            {
                var r=new Border {Width=scale*1.85,Height=scale*1.85,Background=p.Z==0?new SolidColorBrush(Color.FromRgb(119,159,137)):R("Accent"),BorderBrush=R("Surface"),BorderThickness=new(.5),CornerRadius=new(1)};
                Canvas.SetLeft(r,10+(p.X-minX)*scale+p.Z*1.5);Canvas.SetTop(r,4+p.Y*scale-p.Z*1.5);thumbnail.Children.Add(r);
            }
            stack.Children.Add(thumbnail);stack.Children.Add(Text(T(l.Id),20));stack.Children.Add(Text(T(l.Id+"Level")+" · "+Vm.Text.Number(l.Count,"tile"),12,"Muted"));
            var b=Button("",()=>{Vm.Preferences.Layout=l.Id;Vm.SavePreferences();RenderScreen();});b.Content=stack;b.Padding=new(14);b.HorizontalContentAlignment=HorizontalAlignment.Stretch;
            AutomationProperties.SetName(b,T(l.Id)+", "+T(l.Id+"Level"));
            if(Vm.Preferences.Layout==l.Id){b.BorderBrush=R("Accent");b.BorderThickness=new(2);}
            cards.Children.Add(b);
        }
        panel.Children.Add(cards);
        panel.Children.Add(Text(T("tileSet"),16));
        var sets=new System.Windows.Controls.Primitives.UniformGrid {Columns=3};
        foreach(var set in Catalog.Sets)
        {
            var s=new StackPanel();s.Children.Add(Text(T(set),18));
            var tiles=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
            foreach(int rank in new[]{1,17,29}) { var symbol=set=="classic"? Catalog.Deck(set,144)[(rank*4)%144] : new Symbol(set+rank,set+rank,set,rank); tiles.Children.Add(new Viewbox {Width=49,Height=66,Margin=new(3,6,3,0),Child=new TileArt{Symbol=symbol}}); }
            s.Children.Add(tiles);var b=Button("",()=>{Vm.Preferences.Set=set;Vm.SavePreferences();RenderScreen();}); b.Content=s;AutomationProperties.SetName(b,T(set));
            if(Vm.Preferences.Set==set){b.BorderBrush=R("Accent");b.BorderThickness=new(2);}sets.Children.Add(b);
        }
        panel.Children.Add(sets);
        var actions=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new(0,12,0,0)};
        actions.Children.Add(Button(T("back"),()=>Vm.Navigate("menu")));
        var start=Button(T("start"),()=>ConfirmNew(false),true);start.IsEnabled=!Vm.Busy;
        Vm.PropertyChanged += StartChanged;
        void StartChanged(object? sender,PropertyChangedEventArgs e) { start.IsEnabled=!Vm.Busy; if(Vm.Screen!="new")Vm.PropertyChanged-=StartChanged; }
        actions.Children.Add(start);panel.Children.Add(actions);
        var status=Text("",13,"Muted");Bind(status,TextBlock.TextProperty,nameof(Vm.Status));panel.Children.Add(status);
    }
    void ConfirmNew(bool replay)
    {
        if(Vm.Busy || confirming)return;
        if(!Vm.HasSave || Vm.Game!.Tiles.Count==0) { _=Vm.NewGame(replay); return; }
        confirming=true;
        var mask=new Grid {Background=new SolidColorBrush(Color.FromArgb(210,9,22,20))};
        var card=new StackPanel {MaxWidth=500,VerticalAlignment=VerticalAlignment.Center,Margin=new(24)};
        card.Children.Add(TitleText(T("confirm"),30));card.Children.Add(Text(T("replace"),20));
        var actions=new StackPanel {Orientation=Orientation.Horizontal,Margin=new(0,20,0,0)};
        var yes=Button(T("start"),()=>{confirming=false;ContentArea.Children.Remove(mask);_=Vm.NewGame(replay);},true);
        actions.Children.Add(yes);actions.Children.Add(Button(T("back"),()=>{confirming=false;ContentArea.Children.Remove(mask);}));card.Children.Add(actions);mask.Children.Add(card);ContentArea.Children.Add(mask);yes.Focus();
    }
    void RenderGame()
    {
        if(Vm.Game==null){Vm.Navigate("menu");return;}
        var g=new Grid();g.RowDefinitions.Add(new(){Height=new(82)});g.RowDefinitions.Add(new(){Height=new(1,GridUnitType.Star)});g.RowDefinitions.Add(new(){Height=new(36)});g.RowDefinitions.Add(new(){Height=new(66)});
        var header=new Grid();header.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});header.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var title=new StackPanel();title.Children.Add(TitleText(T(Vm.Game.LayoutId),30));title.Children.Add(Text(T(Vm.Game.LayoutId+"Level")+" / "+T(Vm.Game.SetId),12,"Muted"));header.Children.Add(title);
        var stats=new StackPanel {Orientation=Orientation.Horizontal};Grid.SetColumn(stats,1);
        foreach(var (key,path) in new[]{("time",nameof(Vm.Clock)),("remaining",nameof(Vm.Remaining)),("pairs",nameof(Vm.PairCount)),("hints",nameof(Vm.HintCount))})
        {
            var s=new StackPanel {Margin=new(25,9,0,0)};s.Children.Add(Text(T(key),11,"Muted"));var value=Text("",22);value.Margin=new(0,7,0,0);Bind(value,TextBlock.TextProperty,path);s.Children.Add(value);stats.Children.Add(s);
        }
        header.Children.Add(stats);g.Children.Add(header);
        boardHost=new Grid {ClipToBounds=true};Grid.SetRow(boardHost,1);g.Children.Add(boardHost);
        var status=Text("",13,"Muted");Bind(status,TextBlock.TextProperty,nameof(Vm.Status));status.VerticalAlignment=VerticalAlignment.Center;Grid.SetRow(status,2);g.Children.Add(status);
        var bar=new Grid();Grid.SetRow(bar,3);
        var tools=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
        var hint=Button("",()=>{});hint.Command=Vm.HintCommand;Bind(hint,ContentControl.ContentProperty,nameof(Vm.HintLabel)); AutomationProperties.SetName(hint,T("hint"));tools.Children.Add(hint);
        var shuffle=Button("",()=>{});shuffle.Command=Vm.ShuffleCommand;Bind(shuffle,ContentControl.ContentProperty,nameof(Vm.ShuffleLabel));AutomationProperties.SetName(shuffle,T("shuffle"));tools.Children.Add(shuffle);
        var pause=Button("",()=>{});pause.Command=Vm.PauseCommand;Bind(pause,ContentControl.ContentProperty,nameof(Vm.PauseLabel));AutomationProperties.SetName(pause,T("pause"));tools.Children.Add(pause);
        tools.Children.Add(Button(T("menu"),()=>Vm.Navigate("menu")));bar.Children.Add(tools);g.Children.Add(bar);ContentArea.Children.Add(g);DrawBoard();
    }
    void DrawBoard()
    {
        if(boardHost==null || Vm.Game==null || Vm.Screen!="game") return;
        int? focused=tileButtons.FirstOrDefault(p=>p.Value.IsKeyboardFocused).Value?.Tag as int?;
        boardHost.Children.Clear();tileButtons.Clear();
        boardHost.Children.Add(new Border {Background=BoardBackground(),CornerRadius=new(16)});
        if(Vm.Paused || Vm.Game.Tiles.Count==0)
        {
            var p=new Grid {VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center,MaxWidth=620,Margin=new(20)};
            p.Children.Add(new Border{Background=R("Surface"),CornerRadius=new(16),Padding=new(20),Child=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Content=OverlayText()}});boardHost.Children.Add(p);
            if(!Vm.Paused && celebratedGame != Vm.Game.GameId)
            {
                celebratedGame = Vm.Game.GameId;
                if(!Vm.Preferences.ReducedMotion)
                {
                    boardHost.Children.Add(new VictoryCelebration(Vm.Game.GameId.GetHashCode()));
                    p.BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(450)));
                }
            }
            return;
        }
        var positions=Vm.Game.Positions; var active=Rules.Active(Vm.Game);
        double minX=positions.Min(p=>p.X)*38, minY=positions.Min(p=>p.Y)*47-positions.Max(p=>p.Z)*7;
        var canvas=new Canvas {Width=(positions.Max(p=>p.X)*38-minX)+100+positions.Max(p=>p.Z)*5,Height=positions.Max(p=>p.Y)*47-minY+122};
        foreach(var p in active.OrderBy(p=>p.Z).ThenBy(p=>p.Y).ThenBy(p=>p.X))
        {
            var symbol=Vm.Game.Tiles[p.Id];bool free=Rules.Free(p,active);bool hint=Vm.Highlight is Pair pair && (pair.A==p.Id || pair.B==p.Id);
            var art=new TileArt {Symbol=symbol,Blocked=!free,Selected=Vm.Selected==p.Id,Hint=hint,Labels=Vm.Preferences.Labels};
            var tile=new Button {Content=art,Padding=new(0),Margin=new(0),Background=Brushes.Transparent,BorderThickness=new(0),Tag=p.Id,IsTabStop=false,ToolTip=SymbolName(symbol)+" · "+T(free?"free":"locked")};
            string name=SymbolName(symbol)+", "+T(free?"free":"locked")+(Vm.Selected==p.Id?", "+T("selected"):"");AutomationProperties.SetName(tile,name);
            Canvas.SetLeft(tile,12+p.X*38-minX+p.Z*5);Canvas.SetTop(tile,12+p.Y*47-minY-p.Z*7);Panel.SetZIndex(tile,p.Z*1000+(int)p.Y*10);
            tile.Click+=(_,_)=>Vm.Choose(p.Id); canvas.Children.Add(tile);tileButtons[p.Id]=tile;
            if(hint && !Vm.Preferences.ReducedMotion) art.BeginAnimation(OpacityProperty,new DoubleAnimation(.6,1,TimeSpan.FromMilliseconds(500)){AutoReverse=true,RepeatBehavior=new RepeatBehavior(2)});
        }
        var firstFree=active.FirstOrDefault(p=>Rules.Free(p,active)); if(firstFree!=null)tileButtons[firstFree.Id].IsTabStop=true;
        var view=new Viewbox {Child=canvas,Margin=new(22,15,22,15),Stretch=Stretch.Uniform};boardHost.Children.Add(view);
        if(focused.HasValue && tileButtons.TryGetValue(focused.Value,out var button)) button.Focus();
        if(Vm.Busy) boardHost.Children.Add(new Border{Background=new SolidColorBrush(Color.FromArgb(170,15,32,27)),Child=new TextBlock{Text=T("working"),Foreground=Brushes.White,FontSize=24,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center}});
        if(Rules.Pairs(Vm.Game).Count==0)
        {
            var warning=new Border {Background=R("Surface"),CornerRadius=new(9),Padding=new(14),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Bottom,Margin=new(15)};
            var s=new StackPanel();s.Children.Add(Text(T("noMoves"),20));s.Children.Add(Text(T(Vm.Game.ShuffleUsed?"noMovesEnd":"noMovesShuffle"),13));
            if(!Vm.Game.ShuffleUsed) {var b=Button(Vm.ShuffleLabel,()=>_=Vm.Shuffle(),true);b.IsEnabled=Vm.CanShuffle;s.Children.Add(b);} else s.Children.Add(Button(T("newGame"),()=>Vm.Navigate("new"),true));
            warning.Child=s;boardHost.Children.Add(warning);
        }
    }
    StackPanel OverlayText()
    {
        var p=new StackPanel();p.Children.Add(TitleText(T(Vm.Paused?"paused":"congratulations"),36));
        if(!Vm.Paused) p.Children.Add(Text(T("won"),22,"Accent"));
        p.Children.Add(Text(T(Vm.Paused?"pausedSub":"wonSub"),18));
        if(!Vm.Paused)p.Children.Add(Text(Vm.Clock+" · "+T("hints")+": "+Vm.HintCount,16,"Muted"));
        if(!Vm.Paused)
        {
            if(Vm.ResultSaved) p.Children.Add(Text(T("resultSaved"),14,"Accent"));
            else
            {
                p.Children.Add(Text(T("playerName"),13,"Muted"));
                var input=new Grid();input.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});input.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
                var name=new TextBox {MaxLength=32,FontSize=16,Padding=new(8),Margin=new(5),MinWidth=140,VerticalContentAlignment=VerticalAlignment.Center};
                AutomationProperties.SetName(name,T("playerName"));input.Children.Add(name);
                var save=Button(T("saveResult"),()=>{if(Vm.SaveResult(name.Text)) DrawBoard();},true);Grid.SetColumn(save,1);input.Children.Add(save);p.Children.Add(input);
            }
        }
        var actions=new WrapPanel();
        actions.Children.Add(Button(T(Vm.Paused?"resume":"replay"),()=>{if(Vm.Paused)Vm.TogglePause();else _=Vm.NewGame(true);},true));
        if(!Vm.Paused){actions.Children.Add(Button(T("results"),()=>Vm.Navigate("results")));actions.Children.Add(Button(T("newGame"),()=>Vm.Navigate("new")));}
        actions.Children.Add(Button(T("menu"),()=>Vm.Navigate("menu")));p.Children.Add(actions);return p;
    }
    string SymbolName(Symbol s)
    {
        if(s.Family is "geometry" or "nature")return T((s.Family=="geometry"?"shape":"nature")+((s.Rank-1)%12))+" "+((s.Rank-1)/12+1);
        return T(s.Family)+" "+s.Rank;
    }
    void RenderSettings()
    {
        var p=Page(T("settings"));
        var cols=new Grid();cols.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});cols.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});
        var a=new StackPanel {Margin=new(0,0,35,0)};var b=new StackPanel{Margin=new(20,0,0,0)};Grid.SetColumn(b,1);cols.Children.Add(a);cols.Children.Add(b);
        a.Children.Add(Text(T("language")));a.Children.Add(Combo(["pl","en"],Vm.Preferences.Language,id=>Vm.ChangeLanguage(id),id=>id=="pl"?"Polski":"English"));
        a.Children.Add(Text(T("theme")));a.Children.Add(Combo(["light","dark","contrast"],Vm.Preferences.Theme,id=>{Vm.Preferences.Theme=id;Vm.SavePreferences();ApplyTheme();RenderScreen();}));
        a.Children.Add(Text(T("tileSet")));a.Children.Add(Combo(Catalog.Sets,Vm.Preferences.Set,id=>{Vm.Preferences.Set=id;Vm.SavePreferences();}));a.Children.Add(Text(T("nextGame"),12,"Muted"));
        a.Children.Add(Check("labels",Vm.Preferences.Labels,v=>Vm.Preferences.Labels=v));a.Children.Add(Check("reducedMotion",Vm.Preferences.ReducedMotion,v=>Vm.Preferences.ReducedMotion=v));
        b.Children.Add(Text(T("background")));var backgrounds=new WrapPanel();
        foreach(var id in new[]{"felt","wood","forest","paper","minimal"})
        {
            var content=new StackPanel();content.Children.Add(new Border{Width=130,Height=55,Background=BackgroundArt.Create(id),CornerRadius=new(5)});content.Children.Add(Text(T(id),12));
            var button=Button("",()=>{Vm.Preferences.Background=id;Vm.SavePreferences();RenderScreen();});button.Content=content;AutomationProperties.SetName(button,T(id));if(Vm.Preferences.Background==id)button.BorderBrush=R("Accent");backgrounds.Children.Add(button);
        }
        b.Children.Add(backgrounds);b.Children.Add(Check("mute",Vm.Preferences.Muted,v=>Vm.Preferences.Muted=v));b.Children.Add(Text(T("volume")));
        var custom = new StackPanel();
        custom.Children.Add(Button(T("chooseBackground"),ChooseBackground));
        custom.Children.Add(Text(T("customBackgroundHelp"),12,"Muted"));
        if(Vm.CustomBackground.Available)
        {
            var preview = new StackPanel {Orientation=Orientation.Horizontal};
            preview.Children.Add(new Border {Width=110,Height=50,Background=Vm.CustomBackground.Brush,CornerRadius=new(5)});
            preview.Children.Add(Text(T("customBackground"),13));
            var select = Button("",()=>{Vm.Preferences.Background="custom";Vm.SavePreferences();RenderScreen();});
            select.Content=preview; AutomationProperties.SetName(select,T("customBackground"));
            if(Vm.Preferences.Background=="custom") select.BorderBrush=R("Accent");
            custom.Children.Add(select);
        }
        b.Children.Insert(2,custom);
        var slider=new Slider {Minimum=0,Maximum=1,Value=Vm.Preferences.Volume,Margin=new(0,15,0,15),TickFrequency=.1,IsSnapToTickEnabled=true};AutomationProperties.SetName(slider,T("volume"));
        slider.ValueChanged+=(_,_)=>{Vm.Preferences.Volume=slider.Value;Vm.SavePreferences();};slider.PreviewMouseLeftButtonUp+=(_,_)=>Vm.Play("select");b.Children.Add(slider);
        p.Children.Add(cols);p.Children.Add(Button(T("back"),()=>Vm.Navigate(settingsReturn),true));
        var status=Text("",13,"Muted");Bind(status,TextBlock.TextProperty,nameof(Vm.Status));p.Children.Add(status);
    }
    async void ChooseBackground()
    {
        if(Vm.Busy) return;
        var dialog=new OpenFileDialog {Title=T("chooseBackground"),Filter=T("imageFilter"),CheckFileExists=true,Multiselect=false};
        if(dialog.ShowDialog(this)==true)
        {
            await Vm.ImportBackground(dialog.FileName);
            RenderScreen();
        }
    }
    void RenderResults()
    {
        var panel=Page(T("results"),T("resultsHelp"));
        if(Vm.Results.Count==0) panel.Children.Add(Text(T("noResults"),18,"Muted"));
        var culture=CultureInfo.GetCultureInfo(Vm.Text.Language=="pl"?"pl-PL":"en-GB");
        foreach(var result in Vm.Results)
        {
            var row=new StackPanel();
            string player=string.IsNullOrWhiteSpace(result.Player)?T("playerDefault"):result.Player;
            row.Children.Add(Text(player+" · "+TimeSpan.FromSeconds(result.Seconds).ToString(@"hh\:mm\:ss"),21));
            row.Children.Add(Text(T(result.LayoutId)+" / "+T(result.SetId)+" · "+Vm.Text.Number(result.TileCount,"tile"),14));
            row.Children.Add(Text(result.FinishedAt.ToLocalTime().ToString("g",culture)+" · "+T("hints")+": "+result.Hints+" · "+T("shuffleUsed")+": "+T(result.ShuffleUsed?"yes":"no"),13,"Muted"));
            panel.Children.Add(new Border {Background=R("Panel"),CornerRadius=new(9),Padding=new(16),Margin=new(0,12,0,0),Child=row});
        }
        var status=Text("",13,"Muted");Bind(status,TextBlock.TextProperty,nameof(Vm.Status));panel.Children.Add(status);
        panel.Children.Add(Button(T("back"),()=>Vm.Navigate(Vm.Game?.Tiles.Count==0?"game":"menu"),true));
        panel.Children.Add(Button(T("menu"),()=>Vm.Navigate("menu")));
    }
    ComboBox Combo(string[] ids,string selected,Action<string> change,Func<string,string>? label=null)
    {
        var combo=new ComboBox();foreach(var id in ids)combo.Items.Add(new ComboBoxItem{Content=label?.Invoke(id)??T(id),Tag=id});combo.SelectedIndex=Array.IndexOf(ids,selected);
        AutomationProperties.SetName(combo,string.Join(", ",ids.Select(id=>label?.Invoke(id)??T(id))));
        combo.SelectionChanged+=(_,_)=>{if(combo.SelectedItem is ComboBoxItem item)change((string)item.Tag);};return combo;
    }
    CheckBox Check(string key,bool value,Action<bool> change)
    {
        var c=new CheckBox {Content=T(key),IsChecked=value};AutomationProperties.SetName(c,T(key));c.Checked+=(_,_)=>{change(true);Vm.SavePreferences();};c.Unchecked+=(_,_)=>{change(false);Vm.SavePreferences();};return c;
    }
    void RenderHelp()
    {
        var p=Page(T("helpTitle"));var text=Text(T("helpText"),17);text.LineHeight=27;text.MaxWidth=900;text.HorizontalAlignment=HorizontalAlignment.Left;p.Children.Add(text);p.Children.Add(Button(T("back"),()=>Vm.Navigate("menu"),true));
    }
    void OpenSettings() {if(Vm.Busy)return;if(Vm.Screen!="settings")settingsReturn=Vm.Screen;Vm.Navigate("settings");}
    void Language_Click(object sender,RoutedEventArgs e) {if(!confirming)Vm.ChangeLanguage(Vm.Preferences.Language=="pl"?"en":"pl");}
    void Settings_Click(object sender,RoutedEventArgs e) {if(!confirming)OpenSettings();}
    void Fullscreen_Click(object sender,RoutedEventArgs e)=>Fullscreen();
    void Fullscreen(){ if(WindowStyle==WindowStyle.None){WindowStyle=WindowStyle.SingleBorderWindow;WindowState=WindowState.Normal;}else{WindowStyle=WindowStyle.None;WindowState=WindowState.Maximized;} }
    void Window_KeyDown(object sender,KeyEventArgs e)
    {
        if(e.Key==Key.F11){Fullscreen();e.Handled=true;return;}
        if(Vm.Screen!="game" || confirming || Vm.Busy)return;
        if(e.Key==Key.Escape){Vm.TogglePause();e.Handled=true;return;}
        if(e.Key==Key.H){Vm.Hint();e.Handled=true;return;}
        if(!Vm.Playing || e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down))return;
        var current=tileButtons.Values.FirstOrDefault(b=>b.IsKeyboardFocused);
        if(current==null){tileButtons.Values.FirstOrDefault()?.Focus();e.Handled=true;return;}
        int id=(int)current.Tag;var p=Vm.Game!.Positions.Single(p=>p.Id==id);
        var candidates=Rules.Active(Vm.Game).Where(q=>q.Id!=id).Select(q=>new{P=q,DX=(q.X-p.X)*38+(q.Z-p.Z)*5,DY=(q.Y-p.Y)*47-(q.Z-p.Z)*7});
        var next=candidates.Where(q=>e.Key switch{Key.Left=>q.DX<-.1,Key.Right=>q.DX>.1,Key.Up=>q.DY<-.1,_=>q.DY>.1})
            .OrderBy(q=>Math.Sqrt(q.DX*q.DX+q.DY*q.DY)+ (e.Key is Key.Left or Key.Right?Math.Abs(q.DY)*3:Math.Abs(q.DX)*3)).FirstOrDefault();
        if(next!=null)tileButtons[next.P.Id].Focus();e.Handled=true;
    }
    void Window_Closing(object? sender,CancelEventArgs e){if(Vm.Busy){e.Cancel=true;return;}timer.Stop();Vm.Close();}
}
