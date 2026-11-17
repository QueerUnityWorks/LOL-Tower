param([string]$OutputDirectory = (Join-Path $PSScriptRoot '../Assets/ElementalTactics/Branding'))

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$null = New-Item -ItemType Directory -Path $OutputDirectory -Force
$iconDirectory = (Resolve-Path $OutputDirectory).Path

function Draw-Polygon($Graphics, [string]$Color, [float[]]$Coordinates) {
    $points = [System.Drawing.PointF[]]::new($Coordinates.Length / 2)
    for ($index = 0; $index -lt $points.Length; $index++) {
        $points[$index] = [System.Drawing.PointF]::new($Coordinates[$index * 2], $Coordinates[$index * 2 + 1])
    }
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml($Color))
    $Graphics.FillPolygon($brush, $points)
    $brush.Dispose()
}

function Draw-Emblem($Graphics) {
    Draw-Polygon $Graphics '#09182A' @(316,356,708,356,708,492,663,526,683,720,730,754,730,808,294,808,294,754,341,720,361,526,316,492)
    Draw-Polygon $Graphics '#E9B85D' @(336,370,410,370,410,423,474,423,474,370,550,370,550,423,614,423,614,370,688,370,688,479,638,515,658,731,708,765,708,786,316,786,316,765,366,731,386,515,336,479)
    Draw-Polygon $Graphics '#FFE4A0' @(336,370,410,370,410,423,474,423,474,370,512,370,512,731,366,731,386,515,336,479)
    Draw-Polygon $Graphics '#B47A36' @(614,423,614,370,688,370,688,479,638,515,658,731,606,731,589,503,614,479)
    Draw-Polygon $Graphics '#FFF0BA' @(336,465,688,465,668,485,356,485)
    Draw-Polygon $Graphics '#153448' @(454,731,454,593,476,563,512,548,548,563,570,593,570,731)
    Draw-Polygon $Graphics '#52E5D1' @(491,699,491,604,512,584,533,604,533,699)
    Draw-Polygon $Graphics '#FFF0BA' @(366,731,658,731,685,750,339,750)
    Draw-Polygon $Graphics '#A97131' @(316,774,708,774,708,792,316,792)
    Draw-Polygon $Graphics '#C8984F' @(512,190,600,284,512,378,424,284)
    Draw-Polygon $Graphics '#B6FFF1' @(512,210,578,284,512,353,446,284)
    Draw-Polygon $Graphics '#3AD5C4' @(512,210,578,284,512,353,525,285)
    Draw-Polygon $Graphics '#148D9C' @(446,284,512,353,525,285)
    Draw-Polygon $Graphics '#FFFFFF' @(512,225,518,268,548,284,516,290,512,325,504,291,473,284,504,277)
}

foreach ($variant in @('RuneExpeditionIcon', 'RuneExpeditionBackground', 'RuneExpeditionForeground')) {
    $bitmap = [System.Drawing.Bitmap]::new(2048, 2048)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.ScaleTransform(2, 2)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    if ($variant -ne 'RuneExpeditionForeground') {
        $gradient = [System.Drawing.Drawing2D.LinearGradientBrush]::new([System.Drawing.Point]::new(0, 0), [System.Drawing.Point]::new(1024, 1024), [System.Drawing.ColorTranslator]::FromHtml('#214559'), [System.Drawing.ColorTranslator]::FromHtml('#080F20'))
        $graphics.FillRectangle($gradient, 0, 0, 1024, 1024)
        $gradient.Dispose()
    }
    if ($variant -eq 'RuneExpeditionIcon') {
        $pen = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#8A744C'), 9)
        $graphics.DrawEllipse($pen, 106, 106, 812, 812)
        $pen.Dispose()
        $pen = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#3A6872'), 3)
        $graphics.DrawEllipse($pen, 124, 124, 776, 776)
        $pen.Dispose()
        foreach ($center in @(@(166,512), @(858,512))) {
            Draw-Polygon $graphics '#E9B85D' @($center[0],($center[1]-19),($center[0]+13),$center[1],$center[0],($center[1]+19),($center[0]-13),$center[1])
        }
        Draw-Emblem $graphics
    }
    if ($variant -eq 'RuneExpeditionForeground') {
        $graphics.TranslateTransform(512, 512)
        $graphics.ScaleTransform(0.7, 0.7)
        $graphics.TranslateTransform(-512, -512)
        Draw-Emblem $graphics
    }
    $graphics.Dispose()
    $finalBitmap = [System.Drawing.Bitmap]::new(1024, 1024)
    $resizeGraphics = [System.Drawing.Graphics]::FromImage($finalBitmap)
    $resizeGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $resizeGraphics.DrawImage($bitmap, 0, 0, 1024, 1024)
    $finalBitmap.Save((Join-Path $iconDirectory "$variant.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $resizeGraphics.Dispose()
    $finalBitmap.Dispose()
    $bitmap.Dispose()
}
Get-ChildItem -LiteralPath $iconDirectory -Filter '*.png' | Select-Object Name, Length
