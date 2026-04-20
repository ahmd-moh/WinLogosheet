Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Today's date folder
$DateFolder = "Screenshots-" + (Get-Date -Format "yyyy-MM-dd")
$OutputDir = Join-Path $ScriptDir $DateFolder
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

# Current hour file name
$hour = (Get-Date).ToString("HH")
$file = Join-Path $OutputDir "$hour.png"

# Capture full screen
$screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$bitmap = New-Object System.Drawing.Bitmap $screen.width, $screen.height
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.CopyFromScreen($screen.Location, [System.Drawing.Point]::Empty, $screen.size)

# === Crop rectangle area ===
$rect = New-Object System.Drawing.Rectangle 50,192,50,423
$cropped = $bitmap.Clone($rect, $bitmap.PixelFormat)

# === Draw only row descriptions ===
$g = [System.Drawing.Graphics]::FromImage($cropped)
$rows = 19
$rowHeight = [math]::Floor(430 / $rows)
$font = New-Object System.Drawing.Font("Arial", 10, [System.Drawing.FontStyle]::Regular)

# Save final image
$cropped.Save($file, [System.Drawing.Imaging.ImageFormat]::png)

# Cleanup
$graphics.Dispose()
$bitmap.Dispose()
$g.Dispose()
$cropped.Dispose()

