# RAM Danismani - program ikonunu (src\ram.ico) ureten betik.
# Sadece Windows'ta yerlesik PowerShell + System.Drawing kullanir; ek kurulum gerekmez.
# Bir RAM modulu cizer: yesil devre karti, siyah bellek cipleri, altin pinler, kilit centigi.
# Boyutlar: 16, 32, 48 (32 bit BMP: BITMAPINFOHEADER + XOR + AND maskesi) ve 256 (PNG gomulu).
# Bu karisim Windows XP'den 11'e her yerde ve csc.exe /win32icon ile sorunsuz calisir.
#
# Kullanim:  powershell -NoProfile -ExecutionPolicy Bypass -File araclar\ikon-uret.ps1 [-Cikti src\ram.ico] [-Onizleme onizleme.png]
param(
    [string]$Cikti = (Join-Path (Split-Path -Parent $PSScriptRoot) 'src\ram.ico'),
    [string]$Onizleme = ''
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function Renk([int]$r, [int]$g, [int]$b, [int]$a = 255) { [System.Drawing.Color]::FromArgb($a, $r, $g, $b) }

# Kesirli koordinati piksele yuvarlayan dikdortgen (kucuk boyutlarda net kenar icin)
function Dik([double]$x, [double]$y, [double]$w, [double]$h, [bool]$yuvarla) {
    if ($yuvarla) {
        $x1 = [Math]::Round($x); $y1 = [Math]::Round($y)
        $x2 = [Math]::Round($x + $w); $y2 = [Math]::Round($y + $h)
        if ($x2 -le $x1) { $x2 = $x1 + 1 }
        if ($y2 -le $y1) { $y2 = $y1 + 1 }
        return New-Object System.Drawing.RectangleF ([single]$x1), ([single]$y1), ([single]($x2 - $x1)), ([single]($y2 - $y1))
    }
    return New-Object System.Drawing.RectangleF ([single]$x), ([single]$y), ([single]$w), ([single]$h)
}

function YuvarlakYol([System.Drawing.RectangleF]$r, [double]$rad) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    if ($rad -le 0.5) { $p.AddRectangle($r); return $p }
    $d = [single](2 * $rad)
    $p.AddArc($r.X, $r.Y, $d, $d, 180, 90)
    $p.AddArc($r.Right - $d, $r.Y, $d, $d, 270, 90)
    $p.AddArc($r.Right - $d, $r.Bottom - $d, $d, $d, 0, 90)
    $p.AddArc($r.X, $r.Bottom - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

function Ciz([int]$S) {
    $bmp = New-Object System.Drawing.Bitmap $S, $S, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::Transparent)
    $kucuk = $S -le 32
    if ($kucuk) {
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    } else {
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    }

    # Devre karti (PCB)
    $pcbX = 0.0; $pcbW = [double]$S
    if ($S -ge 48) { $pcbX = $S * 0.03; $pcbW = $S * 0.94 }
    $pcbY = $S * 0.22; $pcbH = $S * 0.56
    if ($S -eq 16) { $pcbY = 3; $pcbH = 10 }
    $pcb = Dik $pcbX $pcbY $pcbW $pcbH $kucuk
    $rad = 0
    if (-not $kucuk) { $rad = $S * 0.025 }

    $yesil = Renk 34 139 60
    $koyuYesil = Renk 16 90 36
    $yol = YuvarlakYol $pcb $rad
    $fircaPcb = New-Object System.Drawing.Drawing2D.LinearGradientBrush $pcb, (Renk 44 160 72), (Renk 26 120 50), 90
    if ($kucuk) { $fircaPcb = New-Object System.Drawing.SolidBrush $yesil }
    $g.FillPath($fircaPcb, $yol)
    if ($S -ge 32) {
        $kalem = New-Object System.Drawing.Pen $koyuYesil, ([single][Math]::Max(1, $S / 64))
        $kalem.Alignment = [System.Drawing.Drawing2D.PenAlignment]::Inset
        $g.DrawPath($kalem, $yol)
    }

    # Bellek cipleri
    $cipSayisi = 4; if ($S -eq 16) { $cipSayisi = 3 }
    $kenar = $pcb.Width * 0.07; $bosluk = $pcb.Width * 0.045
    if ($S -eq 16) { $kenar = 1; $bosluk = 1 }
    $cipW = ($pcb.Width - 2 * $kenar - ($cipSayisi - 1) * $bosluk) / $cipSayisi
    $cipY = $pcb.Y + $pcb.Height * 0.13; $cipH = $pcb.Height * 0.45
    if ($S -eq 16) { $cipY = $pcb.Y + 1; $cipH = 4 }
    $siyah = New-Object System.Drawing.SolidBrush (Renk 24 24 28)
    $parlak = New-Object System.Drawing.SolidBrush (Renk 70 74 82)
    for ($i = 0; $i -lt $cipSayisi; $i++) {
        $cx = $pcb.X + $kenar + $i * ($cipW + $bosluk)
        $c = Dik $cx $cipY $cipW $cipH $kucuk
        $g.FillRectangle($siyah, $c)
        if ($S -ge 48) {
            # ust kenar parlakligi ve 1. pin isareti
            $g.FillRectangle($parlak, (Dik $c.X $c.Y $c.Width ([Math]::Max(1, $S / 96)) $false))
            $nokta = [Math]::Max(2, $S / 40)
            $g.FillEllipse($parlak, [single]($c.X + $nokta), [single]($c.Bottom - 2.2 * $nokta), [single]$nokta, [single]$nokta)
        }
    }

    # Kucuk SPD cipi (sadece buyuk boyutlarda)
    if ($S -ge 48) {
        $spdW = $pcb.Width * 0.06; $spdH = $pcb.Height * 0.1
        $spdX = $pcb.X + $pcb.Width * 0.62; $spdY = $cipY + $cipH + $pcb.Height * 0.04
        $g.FillRectangle($siyah, (Dik $spdX $spdY $spdW $spdH $false))
    }

    # Altin pinler
    $pinY = $pcb.Y + $pcb.Height * 0.72; $pinH = $pcb.Bottom - $pinY
    if ($S -eq 16) { $pinY = $pcb.Bottom - 2; $pinH = 2 }
    $pinSayisi = 7
    if ($S -eq 32) { $pinSayisi = 10 } elseif ($S -eq 48) { $pinSayisi = 14 } elseif ($S -ge 128) { $pinSayisi = 26 }
    $pinKenar = $pcb.Width * 0.05
    if ($S -eq 16) { $pinKenar = 1 }
    $alan = $pcb.Width - 2 * $pinKenar
    $adim = $alan / ($pinSayisi * 2 - 1)
    $pinW = $adim
    if ($S -eq 16) { $adim = 2; $pinSayisi = 7; $pinW = 1 }
    $altin = New-Object System.Drawing.SolidBrush (Renk 222 170 40)
    $altinAcik = New-Object System.Drawing.SolidBrush (Renk 250 214 110)
    for ($i = 0; $i -lt $pinSayisi; $i++) {
        $px = $pcb.X + $pinKenar + $i * 2 * $adim
        if ($S -eq 16) { $px = $pcb.X + 1 + $i * 2 }
        $p = Dik $px $pinY $pinW $pinH $kucuk
        $g.FillRectangle($altin, $p)
        if ($S -ge 48) { $g.FillRectangle($altinAcik, (Dik $p.X $p.Y ([Math]::Max(1, $p.Width * 0.35)) $p.Height $false)) }
    }

    # Kilit centigi (alt kenarda bosluk) ve yan centikler: seffaf olarak kes
    $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $seffaf = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::Transparent)
    $centikW = [Math]::Max(1, $pcb.Width * 0.045)
    $centikX = $pcb.X + $pcb.Width * 0.40
    $centikY = $pinY - $pcb.Height * 0.06
    if ($S -eq 16) { $centikX = 7; $centikW = 1; $centikY = $pcb.Bottom - 2 }
    if ($S -eq 32) { $centikY = $pinY }
    $g.FillRectangle($seffaf, (Dik $centikX $centikY $centikW ($pcb.Bottom - $centikY + 1) $kucuk))
    if ($S -ge 48) {
        $yr = $pcb.Height * 0.09
        $yy = $pcb.Y + $pcb.Height * 0.55
        $g.FillEllipse($seffaf, [single]($pcb.X - $yr), [single]($yy - $yr), [single](2 * $yr), [single](2 * $yr))
        $g.FillEllipse($seffaf, [single]($pcb.Right - $yr), [single]($yy - $yr), [single](2 * $yr), [single](2 * $yr))
    }
    $g.Dispose()
    return $bmp
}

