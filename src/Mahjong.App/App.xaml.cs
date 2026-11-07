using System.IO;
using System.Windows;
using Mahjong.Core;

namespace Mahjong.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    InstanceLease? lease;
    EventWaitHandle? activation;
    RegisteredWaitHandle? listener;
    protected override void OnStartup(StartupEventArgs e)
    {
        string directory = e.Args.Contains("--smoke") ? Path.Combine(AppContext.BaseDirectory, "SmokeData")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuietMahjong");
        try
        {
            lease = InstanceLease.TryAcquire(directory);
            activation = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\QuietMahjong-" + InstanceLease.Key(directory));
            if (lease == null) { activation.Set(); Shutdown(0); return; }
            listener = ThreadPool.RegisterWaitForSingleObject(activation, (_, _) => Dispatcher.BeginInvoke(new Action(() =>
            {
                if (MainWindow == null) return;
                if (MainWindow.WindowState == WindowState.Minimized) MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
            })), null, Timeout.Infinite, false);
            base.OnStartup(e);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show("Nie można otworzyć danych gry. / Cannot open game data.", "Mahjong Solitaire");
            Shutdown(1);
        }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        listener?.Unregister(null); activation?.Dispose(); lease?.Dispose(); base.OnExit(e);
    }
}
