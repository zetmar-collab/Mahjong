using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Mahjong.App;

public partial class MainWindow
{
    FrameworkElement Field(string label, Control control)
    {
        var panel = new StackPanel();
        var caption = new TextBlock { Text = label, Foreground = R("Muted"), FontSize = 12, Margin = new(0, 4, 0, 0) };
        AutomationProperties.SetName(control, label);
        AutomationProperties.SetLabeledBy(control, caption);
        panel.Children.Add(caption); panel.Children.Add(control); return panel;
    }
    Border Section(string title, UIElement content)
    {
        var panel = new StackPanel();
        var heading = Text(title, 20); heading.FontWeight = FontWeights.SemiBold; heading.Margin = new(0, 0, 0, 16);
        panel.Children.Add(heading); panel.Children.Add(content);
        return new Border { Child = panel, Background = R("Panel"), BorderBrush = R("Line"), BorderThickness = new(1), CornerRadius = new(16), Padding = new(20), Margin = new(0, 16, 0, 0) };
    }
    void DockActions(StackPanel page, Panel actions)
    {
        page.Children.Remove(actions);
        var scroll = (ScrollViewer)ContentArea.Children[0]; ContentArea.Children.Remove(scroll);
        var root = new Grid(); root.RowDefinitions.Add(new()); root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.Children.Add(scroll);
        var footer = new Border { Child = actions, BorderBrush = R("Line"), BorderThickness = new(0, 1, 0, 0), Padding = new(20, 8, 20, 0), Background = R("Surface") };
        Grid.SetRow(footer, 1); root.Children.Add(footer); ContentArea.Children.Add(root);
    }
}
