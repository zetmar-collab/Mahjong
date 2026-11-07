using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Mahjong.Core;

namespace Mahjong.App;

internal static class Smoke
{
    internal static async Task Run(MainWindow window, GameViewModel vm)
    {
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--smoke-output");
        string output=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(AppContext.BaseDirectory,"SmokeResults");
        Directory.CreateDirectory(output);List<string> log=[];int assertions=0;
        void Check(bool condition,string name){if(!condition)throw new Exception(name);assertions++;log.Add("PASS "+name);File.AppendAllText(Path.Combine(output,"progress.txt"),"PASS "+name+Environment.NewLine); }
        async Task Render(string name,double density=1)
        {
            await window.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();var visual=(FrameworkElement)window;
            var bitmap=new RenderTargetBitmap((int)Math.Ceiling(visual.ActualWidth*density),(int)Math.Ceiling(visual.ActualHeight*density),96*density,96*density,PixelFormats.Pbgra32);
            bitmap.Render(visual);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream=File.Create(Path.Combine(output,name+".png"));encoder.Save(stream);
            Check(bitmap.PixelWidth>900 && bitmap.PixelHeight>500,"Render "+name+" "+bitmap.PixelWidth+"x"+bitmap.PixelHeight);
        }
        try
        {
            Check(window.Icon!=null,"Window icon is loaded from embedded resource");
            var textureVisual=new DrawingVisual();
            using(var drawing=textureVisual.RenderOpen())drawing.DrawRectangle(BackgroundArt.Create("forest"),null,new Rect(0,0,160,100));
            var textureBitmap=new RenderTargetBitmap(160,100,96,96,PixelFormats.Pbgra32);textureBitmap.Render(textureVisual);
            var texturePixels=new byte[160*100*4];textureBitmap.CopyPixels(texturePixels,160*4,0);
            Check(Enumerable.Range(0,160*100).All(i=>texturePixels[i*4+3]==255),"Forest texture fills its entire bounds without transparent edges");
            using(var duplicate=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(AppContext.BaseDirectory,"Mahjong.exe"),"--smoke"){UseShellExecute=false,CreateNoWindow=true})!)
            {
                await duplicate.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Check(duplicate.ExitCode==0,"Second application instance exits before opening shared data");
            }
            int initialResults=vm.Results.Count;
            vm.Preferences.Muted=true;vm.ChangeLanguage("pl");await Render("menu-pl");
            vm.Navigate("new");await Render("new-game-pl");
            foreach(var layout in vm.Layouts)
            {
                vm.Preferences.Layout=layout.Id;vm.Preferences.Set="classic";
                vm.Preferences.ReducedMotion=layout.Id=="stairs";
                var create=vm.NewGame();var duplicate=vm.NewGame();await Task.WhenAll(create,duplicate);
                Check(vm.Game?.LayoutId==layout.Id && vm.Game.Tiles.Count==layout.Count,"New game "+layout.Id+" ignores concurrent request");
                Check(Generator.Replay(vm.Game!,vm.Game!.Solution),"Full control solution "+layout.Id);
                vm.Hint();Check(vm.Highlight!=null && Rules.Pairs(vm.Game).Contains(vm.Highlight),"Hint "+layout.Id);
                var selected=vm.Highlight!;vm.Choose(selected.A);Check(vm.Selected==selected.A,"Select "+layout.Id);vm.Choose(selected.A);Check(vm.Selected==null,"Deselect "+layout.Id);
                if(layout.Id=="turtle")
                {
                    foreach(var (w,h) in new[]{(1280d,720d),(1920d,1080d)})
                    {
                        window.Width=w;window.Height=h;
                        foreach(var density in new[]{1d,1.5,2d})await Render($"turtle-{w}x{h}-{density*100}pct",density);
                    }
                    window.Width=1280;window.Height=720;
                }
                await Render("layout-"+layout.Id);
                // Complete a whole game through the same selection method used by tile buttons.
                foreach(var pair in vm.Game.Solution.ToArray())
                {
                    int before=vm.Game.Tiles.Count;vm.Choose(pair.A);vm.Choose(pair.B);vm.Choose(pair.A);vm.Choose(pair.B);
                    Check(vm.Game.Tiles.Count==before-2,"Removal is idempotent "+layout.Id+" "+pair.A);
                    await window.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.Render);
                }
                Check(vm.Game.Tiles.Count==0 && !vm.Playing,"Victory "+layout.Id);
                Check(vm.Results.Any(r=>r.GameId==vm.Game.GameId && r.FinishedAt==vm.Game.CompletedAt),"Victory automatically records actual completion time "+layout.Id);
                Check(HasCelebration(window)==!vm.Preferences.ReducedMotion,"Victory animation respects reduced motion "+layout.Id);
                await Task.Delay(450);await Render("won-"+layout.Id);
                var nameBox=Descendants(window).OfType<TextBox>().First();
                var nameKey=new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,PresentationSource.FromVisual(window),0,System.Windows.Input.Key.H){RoutedEvent=System.Windows.Input.Keyboard.PreviewKeyDownEvent};
                nameBox.RaiseEvent(nameKey);Check(!nameKey.Handled,"Winner name accepts H "+layout.Id);
                Check(vm.SaveResult("Łukasz test"),"Save finished result "+layout.Id);
                Check(!vm.SaveResult("Duplicate"),"Repeated result save ignored "+layout.Id);
                window.RefreshAppearance();Check(!HasCelebration(window),"Victory animation is not replayed on redraw "+layout.Id);
            }
            Check(vm.Results.Count==initialResults+6,"Six completed games recorded");
            vm.FlushSaves();var history=new GameViewModel(vm.DataDirectory);Check(history.Results.Count==vm.Results.Count,"Result history survives reopening");history.Close(false);
            vm.Navigate("results");await Render("saved-results-pl");vm.ChangeLanguage("en");await Render("saved-results-en");
            vm.Preferences.ReducedMotion=false;
            vm.Preferences.Layout="turtle";vm.Preferences.Set="nature";await vm.NewGame();
            var initial=JsonSerializer.Serialize(vm.Game!.Tiles);var gameId=vm.Game.GameId;int remaining=vm.Game.Tiles.Count;
            vm.TogglePause();double pausedTime=vm.Game.Seconds;await Task.Delay(550);vm.Tick();Check(vm.Game.Seconds==pausedTime,"Pause stops active clock");await Render("pause");
            vm.ChangeLanguage("en");Check(vm.Paused && vm.Game.GameId==gameId && JsonSerializer.Serialize(vm.Game.Tiles)==initial,"Language preserves paused game");await Render("pause-en");vm.TogglePause();
            vm.Hint();var hintPair=vm.Highlight!;vm.Choose(hintPair.A);vm.Choose(hintPair.B);
            Check(vm.CanUndo,"Undo enabled after a move");
            double beforeUndo=vm.Game.Seconds;int beforeHints=vm.Game.Hints;vm.Undo();
            Check(vm.Game.Tiles.Count==remaining && vm.Game.UndoCount==1 && vm.Game.Hints==beforeHints && vm.Game.Seconds>=beforeUndo,"Undo restores pair and preserves time and assistance");
            vm.Choose(hintPair.A);vm.Choose(hintPair.B);
            var beforeShuffle=vm.Game.Tiles.Values.GroupBy(s=>s.Id).ToDictionary(g=>g.Key,g=>g.Count());double seconds=vm.Game.Seconds;int hints=vm.Game.Hints;
            await Task.WhenAll(vm.Shuffle(),vm.Shuffle(),vm.Shuffle());
            Check(vm.Game.ShuffleUsed && vm.Game.Tiles.Count==remaining-2,"Concurrent shuffle consumes only one allowance");
            Check(vm.Game.Hints==hints && vm.Game.Seconds>=seconds,"Shuffle keeps counters and clock");
            Check(beforeShuffle.All(g=>vm.Game.Tiles.Values.Count(s=>s.Id==g.Key)==g.Value),"Shuffle preserves distinct symbols");
            Check(Generator.Replay(vm.Game,vm.Game.Solution),"Shuffled state has full solution");vm.Save();vm.FlushSaves();
            var reload=new GameViewModel(vm.DataDirectory);Check(reload.Game?.ShuffleUsed==true && !reload.CanShuffle,"Reload preserves used allowance");reload.Close(false);
            var state=JsonSerializer.Serialize(vm.Game.Tiles);await vm.Shuffle();Check(state==JsonSerializer.Serialize(vm.Game.Tiles),"Second shuffle ignored");
            vm.Preferences.Set="geometry";Check(vm.Game.SetId=="nature","Next-set preference does not mutate current set");
            var pixels=new byte[640*360*4];
            for(int pixel=0;pixel<pixels.Length;pixel+=4){pixels[pixel]=95;pixels[pixel+1]=(byte)(80+pixel/4/640%100);pixels[pixel+2]=35;pixels[pixel+3]=255;}
            var sample=BitmapSource.Create(640,360,96,96,PixelFormats.Bgra32,null,pixels,640*4);
            foreach(var encoder in new BitmapEncoder[]{new PngBitmapEncoder(),new JpegBitmapEncoder(),new BmpBitmapEncoder(),new GifBitmapEncoder(),new TiffBitmapEncoder()})
            {
                string source=Path.Combine(output,"tło test-"+encoder.GetType().Name+".image");
                encoder.Frames.Add(BitmapFrame.Create(sample));using(var stream=File.Create(source))encoder.Save(stream);
                Check(await vm.ImportBackground(source),"Import "+encoder.GetType().Name);File.Delete(source);
                Check(vm.CustomBackground.Available && File.Exists(vm.CustomBackground.FilePath),"Imported image independent of source "+encoder.GetType().Name);
            }
            vm.FlushSaves();var reloadedBackground=new GameViewModel(vm.DataDirectory);
            Check(reloadedBackground.Preferences.Background=="custom" && reloadedBackground.CustomBackground.Available,"Custom image persists across reopening");
            reloadedBackground.Close(false);
            var previous=File.ReadAllBytes(vm.CustomBackground.FilePath);var previousBrush=vm.CustomBackground.Brush;
            string bad=Path.Combine(output,"invalid-image.bin");File.WriteAllText(bad,"not an image");
            Check(!await vm.ImportBackground(bad),"Invalid image rejected");
            Check(previous.SequenceEqual(File.ReadAllBytes(vm.CustomBackground.FilePath)) && ReferenceEquals(previousBrush,vm.CustomBackground.Brush) && vm.Preferences.Background=="custom","Invalid import preserves existing image and setting");
            string big=Path.Combine(output,"large-image.bin");using(var stream=File.Create(big))stream.SetLength(32*1024*1024+1);
            Check(!await vm.ImportBackground(big),"Oversized image rejected before decoding");File.Delete(big);File.Delete(bad);
            File.Delete(vm.CustomBackground.FilePath);
            var missingBackground=new GameViewModel(vm.DataDirectory);
            Check(missingBackground.Preferences.Background=="felt" && !missingBackground.CustomBackground.Available,"Missing saved background falls back without crashing");
            missingBackground.Close(false);
            File.WriteAllBytes(vm.CustomBackground.FilePath,previous);
            vm.FlushSaves();
            string backupFile=Path.Combine(output,"roundtrip.mahjongbackup"),importFolder=Path.Combine(output,"backup-import-"+Guid.NewGuid().ToString("N"));
            BackupService.Export(vm.DataDirectory,backupFile);
            BackupService.Import(backupFile,importFolder,vm.Layouts,path=>{var image=new CustomBackground(Path.GetDirectoryName(path)!);if(!image.Load())throw new InvalidDataException();});
            var imported=new GameViewModel(importFolder);
            Check(imported.Game?.GameId==vm.Game.GameId && imported.Results.Count==vm.Results.Count && imported.CustomBackground.Available,"Full backup imports game, results, settings and decoded image");imported.Close(false);
            var imageDialog=new Microsoft.Win32.OpenFileDialog {Filter=vm.Text["imageFilter"]};
            Check(imageDialog.Filter.Contains("*.jpg") && imageDialog.Filter.Contains("*.png"),"Image picker filter is valid and includes supported formats");
            vm.Navigate("game");await Render("custom-background");vm.Navigate("settings");await Render("custom-background-settings");
            foreach(var id in new[]{"felt","wood","forest","paper","minimal"}){vm.Preferences.Background=id;vm.Navigate("game");await Render("background-"+id);}
            foreach(var theme in new[]{"light","dark","contrast"})
            {
                vm.Preferences.Theme=theme;window.RefreshAppearance();vm.Navigate("game");await Render("theme-"+theme);
                vm.Navigate("settings");await Render("settings-"+theme);
                foreach(var (ink,paper) in new[]{("Text","Surface"),("Text","Panel"),("Muted","Panel"),("Text","Input"),("AccentText","Panel"),("AccentInk","Accent")})
                {
                    var foreground=((SolidColorBrush)Application.Current.Resources[ink]).Color;
                    var background=((SolidColorBrush)Application.Current.Resources[paper]).Color;
                    Check(Contrast(foreground,background)>=4.5,"Readable theme contrast "+theme+" "+ink+"/"+paper);
                }
                var combo=Descendants(window).OfType<ComboBox>().First();combo.IsDropDownOpen=true;
                await window.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
                var popup=(System.Windows.Controls.Primitives.Popup)combo.Template.FindName("PART_Popup",combo);
                Check(popup.IsOpen && popup.Child is Border border && border.Background==Application.Current.Resources["Panel"],"Dropdown uses current theme "+theme);
                combo.IsDropDownOpen=false;
            }
            vm.Preferences.Theme="dark";vm.Preferences.Background="felt";window.RefreshAppearance();
            vm.Navigate("help");await Render("help-en");vm.ChangeLanguage("pl");await Render("help-pl");
            vm.Navigate("game");await vm.NewGame(true);Check(!vm.Game!.ShuffleUsed && JsonSerializer.Serialize(vm.Game.Tiles)==JsonSerializer.Serialize(vm.Game.Initial),"Replay resets original deal and allowance");
            vm.Preferences.Zoom=2;window.RefreshAppearance();await Render("zoom-200");
            Check(Descendants(window).OfType<ScrollViewer>().Any(s=>s.HorizontalScrollBarVisibility==ScrollBarVisibility.Auto && s.ScrollableWidth>0),"Zoomed board can pan horizontally");
            vm.Preferences.Zoom=1;window.RefreshAppearance();
            foreach(int lesson in Enumerable.Range(0,3))
            {
                var exercise=MainWindow.CreateLesson(lesson);int iterations=0;
                while(exercise.Tiles.Count>0 && iterations++<5){var pair=Rules.Pairs(exercise).First();Check(Rules.Remove(exercise,pair.A,pair.B),"Tutorial uses legal pair "+lesson);}
                Check(exercise.Tiles.Count==0,"Tutorial exercise can be completed "+lesson);
            }
            vm.Navigate("tutorial");await Render("tutorial-pl");vm.ChangeLanguage("en");await Render("tutorial-en");
            await vm.NewGame(daily:new DateOnly(2026,11,7));Check(vm.Game!.ChallengeDate=="2026-11-07","Daily challenge opens in real WPF view");
            string code=DealCode.Encode(vm.Game);var dailyId=vm.Game.GameId;await vm.NewGame(dealCode:code);
            Check(vm.Game!.GameId!=dailyId && DealCode.Encode(vm.Game)==code && vm.Game.ChallengeDate==null,"Code starts independent copy of original deal");
            await Render("code-game-en");
            vm.Navigate("new");
            int Handlers()=>((MulticastDelegate?)typeof(GameViewModel).GetField("PropertyChanged",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(vm))?.GetInvocationList().Length??0;
            int count=Handlers();for(int i=0;i<20;i++)window.RefreshAppearance();Check(Handlers()==count,"Repeated new-game redraws do not accumulate handlers");
            await window.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);window.UpdateLayout();
            var primary=Descendants(window).OfType<Button>().First(b=>b.Style==window.FindResource("PrimaryButton"));
            var primaryText=Descendants(primary).OfType<TextBlock>().First();
            Check(primaryText.Foreground==Application.Current.Resources["AccentInk"],"Primary action text inherits contrasting ink");
            window.Width=1000;window.Height=680;await Render("new-game-minimum");
            var actionPosition=primary.TransformToAncestor(window).Transform(new Point());
            Check(actionPosition.Y+primary.ActualHeight<window.ActualHeight-30,"Start action remains visible at minimum window size");
            window.Width=1280;window.Height=720;
            vm.Navigate("results");await Render("results-filters-en");vm.Navigate("game");
            int existing=vm.Results.Count;
            var sampleResult=vm.Results[0];
            for(int i=0;i<5000;i++)vm.Results.Add(sampleResult with{GameId=Guid.NewGuid()});
            vm.Navigate("results");await window.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);window.UpdateLayout();
            Check(Descendants(window).OfType<ListBoxItem>().Count()<100,"Large result history virtualizes visible rows");
            vm.Results.RemoveRange(existing,5000);vm.Navigate("game");
            string failedPath=Path.Combine(output,"locked-save.json");File.WriteAllText(failedPath,"keep");int writeErrors=0;
            using(var locked=new FileStream(failedPath,FileMode.Open,FileAccess.Read,FileShare.None))
            {
                var writer=new SaveQueue(_=>Interlocked.Increment(ref writeErrors));writer.Enqueue(failedPath,new{Value="replace"});writer.Flush();
                Check(writeErrors==1,"Background writer reports locked destination without crashing");
            }
            Check(File.ReadAllText(failedPath)=="keep","Failed background write preserves previous data");
            vm.Preferences.Muted=false;
            foreach(var sound in new[]{"select","remove","blocked","hint","shuffle","win","stuck"})
            {
                var data=File.ReadAllBytes(Path.Combine(vm.AssetDirectory,"audio",sound+".wav"));Check(data.Length>44 && System.Text.Encoding.ASCII.GetString(data,0,4)=="RIFF","Local WAV "+sound);vm.Play(sound);await Task.Delay(100);
            }
            log.Add($"RESULT: {assertions} WPF checks passed.");
            File.WriteAllLines(Path.Combine(output,"ui-tests.txt"),log);Application.Current.Shutdown(0);
        }
        catch(Exception ex)
        {
            log.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(output,"ui-tests.txt"),log);Application.Current.Shutdown(1);
        }
    }
    static bool HasCelebration(DependencyObject parent)
    {
        if(parent is VictoryCelebration)return true;
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)if(HasCelebration(VisualTreeHelper.GetChild(parent,i)))return true;
        return false;
    }
    static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)
        {
            var child=VisualTreeHelper.GetChild(parent,i);yield return child;
            foreach(var nested in Descendants(child))yield return nested;
        }
    }
    static double Contrast(Color first,Color second)
    {
        double Luminance(Color color)
        {
            double Channel(byte c){double v=c/255d;return v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);}
            return .2126*Channel(color.R)+.7152*Channel(color.G)+.0722*Channel(color.B);
        }
        double a=Luminance(first),b=Luminance(second);return (Math.Max(a,b)+.05)/(Math.Min(a,b)+.05);
    }
}