# 32 bit BMP ikon verisi: BITMAPINFOHEADER (yukseklik x2) + alttan yukari BGRA + AND maskesi
function BmpVerisi([System.Drawing.Bitmap]$bmp) {
    $S = $bmp.Width
    $ms = New-Object System.IO.MemoryStream
    $w = New-Object System.IO.BinaryWriter $ms
    $maskeSatir = [int]([Math]::Floor(($S + 31) / 32) * 4)
    $w.Write([int]40); $w.Write([int]$S); $w.Write([int]($S * 2))
    $w.Write([int16]1); $w.Write([int16]32); $w.Write([int]0)
    $w.Write([int]($S * $S * 4 + $maskeSatir * $S))
    $w.Write([int]0); $w.Write([int]0); $w.Write([int]0); $w.Write([int]0)
    for ($y = $S - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $S; $x++) {
            $c = $bmp.GetPixel($x, $y)
            $w.Write([byte]$c.B); $w.Write([byte]$c.G); $w.Write([byte]$c.R); $w.Write([byte]$c.A)
        }
    }
    for ($y = $S - 1; $y -ge 0; $y--) {
        $satir = New-Object byte[] $maskeSatir
        for ($x = 0; $x -lt $S; $x++) {
            if ($bmp.GetPixel($x, $y).A -lt 128) { $satir[[int][Math]::Floor($x / 8)] = $satir[[int][Math]::Floor($x / 8)] -bor (0x80 -shr ($x % 8)) }
        }
        $w.Write($satir)
    }
    $w.Flush()
    return ,$ms.ToArray()
}

