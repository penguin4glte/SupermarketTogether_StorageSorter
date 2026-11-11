# Generates icon.png (256x256) for the Thunderstore package. Windows only (System.Drawing).
param([string]$Out = (Join-Path $PSScriptRoot 'icon.png'))
Add-Type -AssemblyName System.Drawing

$size = 256
$bmp = New-Object System.Drawing.Bitmap $size, $size
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.Clear([System.Drawing.Color]::FromArgb(255, 36, 52, 71))

function Rgb($r, $gg, $b) { [System.Drawing.Color]::FromArgb(255, $r, $gg, $b) }
function RoundRect($x, $y, $w, $h, $r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $p.AddArc($x, $y, $r, $r, 180, 90); $p.AddArc($x + $w - $r, $y, $r, $r, 270, 90)
    $p.AddArc($x + $w - $r, $y + $h - $r, $r, $r, 0, 90); $p.AddArc($x, $y + $h - $r, $r, $r, 90, 90)
    $p.CloseFigure(); $p
}

# Shelf frame
$wood = New-Object System.Drawing.SolidBrush (Rgb 150 105 70)
$g.FillRectangle($wood, 28, 30, 14, 200)
$g.FillRectangle($wood, 214, 30, 14, 200)
foreach ($y in 92, 158, 224) { $g.FillRectangle($wood, 28, $y, 200, 10) }

# Boxes: three rows, colors grouped (sorted look). Lighter = fuller.
$rows = @(
    @{ Y = 44;  C = (Rgb 231 76 60) },
    @{ Y = 110; C = (Rgb 241 196 15) },
    @{ Y = 176; C = (Rgb 46 204 113) }
)
$tape = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(110, 255, 255, 255))
$edge = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(90, 0, 0, 0)), 2
foreach ($row in $rows) {
    for ($i = 0; $i -lt 4; $i++) {
        $x = 48 + $i * 42
        $f = 1.0 - $i * 0.12
        $c = [System.Drawing.Color]::FromArgb(255, [int]($row.C.R * $f), [int]($row.C.G * $f), [int]($row.C.B * $f))
        $br = New-Object System.Drawing.SolidBrush $c
        $rr = RoundRect $x $row.Y 38 46 8
        $g.FillPath($br, $rr); $g.DrawPath($edge, $rr)
        $g.FillRectangle($tape, $x + 15, $row.Y + 2, 8, 42)
    }
}

# Sort arrow badge (bottom-right)
$badge = New-Object System.Drawing.SolidBrush (Rgb 52 152 219)
$g.FillEllipse($badge, 176, 176, 72, 72)
$g.DrawEllipse((New-Object System.Drawing.Pen (Rgb 255 255 255), 4), 176, 176, 72, 72)
$white = New-Object System.Drawing.SolidBrush (Rgb 255 255 255)
$g.FillRectangle($white, 207, 190, 10, 30)
$g.FillPolygon($white, [System.Drawing.Point[]]@(
    (New-Object System.Drawing.Point 196, 216), (New-Object System.Drawing.Point 228, 216), (New-Object System.Drawing.Point 212, 236)))

$g.Dispose()
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "Wrote $Out"
