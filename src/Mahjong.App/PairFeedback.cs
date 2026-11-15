using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Mahjong.App;

public partial class MainWindow
{
    void ShowPairFeedback()
    {
        var host = boardHost!;
        var label = Text(Vm.Game!.Family == null ? T("pairFound") : "+10", 22, "AccentText");
        label.FontWeight = FontWeights.SemiBold;
        var movement = new TranslateTransform();
        var toast = new Border { Child = label, Background = R("Panel"), CornerRadius = new(14), Padding = new(20, 10, 20, 10),
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top,
            Margin = new(16), IsHitTestVisible = false, RenderTransform = movement };
        host.Children.Add(toast);
        movement.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(220)));
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(350)) { BeginTime = TimeSpan.FromMilliseconds(500) };
        fade.Completed += (_, _) => host.Children.Remove(toast);
        toast.BeginAnimation(OpacityProperty, fade);
    }
}