function PngVerisi([System.Drawing.Bitmap]$bmp) {
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    return ,$ms.ToArray()
}

$boyutlar = @(16, 32, 48, 256)
$resimler = @()
$veriler = @()
foreach ($s in $boyutlar) {
    $b = Ciz $s
    $resimler += ,$b
    if ($s -ge 256) { $veriler += ,(PngVerisi $b) } else { $veriler += ,(BmpVerisi $b) }
}

# ICO dosyasi: ICONDIR + ICONDIRENTRY x n + veriler
$ms = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $ms
$w.Write([int16]0); $w.Write([int16]1); $w.Write([int16]$boyutlar.Count)
$konum = 6 + 16 * $boyutlar.Count
for ($i = 0; $i -lt $boyutlar.Count; $i++) {
    $s = $boyutlar[$i]
    $bs = [byte]0; if ($s -lt 256) { $bs = [byte]$s }
    $w.Write($bs); $w.Write($bs); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([int16]1); $w.Write([int16]32)
    $w.Write([int]$veriler[$i].Length); $w.Write([int]$konum)
    $konum += $veriler[$i].Length
}
foreach ($v in $veriler) { $w.Write($v) }
$w.Flush()
$klasor = Split-Path -Parent $Cikti
if ($klasor -and -not (Test-Path $klasor)) { New-Item -ItemType Directory -Path $klasor | Out-Null }
[System.IO.File]::WriteAllBytes($Cikti, $ms.ToArray())
Write-Output ("Ikon yazildi: " + $Cikti + " (" + $ms.Length + " bayt, boyutlar: " + ($boyutlar -join ', ') + ")")

# Istege bagli onizleme: tum boyutlar yan yana, 4 kat buyutulmus kucukler dahil
if ($Onizleme) {
    $genislik = 0; foreach ($s in $boyutlar) { $genislik += [Math]::Max($s * 4, 256) + 10 }
    $on = New-Object System.Drawing.Bitmap $genislik, 1034
    $g = [System.Drawing.Graphics]::FromImage($on)
    $g.Clear((Renk 240 240 240))
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $x = 0
    for ($i = 0; $i -lt $boyutlar.Count; $i++) {
        $s = $boyutlar[$i]; $k = 4; if ($s -ge 256) { $k = 1 }
        $g.DrawImage($resimler[$i], $x, 0, $s * $k, $s * $k)
        $g.DrawImage($resimler[$i], $x, 1034 - $s - 4, $s, $s)
        $x += [Math]::Max($s * 4, 256) + 10
    }
    $g.Dispose()
    $on.Save($Onizleme, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output ("Onizleme: " + $Onizleme)
}
