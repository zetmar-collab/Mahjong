using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using Mahjong.Core;

namespace Mahjong.App;

public partial class MainWindow
{
    bool familyMode;
    int familyCount = 2;
    readonly string[] familyNames = ["", "", "", ""];
    string[]? NewPlayers => familyMode ? familyNames.Take(familyCount).ToArray() : null;
    void OpenFamilyGame() { familyMode = true; Vm.Navigate("new"); }

    void AddFamilySetup(StackPanel page)
    {
        var modes = new WrapPanel { Margin = new(0, 14, 0, 0) };
        modes.Children.Add(Button(T("soloMode"), () => { familyMode = false; RenderScreen(); }, !familyMode));
        modes.Children.Add(Button(T("familyMode"), () => { familyMode = true; RenderScreen(); }, familyMode));
        page.Children.Add(modes);
        if (!familyMode) return;
        var panel = new StackPanel(); panel.Children.Add(Text(T("familyRules"), 14, "Muted"));
        var count = Combo(["2", "3", "4"], familyCount.ToString(), id => { familyCount = int.Parse(id); RenderScreen(); }, id => id);
        panel.Children.Add(Field(T("playerCount"), count));
        var names = new System.Windows.Controls.Primitives.UniformGrid { Columns = familyCount };
        for (int i = 0; i < familyCount; i++)
        {
            int index = i;
            if (string.IsNullOrWhiteSpace(familyNames[i])) familyNames[i] = T("playerDefault") + " " + (i + 1);
            var input = new TextBox { Text = familyNames[i], MaxLength = 24, Margin = new(0, 4, 10, 0) };
            input.TextChanged += (_, _) => familyNames[index] = input.Text;
            names.Children.Add(Field(T("playerDefault") + " " + (i + 1), input));
        }
        panel.Children.Add(names); page.Children.Add(Section(T("familyTable"), panel));
    }
    FrameworkElement FamilyScoreboard()
    {
        var grid = new Grid { Margin = new(0, 0, 0, 8) };
        grid.ColumnDefinitions.Add(new()); grid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        var text = new StackPanel();
        var turn = Text("", 19, "AccentText"); turn.FontWeight = FontWeights.SemiBold;
        Bind(turn, TextBlock.TextProperty, nameof(Vm.FamilyTurn)); text.Children.Add(turn);
        var scores = Text("", 15); scores.Margin = new(0, 5, 0, 0);
        Bind(scores, TextBlock.TextProperty, nameof(Vm.FamilySummary)); text.Children.Add(scores); grid.Children.Add(text);
        var pass = Button(T("passTurn"), Vm.PassTurn); Bind(pass, IsEnabledProperty, nameof(Vm.Playing));
        Grid.SetColumn(pass, 1); grid.Children.Add(pass);
        return new Border { Child = grid, Background = R("Panel"), CornerRadius = new(12), Padding = new(18, 12, 18, 6), Margin = new(0, 0, 0, 8) };
    }
}
