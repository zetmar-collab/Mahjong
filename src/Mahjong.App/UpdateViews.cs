using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Mahjong.Core;

namespace Mahjong.App;

public partial class MainWindow
{
    static Version AppVersion => typeof(MainWindow).Assembly.GetName().Version ?? new(1, 3, 0);
    readonly HttpClient updateHttp = new() { Timeout = TimeSpan.FromMinutes(5) };
    readonly CancellationTokenSource updateLifetime = new();
    readonly DispatcherTimer updateTimer = new() { Interval = TimeSpan.FromHours(6) };
    ReleaseUpdate? availableUpdate;
    bool checkingUpdates, updateDialogOpen;
    string updateMessage = "updateReady";

    void StartUpdateChecks()
    {
        updateTimer.Tick += (_, _) => { if (Vm.Preferences.CheckUpdates) _ = CheckForUpdates(); };
        updateTimer.Start();
        if (Vm.Preferences.CheckUpdates) _ = CheckForUpdates();
    }
    void StopUpdateChecks() { updateTimer.Stop(); updateLifetime.Cancel(); updateHttp.Dispose(); }
    async Task CheckForUpdates(bool manual = false)
    {
        if (checkingUpdates || updateLifetime.IsCancellationRequested) return;
        checkingUpdates = true; updateMessage = "updateChecking";
        if (manual && Vm.Screen == "settings") RenderScreen();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(updateLifetime.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            var next = await new UpdateService(updateHttp).CheckAsync(AppVersion, timeout.Token);
            PresentUpdate(next);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException or System.Text.Json.JsonException or InvalidOperationException or ArgumentException or KeyNotFoundException or FormatException or OverflowException)
        { updateMessage = "updateCheckError"; }
        finally
        {
            checkingUpdates = false;
            if (!updateLifetime.IsCancellationRequested && Vm.Screen == "settings") RenderScreen();
        }
    }
    internal void PresentUpdate(ReleaseUpdate? release)
    {
        availableUpdate = release; updateMessage = release == null ? "updateCurrent" : "updateAvailable";
        RenderUpdateBanner();
    }
    void RenderUpdateBanner()
    {
        UpdateBanner.Visibility = availableUpdate == null ? Visibility.Collapsed : Visibility.Visible;
        if (availableUpdate == null) return;
        var panel = new DockPanel();
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(Button(T("updateInstall"), ShowUpdateDialog, true));
        actions.Children.Add(Button(T("updateLater"), () => UpdateBanner.Visibility = Visibility.Collapsed));
        DockPanel.SetDock(actions, Dock.Right); panel.Children.Add(actions);
        var text = Text(T("updateAvailable") + " " + availableUpdate.Version, 15);
        text.VerticalAlignment = VerticalAlignment.Center; panel.Children.Add(text); UpdateBanner.Child = panel;
    }
    FrameworkElement UpdateSettings()
    {
        var panel = new StackPanel();
        panel.Children.Add(Text(T("installedVersion") + " " + AppVersion.ToString(3), 14));
        panel.Children.Add(Check("checkUpdates", Vm.Preferences.CheckUpdates, enabled => {
            Vm.Preferences.CheckUpdates = enabled;
            if (enabled) _ = CheckForUpdates();
        }));
        panel.Children.Add(Text(T("updatePrivacy"), 13, "Muted"));
        panel.Children.Add(Text(T(updateMessage) + (availableUpdate == null ? "" : " " + availableUpdate.Version), 14, "AccentText"));
        var check = Button(T("updateCheck"), () => _ = CheckForUpdates(true)); check.IsEnabled = !checkingUpdates; panel.Children.Add(check);
        if (availableUpdate != null) panel.Children.Add(Button(T("updateInstall"), ShowUpdateDialog, true));
        return Section(T("updates"), panel);
    }
    void ShowUpdateDialog()
    {
        if (availableUpdate == null || Vm.Busy || updateDialogOpen) return;
        updateDialogOpen = true;
        var release = availableUpdate;
        bool resume = Vm.Playing, installing = false;
        if (resume) Vm.TogglePause();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(updateLifetime.Token);
        var dialog = new Window { Owner = this, Title = T("updates"), Width = 570, SizeToContent = SizeToContent.Height,
            ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = R("Surface") };
        if (Environment.GetCommandLineArgs().Contains("--smoke"))
        { dialog.ShowActivated = false; dialog.ShowInTaskbar = false; dialog.WindowStartupLocation = WindowStartupLocation.Manual; dialog.Left = dialog.Top = -10000; }
        var panel = new StackPanel { Margin = new(24) };
        panel.Children.Add(TitleText(T("updateAvailable") + " " + release.Version, 28));
        panel.Children.Add(Text(T("updateExplain"), 15));
        var status = Text(T("updateDownloadSize") + " " + (release.Size / 1048576.0).ToString("0.0") + " MB", 14, "Muted");
        status.Margin = new(0, 16, 0, 8); panel.Children.Add(status);
        var progress = new ProgressBar { Minimum = 0, Maximum = 100, Height = 8, Margin = new(0, 0, 0, 12) }; panel.Children.Add(progress);
        string? installer = null;
        var download = Button(T("updateDownload"), () => { }, true);
        var install = Button(T("updateApply"), () => { }, true); install.Visibility = Visibility.Collapsed;
        var back = Button(T("updateLater"), () => dialog.Close()); back.IsCancel = true;
        download.Click += async (_, _) =>
        {
            download.IsEnabled = false; status.Text = T("updateDownloading"); back.Content = T("updateCancel");
            try
            {
                installer = await new UpdateService(updateHttp).DownloadAsync(release, Vm.DataDirectory, new Progress<double>(p => progress.Value = p), cancel.Token);
                if (cancel.IsCancellationRequested) { File.Delete(installer); return; }
                status.Text = T("updateVerified"); download.Visibility = Visibility.Collapsed;
                install.Visibility = Visibility.Visible; back.Content = T("updateLater");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException or InvalidOperationException)
            { status.Text = T("updateDownloadError"); download.IsEnabled = true; back.Content = T("updateLater"); }
        };
        install.Click += async (_, _) =>
        {
            if (installer == null) return;
            install.IsEnabled = false;
            try
            {
                if (!await UpdateService.VerifyAsync(installer, release, cancel.Token)) throw new InvalidDataException();
                // Flush before handing off to the existing per-user installer.
                Vm.Tick(); Vm.Save(); Vm.SavePreferences(); Vm.FlushSaves();
                using var started = Process.Start(new ProcessStartInfo(installer) { UseShellExecute = true });
                if (started == null) throw new InvalidOperationException();
                installing = true; dialog.Close(); Close();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
            { status.Text = T("updateLaunchError"); install.IsEnabled = true; }
        };
        panel.Children.Add(download); panel.Children.Add(install); panel.Children.Add(back);
        dialog.Content = panel;
        dialog.Closed += (_, _) => cancel.Cancel();
        try { dialog.ShowDialog(); }
        finally
        {
            updateDialogOpen = false;
            if (resume && !installing && Vm.Paused) Vm.TogglePause();
            if (!installing && installer != null)
                try { File.Delete(installer); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }
}
