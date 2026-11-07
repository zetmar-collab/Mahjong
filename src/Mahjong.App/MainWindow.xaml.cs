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
    internal GameViewModel Vm;
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    readonly Dictionary<int, Button> tileButtons = [];
    Grid? boardHost;
    string settingsReturn = "menu";
    bool confirming;
    Guid? celebratedGame;
    Guid? renderedGame;
    double renderedZoom;
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
    TextBlock Text(string value, double size = 16, string brush = "Text") => new() { Text = value, FontSize = Math.Max(12,size), Foreground = R(brush=="Accent"?"AccentText":brush), TextWrapping = TextWrapping.Wrap };
    TextBlock TitleText(string value, double size = 34) => new() { Text = value, FontSize = size, FontFamily = new("Georgia"), Foreground = R("Text"), Margin = new(0, 8, 0, 12) };
    Button Button(string label, Action action, bool primary = false)
    {
        var b = new Button { Content = label, HorizontalContentAlignment = HorizontalAlignment.Center };
        AutomationProperties.SetName(b, label);
        if (primary) b.Style=(Style)FindResource("PrimaryButton");
        b.Click += (_, _) => action(); return b;
    }
    static void Bind(DependencyObject obj, DependencyProperty property, string path) => BindingOperations.SetBinding(obj, property, new Binding(path));
    internal void RefreshAppearance() { ApplyTheme(); RenderScreen(); }
    void ApplyTheme()
    {
        string[] values = Vm.Preferences.Theme switch
        {
            "light" => ["#F3F1E9", "#FFFFFF", "#203A30", "#52665B", "#82988A", "#264E3C", "#FFFFFF", "#264E3C", "#F8FAF6", "#E2EDDF", "#276650"],
            "contrast" => ["#000000", "#101010", "#FFFFFF", "#FFFFFF", "#FFFFFF", "#FFE15B", "#000000", "#FFE15B", "#000000", "#333333", "#77EDFF"],
            _ => ["#102722", "#19372F", "#F7F3E8", "#B8C9BF", "#446357", "#E8C782", "#192D25", "#E8C782", "#112C24", "#2B4F40", "#91DCC5"]
        };
        string[] keys = ["Surface", "Panel", "Text", "Muted", "Line", "Accent", "AccentInk", "AccentText", "Input", "SelectedSurface", "Focus"];
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
            case "tutorial": RenderTutorial(); break;
            default: RenderMenu(); break;
        }
    }
    void RenderMenu()
    {
        var grid=new Grid{Margin=new(24,12,24,12)};grid.ColumnDefinitions.Add(new(){Width=new(5,GridUnitType.Star)});grid.ColumnDefinitions.Add(new(){Width=new(6,GridUnitType.Star)});
        var left=new StackPanel{VerticalAlignment=VerticalAlignment.Center,Margin=new(12,0,48,0),MaxWidth=420};
        left.Children.Add(Text(T("solitaire").ToUpperInvariant(),12,"AccentText"));left.Children.Add(TitleText(T("subtitle"),46));
        var intro=Text(T("menuIntro"),16,"Muted");intro.LineHeight=25;intro.Margin=new(0,0,0,24);left.Children.Add(intro);
        if(Vm.HasSave)
        {
            left.Children.Add(Button(T("continue"),()=>Vm.Navigate("game"),true));
            left.Children.Add(Text(T(Vm.Game!.LayoutId)+" · "+T(Vm.Game.SetId)+" · "+Vm.Clock,13,"Muted"));
        }
        left.Children.Add(Button(T("newGame"),()=>Vm.Navigate("new"),!Vm.HasSave));
        var links=new System.Windows.Controls.Primitives.UniformGrid{Columns=2,Margin=new(0,12,0,0)};
        links.Children.Add(Button(T("help"),()=>Vm.Navigate("help")));links.Children.Add(Button(T("results"),()=>{Vm.LoadResults();Vm.Navigate("results");}));
        links.Children.Add(Button(T("settings"),OpenSettings));links.Children.Add(Button(T("exit"),Close));left.Children.Add(links);
        var message=Text("",13,"Muted");Bind(message,TextBlock.TextProperty,nameof(Vm.Status));message.Margin=new(4,20,0,0);left.Children.Add(message);grid.Children.Add(left);
        var art=new Grid{Margin=new(12,12,0,12)};Grid.SetColumn(art,1);art.Children.Add(new Border{Background=BoardBackground(),CornerRadius=new(24)});
        var stage=new StackPanel{VerticalAlignment=VerticalAlignment.Center,Margin=new(24)};
        var caption=Text(T("tableCaption").ToUpperInvariant(),12);caption.HorizontalAlignment=HorizontalAlignment.Center;stage.Children.Add(new Border{Child=caption,Background=R("Panel"),Padding=new(14,8,14,8),CornerRadius=new(8),HorizontalAlignment=HorizontalAlignment.Center});
        var canvas=new Canvas{Width=360,Height=310};var deck=Catalog.Deck(Vm.Preferences.Set,144).Distinct().ToArray();
        for(int i=0;i<6;i++)
        {
            var tile=new TileArt{Symbol=deck[(i*5)%deck.Length]};var box=new Viewbox{Width=90,Height=118,Child=tile,RenderTransform=new RotateTransform(i%2==0?-6:6,45,59)};
            Canvas.SetLeft(box,28+(i%3)*100);Canvas.SetTop(box,40+(i/3)*125);canvas.Children.Add(box);
        }
        stage.Children.Add(new Viewbox{Child=canvas,MaxHeight=390,Margin=new(0,12,0,12)});
        var note=Text(T("tableNote"),14);note.HorizontalAlignment=HorizontalAlignment.Center;stage.Children.Add(new Border{Child=note,Background=R("Panel"),Padding=new(14,8,14,8),CornerRadius=new(8),HorizontalAlignment=HorizontalAlignment.Center});
        art.Children.Add(stage);grid.Children.Add(art);ContentArea.Children.Add(grid);
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
            var thumbnail=new Canvas {Width=240,Height=60};
            double maxX=l.Positions.Max(p=>p.X)+2, minX=l.Positions.Min(p=>p.X), maxY=l.Positions.Max(p=>p.Y)+2;
            double scale=Math.Min(220/(maxX-minX),54/maxY);
            foreach(var p in l.Positions.OrderBy(p=>p.Z))
            {
                var r=new Border {Width=scale*1.85,Height=scale*1.85,Background=p.Z==0?new SolidColorBrush(Color.FromRgb(119,159,137)):R("Accent"),BorderBrush=R("Surface"),BorderThickness=new(.5),CornerRadius=new(1)};
                Canvas.SetLeft(r,10+(p.X-minX)*scale+p.Z*1.5);Canvas.SetTop(r,4+p.Y*scale-p.Z*1.5);thumbnail.Children.Add(r);
            }
            stack.Children.Add(thumbnail);stack.Children.Add(Text(T(l.Id),19));stack.Children.Add(Text(T(l.Id+"Level")+" · "+Vm.Text.Number(l.Count,"tile")+(Vm.Preferences.Layout==l.Id?" · "+T("chosen"):""),12,"Muted"));
            var b=Button("",()=>{Vm.Preferences.Layout=l.Id;Vm.SavePreferences();RenderScreen();});b.Content=stack;b.Padding=new(12);b.HorizontalContentAlignment=HorizontalAlignment.Stretch;
            AutomationProperties.SetName(b,T(l.Id)+", "+T(l.Id+"Level"));
            if(Vm.Preferences.Layout==l.Id){b.BorderBrush=R("AccentText");b.Background=R("SelectedSurface");}
            cards.Children.Add(b);
        }
        panel.Children.Add(cards);
        panel.Children.Add(Text(T("tileSet"),16));
        var sets=new System.Windows.Controls.Primitives.UniformGrid {Columns=3};
        foreach(var set in Catalog.Sets)
        {
            var s=new StackPanel();s.Children.Add(Text(T(set)+(Vm.Preferences.Set==set?" · "+T("chosen"):""),17));
            var tiles=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
            foreach(int rank in new[]{1,17,29}) { var symbol=set=="classic"? Catalog.Deck(set,144)[(rank*4)%144] : new Symbol(set+rank,set+rank,set,rank); tiles.Children.Add(new Viewbox {Width=36,Height=47,Margin=new(3,6,3,0),Child=new TileArt{Symbol=symbol}}); }
            s.Children.Add(tiles);var b=Button("",()=>{Vm.Preferences.Set=set;Vm.SavePreferences();RenderScreen();}); b.Content=s;AutomationProperties.SetName(b,T(set));
            if(Vm.Preferences.Set==set){b.BorderBrush=R("AccentText");b.Background=R("SelectedSurface");}sets.Children.Add(b);
        }
        panel.Children.Add(sets);
        var actions=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new(0,12,0,0)};
        actions.Children.Add(Button(T("back"),()=>Vm.Navigate("menu")));
        var start=Button(T("start"),()=>ConfirmNew(false),true); Bind(start,IsEnabledProperty,nameof(Vm.CanStart));
        actions.Children.Add(start);panel.Children.Add(actions);
        actions.Children.Insert(1,Button(T("daily"),()=>ConfirmReplace(()=>Vm.NewGame(daily:DateOnly.FromDateTime(DateTime.Now)))));
        var codePanel=new StackPanel();codePanel.Children.Add(Text(T("dealCodeHelp"),13,"Muted"));
        var code=new TextBox {MaxLength=1024,Margin=new(4,12,4,8),TextWrapping=TextWrapping.Wrap};AutomationProperties.SetName(code,T("dealCode"));codePanel.Children.Add(code);
        codePanel.Children.Add(Button(T("loadCode"),()=>ConfirmReplace(()=>Vm.NewGame(dealCode:code.Text))));
        panel.Children.Add(new Expander {Header=T("playCode"),Content=codePanel,Margin=new(4,16,4,8),Foreground=R("Text"),FontSize=15});
        var status=Text("",13,"Muted");Bind(status,TextBlock.TextProperty,nameof(Vm.Status));panel.Children.Add(status);
        DockActions(panel,actions);
    }
    void ConfirmNew(bool replay)
        => ConfirmReplace(()=>Vm.NewGame(replay));
    void ConfirmReplace(Func<Task> start)
    {
        if(Vm.Busy || confirming)return;
        if(!Vm.HasSave || Vm.Game!.Tiles.Count==0) { _=start(); return; }
        confirming=true;
        var dialog=new Window {Owner=this,Title=T("confirm"),Width=560,SizeToContent=SizeToContent.Height,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=R("Surface")};
        var card=new StackPanel {Margin=new(24)};
        card.Children.Add(TitleText(T("confirm"),30));card.Children.Add(Text(T("replace"),20));
        var actions=new StackPanel {Orientation=Orientation.Horizontal,Margin=new(0,20,0,0)};
        var yes=Button(T("start"),()=>dialog.DialogResult=true,true);yes.IsDefault=true;
        var cancel=Button(T("back"),()=>dialog.DialogResult=false);cancel.IsCancel=true;
        actions.Children.Add(yes);actions.Children.Add(cancel);card.Children.Add(actions);dialog.Content=card;
        bool accepted=false;
        try { accepted=dialog.ShowDialog()==true; } finally { confirming=false; }
        if(accepted) _=start();
    }
    void RenderGame()
    {
        if(Vm.Game==null){Vm.Navigate("menu");return;}
        var g=new Grid();g.RowDefinitions.Add(new(){Height=new(82)});g.RowDefinitions.Add(new(){Height=new(1,GridUnitType.Star)});g.RowDefinitions.Add(new(){Height=new(36)});g.RowDefinitions.Add(new(){Height=GridLength.Auto});
        var header=new Grid();header.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});header.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var title=new StackPanel();title.Children.Add(TitleText(T(Vm.Game.LayoutId),30));title.Children.Add(Text(T(Vm.Game.LayoutId+"Level")+" / "+T(Vm.Game.SetId)+(Vm.Game.ChallengeDate==null?"":" · "+T("daily")+" "+Vm.Game.ChallengeDate),12,"Muted"));header.Children.Add(title);
        var stats=new StackPanel {Orientation=Orientation.Horizontal,Margin=new(10,0,10,0)};
        foreach(var (key,path) in new[]{("time",nameof(Vm.Clock)),("remaining",nameof(Vm.Remaining)),("pairs",nameof(Vm.PairCount)),("hints",nameof(Vm.HintCount))})
        {
            var s=new StackPanel {Margin=new(25,9,0,0)};s.Children.Add(Text(T(key),12,"Muted"));var value=Text("",22);value.Margin=new(0,7,0,0);Bind(value,TextBlock.TextProperty,path);s.Children.Add(value);stats.Children.Add(s);
        }
        var statsCard=new Border{Child=stats,Background=R("Panel"),CornerRadius=new(12),Padding=new(0,0,16,8),Margin=new(16,0,0,8)};Grid.SetColumn(statsCard,1);header.Children.Add(statsCard);g.Children.Add(header);
        boardHost=new Grid {ClipToBounds=true};Grid.SetRow(boardHost,1);g.Children.Add(boardHost);
        var status=Text("",13,"Muted");Bind(status,TextBlock.TextProperty,nameof(Vm.Status));status.VerticalAlignment=VerticalAlignment.Center;Grid.SetRow(status,2);g.Children.Add(status);
        var bar=new Grid();Grid.SetRow(bar,3);
        var tools=new WrapPanel {HorizontalAlignment=HorizontalAlignment.Center};
        var undo=Button(T("undo"),()=>{});undo.Command=Vm.UndoCommand;tools.Children.Add(undo);
        var hint=Button("",()=>{});hint.Command=Vm.HintCommand;Bind(hint,ContentControl.ContentProperty,nameof(Vm.HintLabel)); AutomationProperties.SetName(hint,T("hint"));tools.Children.Add(hint);
        var shuffle=Button("",()=>{});shuffle.Command=Vm.ShuffleCommand;Bind(shuffle,ContentControl.ContentProperty,nameof(Vm.ShuffleLabel));AutomationProperties.SetName(shuffle,T("shuffle"));tools.Children.Add(shuffle);
        var pause=Button("",()=>{});pause.Command=Vm.PauseCommand;Bind(pause,ContentControl.ContentProperty,nameof(Vm.PauseLabel));AutomationProperties.SetName(pause,T("pause"));tools.Children.Add(pause);
        tools.Children.Add(Button(T("menu"),()=>Vm.Navigate("menu")));bar.Children.Add(tools);g.Children.Add(bar);ContentArea.Children.Add(g);DrawBoard();
        tools.Children.Add(new Border{Width=16});
        var zoomOut=Button("−",()=>SetZoom(Vm.Preferences.Zoom-.25));AutomationProperties.SetName(zoomOut,T("zoomOut"));zoomOut.ToolTip=T("zoomOut");tools.Children.Add(zoomOut);
        tools.Children.Add(Button(T("zoomFit"),()=>SetZoom(1)));
        var zoomIn=Button("+",()=>SetZoom(Vm.Preferences.Zoom+.25));AutomationProperties.SetName(zoomIn,T("zoomIn"));zoomIn.ToolTip=T("zoomIn");tools.Children.Add(zoomIn);
        tools.Children.Add(Button(T("dealCode"),ShowDealCode));
    }
    void DrawBoard()
    {
        if(boardHost==null || Vm.Game==null || Vm.Screen!="game") return;
        if(!Vm.Paused && !Vm.Busy && Vm.Game.Tiles.Count>0 && renderedGame==Vm.Game.GameId && renderedZoom==Vm.Preferences.Zoom
            && tileButtons.Count==Vm.Game.Tiles.Count && tileButtons.All(t=>t.Value.Content is TileArt art && Vm.Game.Tiles.TryGetValue(t.Key,out var s) && art.Symbol==s))
        {
            foreach(var (id,tile) in tileButtons)
            {
                var art=(TileArt)tile.Content;bool hint=Vm.Highlight is Pair pair && (pair.A==id || pair.B==id);
                bool changed=art.Selected!=(Vm.Selected==id) || art.Hint!=hint;
                art.Selected=Vm.Selected==id;
                if(art.Hint!=hint)
                {
                    art.BeginAnimation(OpacityProperty,null);
                    if(hint && !Vm.Preferences.ReducedMotion)art.BeginAnimation(OpacityProperty,new DoubleAnimation(.6,1,TimeSpan.FromMilliseconds(500)){AutoReverse=true,RepeatBehavior=new RepeatBehavior(2)});
                }
                art.Hint=hint;if(changed)art.InvalidateVisual();
                AutomationProperties.SetName(tile,SymbolName(art.Symbol)+", "+T(art.Blocked?"locked":"free")+(art.Selected?", "+T("selected"):""));
            }
            return;
        }
        renderedGame=Vm.Game.GameId;renderedZoom=Vm.Preferences.Zoom;
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
        AddBoardViewport(canvas);
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
            p.Children.Add(Text(T(Vm.Results.Any(r=>r.GameId==Vm.Game!.GameId)?"resultSaved":"resultSaveError"),14,"Accent"));
            if(!Vm.ResultSaved)
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
        var page=Page(T("settings"),T("settingsIntro"));
        var cols=new Grid();cols.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});cols.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});
        var left=new StackPanel{Margin=new(0,0,10,0)};var right=new StackPanel{Margin=new(10,0,0,0)};Grid.SetColumn(right,1);cols.Children.Add(left);cols.Children.Add(right);
        var appearance=new StackPanel();
        appearance.Children.Add(Field(T("language"),Combo(["pl","en"],Vm.Preferences.Language,id=>Vm.ChangeLanguage(id),id=>id=="pl"?"Polski":"English")));
        appearance.Children.Add(Field(T("theme"),Combo(["light","dark","contrast"],Vm.Preferences.Theme,id=>{Vm.Preferences.Theme=id;Vm.SavePreferences();ApplyTheme();RenderScreen();})));
        appearance.Children.Add(Field(T("tileSet"),Combo(Catalog.Sets,Vm.Preferences.Set,id=>{Vm.Preferences.Set=id;Vm.SavePreferences();})));
        appearance.Children.Add(Text(T("nextGame"),13,"Muted"));
        appearance.Children.Add(Check("labels",Vm.Preferences.Labels,v=>Vm.Preferences.Labels=v));appearance.Children.Add(Check("reducedMotion",Vm.Preferences.ReducedMotion,v=>Vm.Preferences.ReducedMotion=v));
        left.Children.Add(Section(T("appearance"),appearance));
        var audio=new StackPanel();audio.Children.Add(Check("mute",Vm.Preferences.Muted,v=>Vm.Preferences.Muted=v));audio.Children.Add(Text(T("volume"),14,"Muted"));
        var slider=new Slider{Minimum=0,Maximum=1,Value=Vm.Preferences.Volume,TickFrequency=.1,IsSnapToTickEnabled=true};AutomationProperties.SetName(slider,T("volume"));
        slider.ValueChanged+=(_,_)=>{Vm.Preferences.Volume=slider.Value;Vm.SavePreferences();};slider.PreviewMouseLeftButtonUp+=(_,_)=>Vm.Play("select");audio.Children.Add(slider);left.Children.Add(Section(T("sound"),audio));
        var backgrounds=new StackPanel();backgrounds.Children.Add(Text(T("backgroundIntro"),13,"Muted"));var swatches=new WrapPanel{Margin=new(0,12,0,8)};
        foreach(var id in new[]{"felt","wood","forest","paper","minimal"})
        {
            var preview=new StackPanel();preview.Children.Add(new Border{Width=112,Height=52,Background=BackgroundArt.Create(id),CornerRadius=new(6)});
            var label=Text(T(id),12);label.Margin=new(0,8,0,0);preview.Children.Add(label);
            if(Vm.Preferences.Background==id)preview.Children.Add(Text(T("chosen"),12,"AccentText"));
            var choose=Button("",()=>{Vm.Preferences.Background=id;Vm.SavePreferences();RenderScreen();});choose.Content=preview;choose.Padding=new(10);AutomationProperties.SetName(choose,T(id)+(Vm.Preferences.Background==id?", "+T("chosen"):""));
            if(Vm.Preferences.Background==id){choose.BorderBrush=R("AccentText");choose.Background=R("SelectedSurface");}swatches.Children.Add(choose);
        }
        backgrounds.Children.Add(swatches);backgrounds.Children.Add(Button(T("chooseBackground"),ChooseBackground));backgrounds.Children.Add(Text(T("customBackgroundHelp"),13,"Muted"));
        if(Vm.CustomBackground.Available)
        {
            var preview=new StackPanel{Orientation=Orientation.Horizontal};preview.Children.Add(new Border{Width=84,Height=44,Background=Vm.CustomBackground.Brush,CornerRadius=new(5),Margin=new(0,0,12,0)});preview.Children.Add(Text(T("customBackground"),14));
            var select=Button("",()=>{Vm.Preferences.Background="custom";Vm.SavePreferences();RenderScreen();});select.Content=preview;AutomationProperties.SetName(select,T("customBackground"));if(Vm.Preferences.Background=="custom")select.BorderBrush=R("AccentText");backgrounds.Children.Add(select);
        }
        right.Children.Add(Section(T("background"),backgrounds));
        var backup=new StackPanel();backup.Children.Add(Text(T("backupHelp"),13,"Muted"));var files=new WrapPanel{Margin=new(0,12,0,0)};files.Children.Add(Button(T("exportData"),ExportData));files.Children.Add(Button(T("importData"),ImportData));backup.Children.Add(files);right.Children.Add(Section(T("dataBackup"),backup));
        page.Children.Add(cols);
        var status=Text("",13,"Muted");Bind(status,TextBlock.TextProperty,nameof(Vm.Status));page.Children.Add(status);
        var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};actions.Children.Add(Button(T("back"),()=>Vm.Navigate(settingsReturn)));page.Children.Add(actions);DockActions(page,actions);
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
        var p=Page(T("helpTitle"));p.Children.Add(Button(T("tutorial"),()=>{tutorialStep=0;tutorialGame=null;Vm.Navigate("tutorial");},true));var text=Text(T("helpText"),17);text.LineHeight=27;text.MaxWidth=900;text.HorizontalAlignment=HorizontalAlignment.Left;p.Children.Add(text);p.Children.Add(Button(T("back"),()=>Vm.Navigate("menu"),true));
    }
    void OpenSettings() {if(Vm.Busy)return;if(Vm.Screen!="settings")settingsReturn=Vm.Screen;Vm.Navigate("settings");}
    void Language_Click(object sender,RoutedEventArgs e) {if(!confirming)Vm.ChangeLanguage(Vm.Preferences.Language=="pl"?"en":"pl");}
    void Settings_Click(object sender,RoutedEventArgs e) {if(!confirming)OpenSettings();}
    void Fullscreen_Click(object sender,RoutedEventArgs e)=>Fullscreen();
    void Fullscreen(){ if(WindowStyle==WindowStyle.None){WindowStyle=WindowStyle.SingleBorderWindow;WindowState=WindowState.Normal;}else{WindowStyle=WindowStyle.None;WindowState=WindowState.Maximized;} }
    void Window_KeyDown(object sender,KeyEventArgs e)
    {
        if(e.Key==Key.F11){Fullscreen();e.Handled=true;return;}
        if(e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase || Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase)return;
        if(Vm.Screen!="game" || confirming || Vm.Busy)return;
        if(e.Key==Key.Z && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Vm.CanUndo){Vm.Undo();e.Handled=true;return;}
        if(e.Key==Key.Escape){Vm.TogglePause();e.Handled=true;return;}
        if(e.Key==Key.H && Vm.Playing){Vm.Hint();e.Handled=true;return;}
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
