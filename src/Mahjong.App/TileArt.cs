using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Mahjong.Core;

namespace Mahjong.App;

public sealed class TileArt : FrameworkElement
{
    public Symbol Symbol { get; set; } = new("geometry1", "geometry1", "geometry", 1);
    public bool Blocked { get; set; }
    public bool Selected { get; set; }
    public bool Hint { get; set; }
    public bool Labels { get; set; } = true;
    public TileArt() { Width = 76; Height = 98; IsHitTestVisible = false; }
    static Brush B(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    static readonly string[] Colors = ["#207C70", "#A64445", "#385D92", "#956A26", "#704E8D", "#376847"];
    protected override void OnRender(DrawingContext d)
    {
        base.OnRender(d);
        var edge = Selected ? B("#2AAFA5") : Hint ? B("#E8AD34") : B("#B6A88B");
        d.DrawRoundedRectangle(B("#33000000"), null, new(4, 7, 70, 89), 7, 7);
        d.DrawRoundedRectangle(B("#B4A181"), null, new(0, 5, 70, 88), 7, 7);
        d.DrawRoundedRectangle(Blocked ? B("#E2DDCB") : B("#FFF9E8"), new(edge, Selected || Hint ? 3.5 : 1), new(0, 0, 70, 87), 7, 7);
        d.DrawLine(new(B("#FFFFFF"), 1), new(7, 4), new(63, 4));
        int shape = (Symbol.Rank - 1) % 12;
        Brush ink = Symbol.Family switch { "dragon" => B(Symbol.Rank == 1 ? "#A64445" : Symbol.Rank == 2 ? "#207C70" : "#385D92"), "characters" => B("#A64445"), "bamboo" => B("#207C70"), "wind" => B("#385D92"), _ => B(Colors[shape % Colors.Length]) };
        d.PushTransform(new TranslateTransform(11, 13));
        if (Symbol.Family == "geometry") Geometry(d, shape, ink);
        else if (Symbol.Family == "nature") Nature(d, shape, ink);
        else Classic(d, Symbol, ink);
        d.Pop();
        int number = Symbol.Family is "geometry" or "nature" ? (Symbol.Rank - 1) / 12 + 1 : Symbol.Rank;
        string label = Symbol.Family switch { "circles" => "DOT", "bamboo" => "BAM", "characters" => "WAN", "wind" => new[] { "E", "S", "W", "N" }[Symbol.Rank - 1], "dragon" => new[] { "R", "G", "W" }[Symbol.Rank - 1], "flower" => "FL", "season" => "SE", _ => "" };
        if (Labels && label != "") Text(d, label, 8, 66, 10, ink);
        Text(d, number.ToString(), 45, 58, 22, ink, true);
        if (Blocked) { d.DrawEllipse(B("#8A897F"), null, new(9, 76), 2, 2); }
    }
    static void Text(DrawingContext d, string value, double x, double y, double size, Brush brush, bool bold = false)
    {
        d.DrawText(new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal), size, brush, 1), new(x, y));
    }
    static void Path(DrawingContext d, string data, Brush? fill, Brush ink, double width = 2.4) => d.DrawGeometry(fill, new(ink, width), System.Windows.Media.Geometry.Parse(data));
    static void Polygon(DrawingContext d, int sides, Brush ink, bool star = false)
    {
        var points = Enumerable.Range(0, star ? sides * 2 : sides).Select(i => { double angle = -Math.PI / 2 + i * Math.PI * 2 / (star ? sides * 2 : sides); double r = star && i % 2 == 1 ? 10 : 23; return new Point(24 + Math.Cos(angle) * r, 25 + Math.Sin(angle) * r); }).ToArray();
        var g = new StreamGeometry(); using (var c = g.Open()) { c.BeginFigure(points[0], true, true); c.PolyLineTo(points.Skip(1).ToArray(), true, true); }
        d.DrawGeometry(ink, new(ink, 2), g);
    }
    static void Geometry(DrawingContext d, int shape, Brush ink)
    {
        switch (shape)
        {
            case 0: d.DrawEllipse(ink, null, new(24, 25), 21, 21); break;
            case 1: Polygon(d, 3, ink); break;
            case 2: d.DrawRoundedRectangle(ink, null, new(4, 5, 40, 40), 3, 3); break;
            case 3: Path(d, "M24,1 L47,25 24,49 1,25 Z", ink, ink); break;
            case 4: Polygon(d, 5, ink, true); break;
            case 5: Polygon(d, 6, ink); break;
            case 6: Path(d, "M17,3 L31,3 31,18 46,18 46,32 31,32 31,47 17,47 17,32 2,32 2,18 17,18 Z", ink, ink); break;
            case 7: Path(d, "M35,3 A23,23 0 1 0 35,47 A20,23 0 0 1 35,3 Z", ink, ink); break;
            case 8: Path(d, "M24,45 C-13,20 5,-6 24,10 C43,-6 61,20 24,45 Z", ink, ink); break;
            case 9: Polygon(d, 8, ink); break;
            case 10: d.DrawEllipse(null, new(ink, 9), new(24, 25), 18, 18); break;
            default: Path(d, "M5,3 L43,3 28,25 43,47 5,47 20,25 Z", ink, ink); break;
        }
    }
    static void Nature(DrawingContext d, int shape, Brush ink)
    {
        switch (shape)
        {
            case 0: Path(d, "M4,44 C-2,10 19,0 45,3 C49,30 26,48 4,44 Z", ink, ink); Path(d, "M4,44 L35,12 M16,31 L14,17 M23,24 L36,26", null, Brushes.Ivory, 1.7); break;
            case 1: Path(d, "M24,1 L6,23 14,23 2,38 20,38 20,49 28,49 28,38 46,38 34,23 42,23 Z", ink, ink); break;
            case 2: Path(d, "M24,15 C2,1 -8,35 12,47 Q24,51 26,46 Q49,52 47,25 Q43,3 24,15 Z M24,14 Q21,2 27,0 M26,9 Q41,-2 42,6 Q38,15 26,9", ink, ink); break;
            case 3: Path(d, "M2,25 Q22,-3 39,19 L48,10 48,40 39,31 Q22,54 2,25 Z", ink, ink); d.DrawEllipse(Brushes.Ivory, null, new(12, 23), 3, 3); break;
            case 4: Path(d, "M2,32 Q21,33 20,17 Q22,4 32,9 L39,15 47,17 38,21 Q41,40 20,43 L2,48 9,37 Z", ink, ink); Path(d, "M18,30 Q25,17 32,28", null, Brushes.Ivory, 2); break;
            case 5: d.DrawEllipse(ink, null, new(12, 16), 11, 15); d.DrawEllipse(ink, null, new(36, 16), 11, 15); d.DrawEllipse(ink, null, new(14, 36), 9, 11); d.DrawEllipse(ink, null, new(34, 36), 9, 11); Path(d, "M24,9 L24,45 M24,10 L17,2 M24,10 L31,2", null, Brushes.Ivory, 3); break;
            case 6: Path(d, "M3,27 A21,23 0 0 1 45,27 Z M18,28 L18,47 31,47 29,28", ink, ink); d.DrawEllipse(Brushes.Ivory, null, new(16, 17), 4, 4); d.DrawEllipse(Brushes.Ivory, null, new(31, 14), 3, 3); break;
            case 7: Flower(d, ink); break;
            case 8: Path(d, "M21,9 Q24,-2 30,1 M21,9 C8,11 18,21 7,32 C-4,53 52,56 42,32 C32,21 36,10 26,9 Z", ink, ink); break;
            case 9: d.DrawEllipse(ink, null, new(22, 24), 18, 18); Path(d, "M20,24 C12,16 30,11 32,24 C35,39 8,40 10,22 M3,46 L40,46 45,29 M41,35 L37,28 M43,32 L48,26", null, ink, 3); d.DrawEllipse(null, new(Brushes.Ivory, 2), new(23, 24), 8, 8); break;
            case 10: d.DrawEllipse(ink, null, new(24, 25), 12, 12); for (int i = 0; i < 12; i++) { double a = i * Math.PI / 6; d.DrawLine(new(ink, 3), new(24 + 17 * Math.Cos(a), 25 + 17 * Math.Sin(a)), new(24 + 24 * Math.Cos(a), 25 + 24 * Math.Sin(a))); } break;
            default: Path(d, "M0,45 L19,6 30,27 36,14 49,45 Z", ink, ink); Path(d, "M13,19 L19,6 26,20 20,18 Z", Brushes.Ivory, Brushes.Ivory, 1); break;
        }
    }
    static void Flower(DrawingContext d, Brush ink)
    {
        for (int i = 0; i < 6; i++) { double a = i * Math.PI / 3; d.DrawEllipse(ink, null, new(24 + 13 * Math.Cos(a), 25 + 13 * Math.Sin(a)), 9, 9); }
        d.DrawEllipse(Brushes.Ivory, null, new(24, 25), 6, 6);
    }
    static void ChineseNumber(DrawingContext d, int rank, Brush ink)
    {
        string[] strokes = ["M5,16 L43,16", "M8,8 L40,8 M3,25 L45,25", "M7,4 L41,4 M10,15 L38,15 M3,27 L45,27", "M5,2 L43,2 43,29 5,29 Z M18,3 L18,19 10,24 M30,3 L30,21 37,21", "M7,2 L40,2 M22,2 L17,26 M5,12 L35,12 35,27 M3,28 L45,28", "M20,0 L27,6 M3,11 L45,11 M18,17 L5,30 M30,17 L43,30", "M3,15 L44,10 M22,0 L22,25 Q31,32 43,26", "M18,1 Q16,22 3,30 M28,1 Q32,22 45,30", "M5,10 L35,10 31,25 Q37,34 46,25 M20,0 Q23,19 4,31"];
        Path(d, strokes[rank-1], null, ink, 2.6);
    }
    static void Classic(DrawingContext d, Symbol s, Brush ink)
    {
        switch (s.Family)
        {
            case "circles":
                for (int i = 0; i < s.Rank; i++) { int cols = s.Rank < 5 ? 2 : 3; double x = s.Rank == 1 ? 24 : 8 + i % cols * (cols == 2 ? 30 : 16); double y = s.Rank == 1 ? 24 : 8 + i / cols * 16; d.DrawEllipse(null, new(ink, 2.7), new(x, y), s.Rank == 1 ? 18 : 5, s.Rank == 1 ? 18 : 5); if(s.Rank == 1) d.DrawEllipse(ink, null, new(x,y),8,8); } break;
            case "bamboo":
                for (int i = 0; i < s.Rank; i++) { double x = 7 + i % 3 * 16, y = 2 + i / 3 * 17; d.DrawRoundedRectangle(ink, null, new(x,y,7,13),3,3); d.DrawLine(new(Brushes.Ivory,1),new(x,y+6),new(x+7,y+6)); } break;
            case "characters":
                ChineseNumber(d, s.Rank, ink);
                Path(d, "M5,34 L43,34 M12,28 L12,40 M35,28 L35,40 M8,44 L40,44 40,52 M18,39 L18,52 M29,39 L29,52", null, ink, 2.5); break;
            case "wind": Path(d, new[] { "M3,5 L45,5 M24,0 L24,49 M7,13 L41,13 41,33 7,33 Z M7,23 L41,23 M24,34 L3,49 M24,34 L45,49", "M2,8 L46,8 M24,0 L24,16 M6,49 L6,18 42,18 42,49 M12,28 L36,28 M14,23 L20,28 M34,23 L28,28 M12,37 L36,37 M24,29 L24,49", "M2,3 L46,3 M6,49 L6,14 42,14 42,49 Z M18,3 L18,27 10,36 M30,3 L30,31 36,31 M7,42 L41,42", "M3,15 L20,15 M20,1 L20,49 M3,41 L20,35 M31,1 L31,44 Q39,51 46,43 M31,25 L44,14" }[s.Rank-1], null, ink, 2.8); break;
            case "dragon": if(s.Rank == 3) d.DrawRoundedRectangle(null,new(ink,4),new(4,2,40,46),3,3); else Path(d, s.Rank == 1 ? "M24,0 L24,49 M6,12 L42,12 42,36 6,36 Z" : "M7,2 L37,2 14,22 44,22 22,48 M5,37 L43,37",null,ink,4); break;
            case "flower": Flower(d, ink); break;
            case "season": Nature(d, s.Rank switch { 1 => 0, 2 => 10, 3 => 1, _ => 11 }, ink); break;
        }
    }
}

