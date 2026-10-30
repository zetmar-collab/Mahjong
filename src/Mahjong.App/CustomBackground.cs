using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Mahjong.App;

/// <summary>Imports a bounded local bitmap as a self-contained PNG, without retaining the source handle.</summary>
public sealed class CustomBackground
{
    public string FilePath { get; }
    public ImageBrush? Brush { get; private set; }
    public bool Available => Brush != null;
    public CustomBackground(string directory) => FilePath = Path.Combine(directory, "background.png");

    static BitmapSource Decode(string path)
    {
        var info = new FileInfo(path);
        if (info.Length > 32 * 1024 * 1024) throw new InvalidDataException("Image is too large.");
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
        var frame = decoder.Frames[0];
        if (frame.PixelWidth < 1 || frame.PixelHeight < 1 || (long)frame.PixelWidth * frame.PixelHeight > 80_000_000)
            throw new InvalidDataException("Image dimensions are too large.");
        int width = frame.PixelWidth, height = frame.PixelHeight;
        stream.Position = 0;
        var bitmap = new BitmapImage();
        bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.StreamSource = stream;
        if (width >= height && width > 2560) bitmap.DecodePixelWidth = 2560;
        else if (height > 2560) bitmap.DecodePixelHeight = 2560;
        bitmap.EndInit(); bitmap.Freeze();
        return bitmap;
    }

    static ImageBrush MakeBrush(BitmapSource image)
    {
        var brush = new ImageBrush(image) { Stretch = Stretch.UniformToFill, AlignmentX = AlignmentX.Center, AlignmentY = AlignmentY.Center };
        brush.Freeze(); return brush;
    }

    public bool Load()
    {
        try { Brush = MakeBrush(Decode(FilePath)); return true; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or InvalidOperationException or System.Runtime.InteropServices.COMException or FileFormatException or InvalidDataException)
        { Brush = null; return false; }
    }

    public void Import(string source)
    {
        var image = Decode(source);
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        string temporary = FilePath + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image)); encoder.Save(stream); stream.Flush(true);
            }
            File.Move(temporary, FilePath, true);
            Brush = MakeBrush(image);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
