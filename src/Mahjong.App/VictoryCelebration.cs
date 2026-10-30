using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Mahjong.App;

public sealed class VictoryCelebration : FrameworkElement
{
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(nameof(Progress), typeof(double), typeof(VictoryCelebration),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Progress { get => (double)GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    readonly (double X, double Delay, double Drift, double Size, double Spin, Brush Color)[] pieces;

    public VictoryCelebration(int seed)
    {
        IsHitTestVisible = false; ClipToBounds = true;
        var random = new Random(seed);
        string[] colors = ["#EBCB88", "#72D6BC", "#F49A86", "#B8C6FF", "#FFF2CC"];
        pieces = Enumerable.Range(0, 70).Select(_ => (random.NextDouble(), random.NextDouble() * .3,
            random.NextDouble() * 120 - 60, 4 + random.NextDouble() * 5, random.NextDouble() * 720,
            (Brush)new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[random.Next(colors.Length)])))).ToArray();
        BeginAnimation(ProgressProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(2.8)) { FillBehavior = FillBehavior.HoldEnd });
    }
    protected override void OnRender(DrawingContext drawing)
    {
        double progress = Progress;
        if (progress >= 1) return;
        drawing.PushOpacity(Math.Min(1, (1 - progress) * 4));
        foreach (var piece in pieces)
        {
            double t = Math.Max(0, (progress - piece.Delay) / (1 - piece.Delay));
            double x = piece.X * ActualWidth + Math.Sin(t * Math.PI * 2) * piece.Drift;
            double y = -20 + t * (ActualHeight + 60);
            drawing.PushTransform(new RotateTransform(piece.Spin * t, x, y));
            drawing.DrawRoundedRectangle(piece.Color, null, new Rect(x, y, piece.Size, piece.Size * 1.8), 1, 1);
            drawing.Pop();
        }
        drawing.Pop();
    }
}
