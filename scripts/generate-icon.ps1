$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path $PSScriptRoot -Parent
function Round-Rect([System.Drawing.Graphics]$graphics,[System.Drawing.Brush]$brush,[single]$x,[single]$y,[single]$width,[single]$height,[single]$radius) {
    $path=[System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter=$radius*2
    $path.AddArc($x,$y,$diameter,$diameter,180,90)
    $path.AddArc($x+$width-$diameter,$y,$diameter,$diameter,270,90)
    $path.AddArc($x+$width-$diameter,$y+$height-$diameter,$diameter,$diameter,0,90)
    $path.AddArc($x,$y+$height-$diameter,$diameter,$diameter,90,90)
    $path.CloseFigure();$graphics.FillPath($brush,$path);$path.Dispose()
}
$frames=[System.Collections.Generic.List[object]]::new()
foreach($size in @(16,24,32,48,64,128,256)) {
    $bitmap=[System.Drawing.Bitmap]::new($size,$size,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g=[System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.ScaleTransform($size/256.0,$size/256.0)
    $green=[System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#163E35'))
    $gold=[System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#EBCB88'))
    $ivory=[System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#FFF8E5'))
    $edge=[System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#BBA57E'))
    $teal=[System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#227E6C'))
    Round-Rect $g $green 4 4 248 248 49
    Round-Rect $g $gold 82 36 131 183 19
    Round-Rect $g $edge 42 43 139 179 19
    Round-Rect $g $ivory 42 34 139 179 19
    foreach($petal in @(@(92,70,36,49),@(92,122,36,49),@(66,96,49,36),@(118,96,49,36))) { $g.FillEllipse($teal,[single]$petal[0],[single]$petal[1],[single]$petal[2],[single]$petal[3]) }
    $g.FillEllipse($gold,97,101,26,26)
    Round-Rect $g $green 82 184 58 6 3
    $stream=[System.IO.MemoryStream]::new();$bitmap.Save($stream,[System.Drawing.Imaging.ImageFormat]::Png)
    $frames.Add(@{Size=$size;Data=$stream.ToArray()})
    if($size -eq 256){$bitmap.Save((Join-Path $projectRoot 'assets/mahjong-icon.png'),[System.Drawing.Imaging.ImageFormat]::Png)}
    $stream.Dispose();$g.Dispose();$bitmap.Dispose();$green.Dispose();$gold.Dispose();$ivory.Dispose();$edge.Dispose();$teal.Dispose()
}
$iconPath=Join-Path $projectRoot 'assets/mahjong.ico'
$file=[System.IO.File]::Create($iconPath);$writer=[System.IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$frames.Count)
    $offset=6+16*$frames.Count
    foreach($frame in $frames){
        $dimension=if($frame.Size -eq 256){0}else{$frame.Size}
        $writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0)
        $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frame.Data.Length);$writer.Write([uint32]$offset)
        $offset+=$frame.Data.Length
    }
    foreach($frame in $frames){$writer.Write([byte[]]$frame.Data)}
} finally { $writer.Dispose();$file.Dispose() }
Write-Host "Created $iconPath (7 sizes)"