public static class BackgroundArt
{
    static readonly Dictionary<string, Brush> Cache = [];
    static Color Color(string s) => (Color)ColorConverter.ConvertFromString(s);
    public static Brush Create(string id)
    {
        if (Cache.TryGetValue(id, out var cached)) return cached;
        var palette = id switch { "wood" => new[] { "#785437", "#4A3428" }, "forest" => new[] { "#3B6255", "#102F2A" }, "paper" => new[] { "#E5DCC5", "#C3B99F" }, "minimal" => new[] { "#252D32", "#141C22" }, _ => new[] { "#346D5C", "#184A3E" } };
        var group = new DrawingGroup { ClipGeometry = new RectangleGeometry(new Rect(0, 0, 1000, 700)) };
        using(var d = group.Open())
        {
            d.DrawRectangle(new LinearGradientBrush(Color(palette[0]),Color(palette[1]),65),null,new(0,0,1000,700));
            var pen = new Pen(new SolidColorBrush(Color(id == "paper" ? "#0A493E30" : "#09FFFFFF")),1);
            if(id == "wood") for(int y=0;y<700;y+=8) { var g = new StreamGeometry(); using(var c=g.Open()){c.BeginFigure(new(0,y),false,false);c.BezierTo(new(250,y+20),new(700,y-18),new(1000,y+7),true,false);} d.DrawGeometry(null,pen,g); }
            else if(id == "forest") for(int i=0;i<18;i++) { double x=i*67; var g=System.Windows.Media.Geometry.Parse($"M{x},700 L{x+100},80 {x+200},700 Z"); d.DrawGeometry(new SolidColorBrush(Color("#0C021C17")),null,g); }
            else if(id != "minimal") for(int y=0;y<700;y+=6) for(int x=0;x<1000;x+=8) d.DrawEllipse(pen.Brush,null,new(x+(y%12),y),.7,.7);
            d.DrawRoundedRectangle(null,new Pen(new SolidColorBrush(Color("#16FFFFFF")),1.5),new(18,18,964,664),14,14);
        }
        var result = new DrawingBrush(group) { Stretch=Stretch.Fill };
        result.Freeze(); Cache[id] = result; return result;
    }
}
