param(
    # Override the SCADA window title pattern from the command line (testing).
    # Passing this switches matching to TITLE mode instead of process mode.
    [string]$WindowTitle = '',
    # Override the HMI process name (testing).
    [string]$ProcessName = '',
    # Override the capture-instance command-line marker (testing).
    [string]$UserMarker = '',
    # Override the tab name to restore before capture (testing/discovery).
    [string]$TabName = '',
    # Save the FULL uncropped window capture as calibrate-full.png and exit.
    # Run this once on the plant PC to measure the crop rectangle.
    [switch]$Calibrate,
    # Dump every clickable/selectable UI element of the aView window to
    # capture.log and exit. Run this once to discover the exact tab name to
    # put in $DpsTabName below.
    [switch]$InspectTabs,
    # OCR-discovery: capture the window, save calibrate-tabs.png, and dump every
    # OCR token + bounding box + colour of the frame to capture.log so the tab
    # strip position, the DPS tab's click point and the active-tab highlight can
    # all be measured from ONE run on the plant PC. Never switches a tab.
    [switch]$CalibrateTabs,
    # One-shot: run the FULL OCR switch-and-restore ONCE right now (regardless of
    # the $OcrTabSwitch master flag), with verbose logging + testswitch-before/
    # after.png, and WITHOUT writing the hourly PNG. Park the target window on a
    # non-DPS tab first to watch a real switch. Safe to run any time.
    [switch]$TestSwitch,
    # Override the output file path (testing only).
    [string]$OutFile = ''
)

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# ═════════════════════════════════ CONFIG ════════════════════════════════
# The SCADA HMI is GE DS Agile aView. Capture renders the aView WINDOW
# itself (PrintWindow with PW_RENDERFULLCONTENT), so the correct image is
# produced even when the window is on the TV (second screen), covered, or
# not in the foreground. Before capturing, the script also RESTORES the
# LOCAL/REMOTE DPS tab inside that window via UI Automation, so a tab the
# operator switched away from no longer produces a wrong image.
#
# TWO-INSTANCE SETUP (recommended when aView allows it): run a SECOND copy
# of aView parked on the LOCAL/REMOTE DPS tab behind the operator's copy.
# When several windows match the title pattern, the script prefers the one
# already SHOWING the DPS tab, so the operator's window is never switched
# and the hourly capture becomes invisible to them.
#
# ── HMI process ──────────────────────────────────────────────────────────
# Windows are matched by OWNING PROCESS: the HMI is DSAgileHMI.exe. The
# similarly named DSAgileHMIDaemon.exe is only its support service — it
# has a different process name and no visible window, so it can never be
# picked. The title pattern below is only a fallback.
$DefaultProcessName = 'DSAgileHMI'
# The dedicated capture copy is started with the read-only user on its
# command line ("DSAgileHMI.exe PublicUser Public12345"), which lets the
# script recognize it by COMMAND LINE alone — no window is ever probed.
$DefaultUserMarker = 'PublicUser'
# Auto-start that capture copy when it is not running yet, so the setup
# self-heals like the rest of the system. Skipped when the exe is absent
# (dev machine) and in -Calibrate / -InspectTabs diagnostic runs; the
# freshly started copy becomes the capture target from the NEXT hour.
$AutoStartCapture = $true
$HmiExePath = 'C:\Program Files (x86)\DSAgile\aView\exe\loc\DSAgileHMI.exe'
$HmiExeArgs = 'PublicUser Public12345'

# Substring of the HMI window title (fallback when process matching finds
# nothing, and forced with -WindowTitle for testing). When no window
# matches either way, every visible title is written to capture.log so
# the right pattern can be chosen.
$DefaultWindowTitle = '*aView*'

# ── Tab auto-restore ─────────────────────────────────────────────────────
$RestoreTab = $true               # master switch for the whole feature
# Name of the tab to select before capture. This must match aView's own
# label for the tab. If unsure, run:  screencap-SET2.ps1 -InspectTabs
# and copy the exact text it logs.
$DpsTabName = 'LOCAL/REMOTE DPS'
# Milliseconds to wait after selecting the tab before capturing, so aView
# has time to repaint the newly-shown tab.
$TabSettleMs = 700
# Coordinate-click FALLBACK, used only when UI Automation cannot find the
# tab (some HMIs draw tabs on a single custom canvas with no UIA elements).
# Measure the tab's pixel position inside calibrate-full.png and put its
# CLIENT coordinates here. Leave both 0 to disable the fallback.
$TabClickX = 0
$TabClickY = 0

# ── OCR tab switch (Stage 2) ── DISABLED until calibrated ────────────────
# When $true, the script (before saving) checks whether the value strip is
# empty; if so it OCR-locates the LOCAL/REMOTE DPS tab, switches to it,
# re-captures, and clicks back to the operator's previous tab. Every step
# aborts safely on any uncertainty. Turn this ON only AFTER -CalibrateTabs
# has been used to fill in the measured values below.
$OcrTabSwitch = $false
# Only switch the operator's live tab when we can also identify (and restore)
# the tab that was active. Leave $true so the operator is never stranded on
# DPS by a switch we cannot undo.
$OcrSwitchOnlyIfRestorable = $true
# CLIENT-area vertical band that contains the tab strip. Measure Top/Height
# from calibrate-tabs.png (the CALTABS token Y values bracket it).
$TabStripTop = 0
$TabStripHeight = 44
# Regex that identifies the DPS tab among the OCR'd tab labels.
$DpsTabPattern = 'DPS'
# Word-tokens closer than this many px horizontally belong to the same tab
# label (e.g. "LOCAL/REMOTE" + "DPS").
$TabGroupGapPx = 26
# From a tab label's centre, move this many px DOWN to land on the tab's solid
# fill (not on its text) when comparing active-vs-inactive colour. Measure it.
$TabProbeDy = 14
# Minimum RGB distance for a tab's fill to count as the odd-one-out active tab.
$TabActiveMinDelta = 40
# How to click a tab:  'auto' = try a posted click first, fall back to a brief
# foreground real-mouse click;  'post' = posted only;  'foreground' = always
# the brief foreground click.  'auto' is safest until we learn what aView honors.
$TabClickMode = 'auto'
# Value-presence test: OCR the (upscaled) value strip; this many numeric tokens
# means the DPS readings are already on screen (so no switch is needed).
$DpsMinValueTokens = 3
$ValueOcrScale = 3

# ── Crop rectangle ───────────────────────────────────────────────────────
# CLIENT-AREA coordinates of the aView window. If they drift after moving
# to window capture, run:  screencap-SET2.ps1 -Calibrate
# and measure the rectangle inside calibrate-full.png.
$CropX = 50; $CropY = 192; $CropW = 50; $CropH = 423

# Screen used when the aView window cannot be found at all:
# 'Secondary' = the TV (falls back to primary if only one screen exists).
$FallbackScreen = 'Secondary'
# ═════════════════════════════════════════════════════════════════════════

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$LogFile   = Join-Path $ScriptDir 'capture.log'

function Write-CapLog([string]$msg) {
    $line = '{0:yyyy-MM-dd HH:mm:ss}  {1}' -f (Get-Date), $msg
    try { Add-Content -Path $LogFile -Value $line -Encoding UTF8 } catch { }
}

# Allow the -TabName parameter to override the configured tab.
if ($TabName) { $DpsTabName = $TabName }
$ProcName  = if ($ProcessName) { $ProcessName -replace '\.exe$', '' } else { $DefaultProcessName }
$CapMarker = if ($UserMarker) { $UserMarker } else { $DefaultUserMarker }

# UI Automation lives in the .NET Framework (Windows PowerShell 5.1, which
# is what screencap.vbs launches). If it is missing we degrade to
# capture-only with a logged warning.
$UiaOk = $false
try {
    Add-Type -AssemblyName UIAutomationClient -ErrorAction Stop
    Add-Type -AssemblyName UIAutomationTypes  -ErrorAction Stop
    $UiaOk = $true
} catch {
    Write-CapLog "WARN UI Automation unavailable ($($_.Exception.Message)); tab restore falls back to click/none"
}

# user32-only helper (no System.Drawing references so the same C# compiles
# under both Windows PowerShell 5.1 and PowerShell 7).
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public class Win32Cap
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder sb, int max);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    // Tab-switch clicking / foregrounding (Stage 2).
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT p);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, IntPtr ei);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    // All visible top-level windows that have a title, as "handle|pid|title"
    // tuples (titles may contain '|', so parse with Split(char, 3)).
    public static List<string> GetTopWindows()
    {
        List<string> list = new List<string>();
        EnumWindows(delegate(IntPtr h, IntPtr l)
        {
            if (!IsWindowVisible(h)) return true;
            StringBuilder sb = new StringBuilder(512);
            GetWindowText(h, sb, 512);
            if (sb.Length > 0)
            {
                uint pid;
                GetWindowThreadProcessId(h, out pid);
                list.Add(h.ToInt64().ToString() + "|" + pid.ToString() + "|" + sb.ToString());
            }
            return true;
        }, IntPtr.Zero);
        return list;
    }
}
'@

# ── OCR (Windows.Media.Ocr) ───────────────────────────────────────────────
# Reads text + per-word bounding boxes from a System.Drawing.Bitmap using the
# OCR engine built into Windows 10/11 (no external dependency, unlike the
# Tesseract package the C# side uses). This is what lets the script FIND the
# "LOCAL/REMOTE DPS" tab by its printed label and check whether the value area
# is already showing readings. Everything degrades gracefully: on a machine
# without the OCR component the caller falls back to UIA / measured
# coordinates, so the hourly capture is never blocked by a missing engine.
$script:OcrReady = $false
$script:OcrTried = $false
$script:AsTaskOp = $null
$script:TabDiag  = $false   # verbose per-tab diagnostics (set on by -TestSwitch)

function Initialize-WinRtOcr {
    if ($script:OcrReady) { return $true }
    if ($script:OcrTried) { return $false }   # one attempt per run, logged once
    $script:OcrTried = $true
    try {
        # Project the WinRT types into this Windows PowerShell 5.1 session.
        $null = [Windows.Media.Ocr.OcrEngine,          Windows.Foundation, ContentType = WindowsRuntime]
        $null = [Windows.Graphics.Imaging.BitmapDecoder, Windows.Foundation, ContentType = WindowsRuntime]
        $null = [Windows.Graphics.Imaging.SoftwareBitmap, Windows.Foundation, ContentType = WindowsRuntime]
        $null = [Windows.Globalization.Language,        Windows.Foundation, ContentType = WindowsRuntime]
        Add-Type -AssemblyName System.Runtime.WindowsRuntime -ErrorAction Stop
        # The generic AsTask(IAsyncOperation<T>) overload used to await WinRT ops.
        $script:AsTaskOp = ([System.WindowsRuntimeSystemExtensions].GetMethods() |
            Where-Object {
                $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and
                $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'
            })[0]
        if (-not $script:AsTaskOp) { throw 'AsTask(IAsyncOperation) overload not found' }
        $script:OcrReady = $true
        return $true
    } catch {
        Write-CapLog "WARN Windows.Media.Ocr unavailable ($($_.Exception.Message)); OCR tab features disabled"
        return $false
    }
}

# Synchronously wait on a WinRT IAsyncOperation<T> and return its result.
function Await-Op($op, [Type]$resultType) {
    $task = $script:AsTaskOp.MakeGenericMethod($resultType).Invoke($null, @($op))
    [void]$task.Wait(-1)
    return $task.Result
}

# OCR a bitmap. Returns tokens as PSCustomObjects { Text, X, Y, W, H, Cx, Cy }
# in the bitmap's own pixel coordinates, or $null when OCR is unavailable /
# failed (an empty list means "ran fine, found nothing"). When $region (a
# System.Drawing.Rectangle) is given, only that sub-area is scanned and the
# returned coordinates are offset back into full-bitmap space.
function Get-OcrTokens([System.Drawing.Bitmap]$bmp, $region = $null) {
    if (-not (Initialize-WinRtOcr)) { return $null }

    $work = $bmp; $ox = 0; $oy = 0; $crop = $null
    if ($region) {
        $b = New-Object System.Drawing.Rectangle 0, 0, $bmp.Width, $bmp.Height
        $region.Intersect($b)
        if ($region.Width -le 0 -or $region.Height -le 0) { return @() }
        $crop = $bmp.Clone($region, $bmp.PixelFormat)
        $work = $crop; $ox = $region.X; $oy = $region.Y
    }

    $ms = New-Object System.IO.MemoryStream
    try {
        $work.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $ms.Position = 0
        $ras     = [System.IO.WindowsRuntimeStreamExtensions]::AsRandomAccessStream($ms)
        $decoder = Await-Op ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($ras)) ([Windows.Graphics.Imaging.BitmapDecoder])
        $sb      = Await-Op ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])

        $engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
        if (-not $engine) {
            try {
                $lang   = New-Object Windows.Globalization.Language 'en-US'
                $engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromLanguage($lang)
            } catch { }
        }
        if (-not $engine) { Write-CapLog 'WARN no OCR language pack available'; return $null }

        $res    = Await-Op ($engine.RecognizeAsync($sb)) ([Windows.Media.Ocr.OcrResult])
        $tokens = New-Object System.Collections.Generic.List[object]
        foreach ($line in $res.Lines) {
            foreach ($w in $line.Words) {
                $r = $w.BoundingRect
                $x = [int]$r.X + $ox; $y = [int]$r.Y + $oy
                $tokens.Add([pscustomobject]@{
                    Text = $w.Text
                    X = $x; Y = $y; W = [int]$r.Width; H = [int]$r.Height
                    Cx = $x + [int]($r.Width / 2); Cy = $y + [int]($r.Height / 2)
                })
            }
        }
        return $tokens
    } catch {
        Write-CapLog "WARN OCR failed: $($_.Exception.Message)"
        return $null
    } finally {
        $ms.Dispose()
        if ($crop) { $crop.Dispose() }
    }
}

# ── OCR tab-switch helpers (Stage 2) ─────────────────────────────────────
# Everything below only runs when $OcrTabSwitch is enabled. The design is
# deliberately conservative: any uncertainty (DPS tab not found, active tab
# not identifiable, OCR unavailable) aborts the switch and keeps the capture
# already taken, so a live operator is never left stranded on the wrong tab.

# True when a bitmap is a single flat colour (a failed GPU-exclusive
# PrintWindow), sampled on a 10x10 grid. Shared by the main capture and the
# post-switch re-capture.
function Test-BlankBitmap([System.Drawing.Bitmap]$bmp) {
    $first = $bmp.GetPixel(0, 0)
    for ($sx = 0; $sx -lt 10; $sx++) {
        for ($sy = 0; $sy -lt 10; $sy++) {
            $px = $bmp.GetPixel(
                [int](($bmp.Width  - 1) * $sx / 9),
                [int](($bmp.Height - 1) * $sy / 9))
            if ($px.ToArgb() -ne $first.ToArgb()) { return $false }
        }
    }
    return $true
}

# Capture a window's client area via PrintWindow; $null on failure/blank.
function Get-WindowCapture([IntPtr]$hwnd) {
    $cr = New-Object Win32Cap+RECT
    [Win32Cap]::GetClientRect($hwnd, [ref]$cr) | Out-Null
    $cw = $cr.Right - $cr.Left; $ch = $cr.Bottom - $cr.Top
    if ($cw -le 0 -or $ch -le 0) { return $null }
    $bmp = New-Object System.Drawing.Bitmap $cw, $ch
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    $ok = [Win32Cap]::PrintWindow($hwnd, $hdc, 0x3)
    $g.ReleaseHdc($hdc); $g.Dispose()
    if (-not $ok) { $bmp.Dispose(); return $null }
    if (Test-BlankBitmap $bmp) { $bmp.Dispose(); return $null }
    return $bmp
}

# Median of a numeric array (robust to the single active-tab outlier).
function Get-Median($values) {
    $s = @($values | Sort-Object)
    $n = $s.Count
    if ($n -eq 0) { return 0 }
    $mid = [int][Math]::Floor($n / 2)
    if ($n % 2 -eq 1) { return $s[$mid] }
    return (($s[$mid - 1] + $s[$mid]) / 2)
}

# $true when the value strip already shows DPS readings; $false when it is
# empty/other; $null when OCR could not run at all.
function Test-DpsValuesPresent([System.Drawing.Bitmap]$bmp) {
    $rect = New-Object System.Drawing.Rectangle $CropX, $CropY, $CropW, $CropH
    $rect.Intersect((New-Object System.Drawing.Rectangle 0, 0, $bmp.Width, $bmp.Height))
    if ($rect.Width -le 0 -or $rect.Height -le 0) { return $null }
    $crop = $bmp.Clone($rect, $bmp.PixelFormat)
    $scaled = $null
    try {
        $scaled = New-Object System.Drawing.Bitmap `
            ([int]($rect.Width * $ValueOcrScale)), ([int]($rect.Height * $ValueOcrScale))
        $g = [System.Drawing.Graphics]::FromImage($scaled)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.DrawImage($crop, 0, 0, $scaled.Width, $scaled.Height)
        $g.Dispose()
        $tokens = Get-OcrTokens $scaled
        if ($null -eq $tokens) { return $null }
        $numeric = @($tokens | Where-Object { $_.Text -match '\d' })
        if ($script:TabDiag) {
            $seen = (($tokens | ForEach-Object { $_.Text }) -join ' ')
            Write-CapLog ("TESTSWITCH value-check: {0} numeric token(s), need >= {1}  [{2}]" -f `
                $numeric.Count, $DpsMinValueTokens, $seen)
        }
        return ($numeric.Count -ge $DpsMinValueTokens)
    } catch {
        Write-CapLog "WARN value-presence OCR failed: $($_.Exception.Message)"
        return $null
    } finally {
        $crop.Dispose()
        if ($scaled) { $scaled.Dispose() }
    }
}

# OCR the tab strip band and group adjacent word-tokens into tabs. Returns a
# list of { Label, X1, X2, ClickX, ClickY } or $null when nothing was read.
function Get-TabGroups([System.Drawing.Bitmap]$bmp) {
    $band = New-Object System.Drawing.Rectangle 0, $TabStripTop, $bmp.Width, $TabStripHeight
    $tokens = Get-OcrTokens $bmp $band
    if ($null -eq $tokens -or $tokens.Count -eq 0) { return $null }

    $groups = New-Object System.Collections.Generic.List[object]
    $label = ''; $x1 = 0; $x2 = 0; $ys = $null
    foreach ($t in ($tokens | Sort-Object X)) {
        if ($label -eq '') {
            $label = $t.Text; $x1 = $t.X; $x2 = $t.X + $t.W; $ys = @($t.Cy)
        } elseif (($t.X - $x2) -le $TabGroupGapPx) {
            $label += ' ' + $t.Text; $x2 = [Math]::Max($x2, $t.X + $t.W); $ys += $t.Cy
        } else {
            $groups.Add([pscustomobject]@{
                Label = $label; X1 = $x1; X2 = $x2
                ClickX = [int](($x1 + $x2) / 2); ClickY = [int](Get-Median $ys) })
            $label = $t.Text; $x1 = $t.X; $x2 = $t.X + $t.W; $ys = @($t.Cy)
        }
    }
    if ($label -ne '') {
        $groups.Add([pscustomobject]@{
            Label = $label; X1 = $x1; X2 = $x2
            ClickX = [int](($x1 + $x2) / 2); ClickY = [int](Get-Median $ys) })
    }
    return $groups
}

# Identify the currently-active tab as the colour odd-one-out. Samples each
# tab's fill ($TabProbeDy px below its label) and returns the group whose fill
# is farthest from the median fill — or $null when nothing stands out by at
# least $TabActiveMinDelta (so we never guess).
function Find-ActiveTab($groups, [System.Drawing.Bitmap]$bmp) {
    if ($null -eq $groups -or $groups.Count -lt 2) { return $null }
    foreach ($g in $groups) {
        $x = [Math]::Min($bmp.Width - 1,  [Math]::Max(0, $g.ClickX))
        $y = [Math]::Min($bmp.Height - 1, [Math]::Max(0, $g.ClickY + $TabProbeDy))
        $g | Add-Member -NotePropertyName Fill -NotePropertyValue $bmp.GetPixel($x, $y) -Force
    }
    $medR = Get-Median ($groups | ForEach-Object { [int]$_.Fill.R })
    $medG = Get-Median ($groups | ForEach-Object { [int]$_.Fill.G })
    $medB = Get-Median ($groups | ForEach-Object { [int]$_.Fill.B })
    $best = $null; $bestD = -1.0
    foreach ($g in $groups) {
        $dr = [int]$g.Fill.R - $medR; $dg = [int]$g.Fill.G - $medG; $db = [int]$g.Fill.B - $medB
        $d = [Math]::Sqrt($dr * $dr + $dg * $dg + $db * $db)
        if ($script:TabDiag) {
            Write-CapLog ("TESTSWITCH   tab '{0}' fill=({1},{2},{3}) dist={4:0}" -f `
                $g.Label, $g.Fill.R, $g.Fill.G, $g.Fill.B, $d)
        }
        if ($d -gt $bestD) { $bestD = $d; $best = $g }
    }
    if ($script:TabDiag) {
        $bl = if ($best) { $best.Label } else { '<none>' }
        Write-CapLog ("TESTSWITCH   active pick: '{0}' dist={1:0} (threshold {2}); median fill=({3},{4},{5})" -f `
            $bl, $bestD, $TabActiveMinDelta, $medR, $medG, $medB)
    }
    if ($bestD -ge $TabActiveMinDelta) { return $best }
    return $null
}

# Bring a window to the foreground even from a background (scheduled-task)
# process, using the AttachThreadInput unlock trick. Returns the previous
# foreground window handle so the caller can restore it.
function Set-ForegroundForce([IntPtr]$hwnd) {
    $prev = [Win32Cap]::GetForegroundWindow()
    $fgPid = 0
    $fgThread = [Win32Cap]::GetWindowThreadProcessId($prev, [ref]$fgPid)
    $ourThread = [Win32Cap]::GetCurrentThreadId()
    $attached = $false
    if ($fgThread -ne 0 -and $fgThread -ne $ourThread) {
        $attached = [Win32Cap]::AttachThreadInput($ourThread, $fgThread, $true)
    }
    [Win32Cap]::ShowWindow($hwnd, 5) | Out-Null    # SW_SHOW
    [Win32Cap]::BringWindowToTop($hwnd) | Out-Null
    [Win32Cap]::SetForegroundWindow($hwnd) | Out-Null
    if ($attached) { [Win32Cap]::AttachThreadInput($ourThread, $fgThread, $false) | Out-Null }
    return $prev
}

# Click a CLIENT-coordinate point of a window.
#  -Foreground: move the real cursor to the point and left-click (needs the
#   window frontmost; used when the app ignores posted messages). Otherwise
#   POST WM_LBUTTONDOWN/UP to the point without moving the cursor.
function Invoke-ClientClick([IntPtr]$hwnd, [int]$cx, [int]$cy, [switch]$Foreground) {
    if ($Foreground) {
        $pt = New-Object Win32Cap+POINT; $pt.X = $cx; $pt.Y = $cy
        [Win32Cap]::ClientToScreen($hwnd, [ref]$pt) | Out-Null
        [Win32Cap]::SetCursorPos($pt.X, $pt.Y) | Out-Null
        Start-Sleep -Milliseconds 40
        [Win32Cap]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero)   # LEFTDOWN
        Start-Sleep -Milliseconds 40
        [Win32Cap]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero)   # LEFTUP
    } else {
        $lp = [IntPtr](($cy -shl 16) -bor ($cx -band 0xFFFF))
        [Win32Cap]::PostMessage($hwnd, 0x0201, [IntPtr]1, $lp) | Out-Null   # WM_LBUTTONDOWN
        [Win32Cap]::PostMessage($hwnd, 0x0202, [IntPtr]0, $lp) | Out-Null   # WM_LBUTTONUP
    }
}

# Orchestrate: if the value strip is empty, switch to the DPS tab, re-capture,
# then restore the operator's previous tab. Returns
#   @{ Info = <status string>; Bitmap = <new bmp or $null>; Replaced = <bool> }
# and never throws. When Replaced is $true the caller disposes its old bitmap
# and adopts .Bitmap.
function Invoke-DpsOcrSwitch([System.Drawing.Bitmap]$bmp, [IntPtr]$hwnd, [string]$title) {
    $out = @{ Info = 'ocr(?)'; Bitmap = $null; Replaced = $false }
    try {
        $present = Test-DpsValuesPresent $bmp
        if ($null -eq $present) { $out.Info = 'ocr(value-check-failed)'; return $out }

        # Test mode: always show what we detect, even when no switch is needed.
        if ($script:TabDiag) {
            $dg = Get-TabGroups $bmp
            if ($dg) {
                $null = Find-ActiveTab $dg $bmp
                $dd = $dg | Where-Object { $_.Label -match $DpsTabPattern } | Select-Object -First 1
                $ddDesc = if ($dd) { "'$($dd.Label)'@($($dd.ClickX),$($dd.ClickY))" } else { '<not found>' }
                Write-CapLog "TESTSWITCH detect: dps=$ddDesc valuesPresent=$present"
            } else {
                Write-CapLog "TESTSWITCH detect: no tab tokens in band (Top=$TabStripTop H=$TabStripHeight) - adjust the band"
            }
        }

        if ($present) { $out.Info = 'ocr(values-present)'; return $out }

        $groups = Get-TabGroups $bmp
        if ($null -eq $groups -or $groups.Count -eq 0) { $out.Info = 'ocr(no-tab-tokens)'; return $out }

        $dps = $groups | Where-Object { $_.Label -match $DpsTabPattern } | Select-Object -First 1
        if (-not $dps) {
            $labels = ($groups | ForEach-Object { $_.Label }) -join '|'
            $out.Info = "ocr(dps-not-found; tabs=[$labels])"; return $out
        }

        $active = Find-ActiveTab $groups $bmp
        if ($active -and $active.Label -match $DpsTabPattern) {
            # Value check said empty but DPS looks active — don't trust a switch.
            $out.Info = 'ocr(dps-appears-active; no-switch)'; return $out
        }
        if (-not $active -and $OcrSwitchOnlyIfRestorable) {
            $out.Info = 'ocr(active-tab-unknown; switch-skipped)'; return $out
        }

        $activeLbl = if ($active) { $active.Label } else { '?' }
        $activePos = if ($active) { "$($active.ClickX),$($active.ClickY)" } else { '?' }
        Write-CapLog ("OCRSW switching: dps='{0}'@({1},{2}) active='{3}'@({4})" -f `
            $dps.Label, $dps.ClickX, $dps.ClickY, $activeLbl, $activePos)

        $result = $null; $replaced = $false; $usedFg = $false

        # Attempt 1: posted click (no focus steal), verified by re-capture.
        if ($TabClickMode -eq 'post' -or $TabClickMode -eq 'auto') {
            if ($script:TabDiag) { Write-CapLog "TESTSWITCH post-click DPS @($($dps.ClickX),$($dps.ClickY))" }
            Invoke-ClientClick $hwnd $dps.ClickX $dps.ClickY
            Start-Sleep -Milliseconds $TabSettleMs
            $cap = Get-WindowCapture $hwnd
            $capOk = $false
            if ($cap) { $capOk = [bool](Test-DpsValuesPresent $cap) }
            if ($script:TabDiag) { Write-CapLog "TESTSWITCH post result: recaptured=$([bool]$cap) valuesNow=$capOk" }
            if ($capOk) {
                $result = $cap; $replaced = $true
                if ($active) { Invoke-ClientClick $hwnd $active.ClickX $active.ClickY }
            } elseif ($cap) { $cap.Dispose() }
        }

        # Attempt 2: brief foreground + real click, if posting had no effect.
        if (-not $replaced -and ($TabClickMode -eq 'foreground' -or $TabClickMode -eq 'auto')) {
            $usedFg = $true
            if ($script:TabDiag) { Write-CapLog "TESTSWITCH foreground-click DPS @($($dps.ClickX),$($dps.ClickY))" }
            $prevCur = New-Object Win32Cap+POINT
            [Win32Cap]::GetCursorPos([ref]$prevCur) | Out-Null
            $prevFg = $null
            try {
                $prevFg = Set-ForegroundForce $hwnd
                Start-Sleep -Milliseconds 120
                Invoke-ClientClick $hwnd $dps.ClickX $dps.ClickY -Foreground
                Start-Sleep -Milliseconds $TabSettleMs
                $cap = Get-WindowCapture $hwnd
                $capOk = $false
                if ($cap) { $capOk = [bool](Test-DpsValuesPresent $cap) }
                if ($script:TabDiag) { Write-CapLog "TESTSWITCH foreground result: recaptured=$([bool]$cap) valuesNow=$capOk" }
                if ($capOk) { $result = $cap; $replaced = $true }
                elseif ($cap) { $cap.Dispose() }
                if ($active) { Invoke-ClientClick $hwnd $active.ClickX $active.ClickY -Foreground }
            } finally {
                [Win32Cap]::SetCursorPos($prevCur.X, $prevCur.Y) | Out-Null
                if ($prevFg -and $prevFg -ne [IntPtr]::Zero) {
                    [Win32Cap]::SetForegroundWindow($prevFg) | Out-Null
                }
            }
        }

        if ($replaced) {
            $via = if ($usedFg) { 'foreground' } else { 'post' }
            $restored = if ($active) { $active.Label } else { 'none' }
            $out.Bitmap = $result; $out.Replaced = $true
            $out.Info = "ocr(switched via $via; restored='$restored')"
        } else {
            $out.Info = 'ocr(switch-click-had-no-effect)'
        }
        return $out
    } catch {
        Write-CapLog "WARN OCR switch error: $($_.Exception.Message)"
        $out.Info = "ocr(error)"
        return $out
    }
}

# ── Tab restore helpers ──────────────────────────────────────────────────
# Search the window's UIA tree for the tab element named like $name.
# Preference: an exact TabItem, then a TabItem whose name contains $name,
# then any element with that exact name (custom control types).
function Find-TabElement($root, [string]$name) {
    $auto  = [System.Windows.Automation.AutomationElement]
    $ct    = [System.Windows.Automation.ControlType]
    $scope = [System.Windows.Automation.TreeScope]::Descendants

    $condTab = New-Object System.Windows.Automation.PropertyCondition(
        $auto::ControlTypeProperty, $ct::TabItem)

    $condName = New-Object System.Windows.Automation.PropertyCondition(
        $auto::NameProperty, $name)
    $andExact = New-Object System.Windows.Automation.AndCondition($condName, $condTab)
    $hit = $root.FindFirst($scope, $andExact)
    if ($hit) { return $hit }

    foreach ($t in $root.FindAll($scope, $condTab)) {
        if ($t.Current.Name -like "*$name*") { return $t }
    }

    # Custom control types (Button / ListItem / etc.) that carry the name.
    $hit = $root.FindFirst($scope, $condName)
    if ($hit) { return $hit }

    return $null
}

# Post a left-click at the configured tab coordinates (does not move the
# real cursor). Returns a status string, or $null when the fallback is
# disabled (TabClickX/Y not measured).
function Send-TabClick([IntPtr]$hwnd) {
    if ($TabClickX -le 0 -or $TabClickY -le 0) { return $null }
    $lp = [IntPtr](($TabClickY -shl 16) -bor ($TabClickX -band 0xFFFF))
    [Win32Cap]::PostMessage($hwnd, 0x0201, [IntPtr]1, $lp) | Out-Null   # WM_LBUTTONDOWN
    [Win32Cap]::PostMessage($hwnd, 0x0202, [IntPtr]0, $lp) | Out-Null   # WM_LBUTTONUP
    return "postclick($TabClickX,$TabClickY)"
}

# Returns a short status string describing what was done, for the log.
function Restore-DpsTab([IntPtr]$hwnd) {
    if (-not $RestoreTab) { return 'skipped(disabled)' }

    if ($UiaOk) {
        try {
            $auto = [System.Windows.Automation.AutomationElement]
            $root = $auto::FromHandle($hwnd)
            if ($root) {
                $el = Find-TabElement $root $DpsTabName
                if ($el) {
                    $nm = $el.Current.Name
                    # A real tab exposes SelectionItemPattern; select it
                    # WITHOUT moving the mouse or forcing focus.
                    $sel = $null
                    [void]$el.TryGetCurrentPattern(
                        [System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sel)
                    if ($sel) {
                        # Background instance parked on the tab: nothing to
                        # do, and the caller can skip the repaint wait.
                        if ($sel.Current.IsSelected) { return "already-selected('$nm')" }
                        $sel.Select(); return "uia-select('$nm')"
                    }
                    # Button-like tab: invoke it.
                    $inv = $null
                    [void]$el.TryGetCurrentPattern(
                        [System.Windows.Automation.InvokePattern]::Pattern, [ref]$inv)
                    if ($inv) { $inv.Invoke(); return "uia-invoke('$nm')" }
                    # Pattern-less match (the page itself, or a custom
                    # canvas). A page element that is on screen means the
                    # DPS page is already the one showing — nothing to do.
                    if (-not $el.Current.IsOffscreen) { return "already-visible('$nm')" }
                    $click = Send-TabClick $hwnd
                    if ($click) { return "$click after uia-found-no-pattern('$nm')" }
                    return "uia-found-no-pattern('$nm')"
                }
            }
        } catch {
            Write-CapLog "WARN UIA tab restore error: $($_.Exception.Message)"
        }
    }

    # Fallback: post a left-click to the tab's client coordinates. Works on
    # a background window for apps that honor posted mouse messages.
    $click = Send-TabClick $hwnd
    if ($click) { return $click }

    return 'no-tab-found'
}

# How a window relates to the DPS tab: 'selected' (tab exists and is the
# one being shown), 'present' (exists but another tab is shown), 'absent',
# or 'unknown' when UIA cannot answer. Used to choose between several
# matching aView windows without touching any of them.
function Get-DpsTabState([IntPtr]$hwnd) {
    if (-not $UiaOk) { return 'unknown' }
    try {
        $auto = [System.Windows.Automation.AutomationElement]
        $root = $auto::FromHandle($hwnd)
        if (-not $root) { return 'absent' }
        $el = Find-TabElement $root $DpsTabName
        if (-not $el) { return 'absent' }
        $sel = $null
        [void]$el.TryGetCurrentPattern(
            [System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sel)
        if ($sel) { if ($sel.Current.IsSelected) { return 'selected' } return 'present' }
        # Pattern-less match (page pane / custom canvas): being on screen
        # means that page is the one currently shown.
        if (-not $el.Current.IsOffscreen) { return 'selected' }
        return 'present'
    } catch { return 'unknown' }
}

# -InspectTabs: dump named, clickable/selectable elements so the operator
# can read off the exact tab label to configure in $DpsTabName.
function Inspect-Tabs([IntPtr]$hwnd, [string]$title) {
    if (-not $UiaOk) { Write-CapLog "INSPECT aborted: UI Automation unavailable"; return }
    $auto  = [System.Windows.Automation.AutomationElement]
    $scope = [System.Windows.Automation.TreeScope]::Descendants
    $root  = $auto::FromHandle($hwnd)
    if (-not $root) { Write-CapLog "INSPECT aborted: no UIA root for window"; return }
    Write-CapLog "INSPECT named elements of '$title':"
    $n = 0
    foreach ($e in $root.FindAll($scope, [System.Windows.Automation.Condition]::TrueCondition)) {
        try {
            $name = $e.Current.Name
            if ([string]::IsNullOrWhiteSpace($name)) { continue }
            $type = $e.Current.ControlType.ProgrammaticName   # e.g. ControlType.TabItem
            if ($type -match 'Tab|Button|List|Menu|Radio|Custom') {
                Write-CapLog ("       [{0}] '{1}'" -f ($type -replace '^ControlType\.', ''), $name)
                $n++
            }
        } catch { }
    }
    Write-CapLog "INSPECT done ($n elements). Copy the DPS tab's text into `$DpsTabName."
}

# ── Workday folder / hour naming (unchanged behavior) ────────────────────
# Workday spans 8 AM today → 7 AM next morning. Hours 0..7 of the next
# calendar day belong to the workday that started yesterday, so they must
# be written into yesterday's folder (the C# app keeps _imageFolderPath
# pinned to the session start date until the user clicks button_New).
# Midnight (00) is stored as "24" by convention.
$now = Get-Date
if ($now.Hour -eq 0) {
    $hourStr    = "24"
    $folderDate = $now.AddDays(-1).ToString("yyyy-MM-dd")
} elseif ($now.Hour -ge 1 -and $now.Hour -le 7) {
    $hourStr    = "{0:D2}" -f $now.Hour
    $folderDate = $now.AddDays(-1).ToString("yyyy-MM-dd")
} else {
    $hourStr    = "{0:D2}" -f $now.Hour
    $folderDate = $now.ToString("yyyy-MM-dd")
}

$OutputDir = Join-Path $ScriptDir "Screenshots-$folderDate"
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}
$file = if ($OutFile) { $OutFile } else { Join-Path $OutputDir "$hourStr.png" }

# ── Self-healing capture instance ────────────────────────────────────────
# Start the dedicated read-only copy when it is missing, so the operator's
# window stops being the capture target from the next hour on.
if ($AutoStartCapture -and -not $Calibrate -and -not $InspectTabs -and -not $CalibrateTabs -and -not $TestSwitch -and
    $CapMarker -and (Test-Path $HmiExePath)) {
    try {
        $running = @(Get-CimInstance Win32_Process -Filter "Name='$ProcName.exe'" |
                     Where-Object { $_.CommandLine -like "*$CapMarker*" })
        if ($running.Count -eq 0) {
            Start-Process -FilePath $HmiExePath -ArgumentList $HmiExeArgs
            Write-CapLog "INFO capture instance ($CapMarker) not running - started '$HmiExePath'; it becomes the capture target once logged in"
        }
    } catch { Write-CapLog "WARN capture-instance auto-start failed: $($_.Exception.Message)" }
}

# ── Locate the SCADA window ──────────────────────────────────────────────
$pattern = if ($WindowTitle) { $WindowTitle } else { $DefaultWindowTitle }
$allWindows = [Win32Cap]::GetTopWindows()

$procMap = @{}
foreach ($p in Get-Process) { $procMap[[uint32]$p.Id] = $p.ProcessName }

# Visible top-level windows matched either by owning process name or by
# title pattern. Entries are "handle|pid|title".
function Get-CandidateWindows([bool]$byTitle) {
    $found = @()
    foreach ($entry in $allWindows) {
        $parts = $entry.Split([char]'|', 3)
        $hwnd = [IntPtr][long]$parts[0]
        $procId = [uint32]$parts[1]
        $title = $parts[2]
        if ($byTitle) { if ($title -notlike $pattern) { continue } }
        elseif ($procMap[$procId] -ne $ProcName) { continue }
        $r = New-Object Win32Cap+RECT
        [Win32Cap]::GetWindowRect($hwnd, [ref]$r) | Out-Null
        $area = [math]::Max(0, $r.Right - $r.Left) * [math]::Max(0, $r.Bottom - $r.Top)
        $found += [pscustomobject]@{ Hwnd = $hwnd; Pid = $procId; Title = $title; Area = $area }
    }
    return ,$found
}

# Explicit -WindowTitle forces title matching (testing); otherwise match by
# process, falling back to the title pattern when the process has no window.
if ($WindowTitle) {
    $candidates = Get-CandidateWindows $true
    $matchDesc = "title '$pattern'"
} else {
    $candidates = Get-CandidateWindows $false
    $matchDesc = "process '$ProcName.exe'"
    if ($candidates.Count -eq 0) {
        Write-CapLog "WARN no visible window owned by '$ProcName.exe' - trying title pattern '$pattern'"
        $candidates = Get-CandidateWindows $true
        $matchDesc = "title '$pattern' (process fallback)"
    }
}

$target = $null; $targetTitle = ''
if ($candidates.Count -eq 1) {
    $target = $candidates[0].Hwnd; $targetTitle = $candidates[0].Title
} elseif ($candidates.Count -gt 1) {
    # Several matches: operator copy + capture copy (or splash/popup). The
    # dedicated capture copy carries the read-only user name on its command
    # line, so it is recognized without touching any window at all.
    $best = $null
    if ($CapMarker) {
        $cmdMap = @{}
        try {
            foreach ($wp in Get-CimInstance Win32_Process -Filter "Name='$ProcName.exe'") {
                $cmdMap[[uint32]$wp.ProcessId] = [string]$wp.CommandLine
            }
        } catch { }
        $marked = @($candidates | Where-Object { $cmdMap[$_.Pid] -like "*$CapMarker*" })
        if ($marked.Count -gt 0) {
            $best = $marked | Sort-Object Area -Descending | Select-Object -First 1
            $best | Add-Member NoteProperty Dps 'cmdline'
        }
    }
    # No marked instance: prefer the window already SHOWING the DPS tab,
    # then one that at least HAS it, and only then the largest — so the
    # operator's window is captured (and its tab switched) only when
    # nothing better exists. Probe from the BOTTOM of the z-order
    # (EnumWindows returns topmost first) and stop at the first 'selected'
    # hit: the parked copy sits behind the operator's, so its window is
    # normally the only one UIA-probed — probing can disturb some UI
    # frameworks.
    if (-not $best) {
        for ($i = $candidates.Count - 1; $i -ge 0; $i--) {
            $c = $candidates[$i]
            $state = Get-DpsTabState $c.Hwnd
            $rank = 0
            if ($state -eq 'selected') { $rank = 2 }
            elseif ($state -eq 'present') { $rank = 1 }
            $c | Add-Member NoteProperty Dps  $state
            $c | Add-Member NoteProperty Rank $rank
            if ($rank -eq 2) { $best = $c; break }
        }
    }
    if (-not $best) {
        $best = $candidates | Sort-Object Rank, Area -Descending | Select-Object -First 1
    }
    $target = $best.Hwnd; $targetTitle = $best.Title
    Write-CapLog ("INFO {0} windows match {1} - picked '{2}' (dps={3})" -f `
        $candidates.Count, $matchDesc, $best.Title, $best.Dps)
}

$bitmap = $null
$method = ''
$tabInfo = 'no-window'

if ($target) {
    # A minimized window has a bogus client rect and PrintWindow renders
    # nothing useful — restore it WITHOUT giving it focus, then put it back.
    $wasMinimized = [Win32Cap]::IsIconic($target)
    if ($wasMinimized) {
        [Win32Cap]::ShowWindow($target, 4) | Out-Null   # SW_SHOWNOACTIVATE
        Start-Sleep -Milliseconds 600
    }

    # Discovery mode: dump the UIA tree and stop before capturing.
    if ($InspectTabs) {
        Inspect-Tabs $target $targetTitle
        if ($wasMinimized) { [Win32Cap]::ShowWindow($target, 7) | Out-Null }
        Write-Output "Element list written to $LogFile"
        return
    }

    # Select the LOCAL/REMOTE DPS tab, then let aView repaint it. No wait
    # needed when the tab was already showing (background instance).
    # -CalibrateTabs captures the CURRENT state untouched, so the natural tab
    # layout (and whichever tab happens to be active) can be measured.
    if ($CalibrateTabs) {
        $tabInfo = 'caltabs(no-restore)'
    } elseif ($TestSwitch) {
        # Capture the current state untouched; the forced switch runs after.
        $tabInfo = 'testswitch(pending)'
    } elseif ($OcrTabSwitch -and (Initialize-WinRtOcr)) {
        # Decided AFTER the first capture: only switch when the value strip is
        # empty (see the OCR-driven switch block further down). When OCR is
        # unavailable this branch is skipped, so we fall back to legacy UIA.
        $tabInfo = 'ocr(pending)'
    } else {
        $tabInfo = Restore-DpsTab $target
        if ($RestoreTab -and $tabInfo -notlike 'skipped*' -and
            $tabInfo -notlike 'already-*') { Start-Sleep -Milliseconds $TabSettleMs }
    }

    $cr = New-Object Win32Cap+RECT
    [Win32Cap]::GetClientRect($target, [ref]$cr) | Out-Null
    $cw = $cr.Right - $cr.Left; $ch = $cr.Bottom - $cr.Top

    if ($cw -gt 0 -and $ch -gt 0) {
        $bitmap = New-Object System.Drawing.Bitmap $cw, $ch
        $g = [System.Drawing.Graphics]::FromImage($bitmap)
        $hdc = $g.GetHdc()
        # 0x1 PW_CLIENTONLY | 0x2 PW_RENDERFULLCONTENT: DWM renders the full
        # client area even when the window is covered or on another screen.
        $ok = [Win32Cap]::PrintWindow($target, $hdc, 0x3)
        $g.ReleaseHdc($hdc)
        $g.Dispose()
        if (-not $ok) { $bitmap.Dispose(); $bitmap = $null }
    }

    # Reject an all-one-color (black/white) frame: some GPU-exclusive
    # renderers defeat PrintWindow, better to fall back than store garbage.
    if ($bitmap) {
        if (Test-BlankBitmap $bitmap) {
            Write-CapLog "WARN blank PrintWindow frame from '$targetTitle' - falling back to screen copy"
            $bitmap.Dispose(); $bitmap = $null
        } else {
            $method = "PrintWindow('$targetTitle')"
        }
    }

    if (-not $bitmap) {
        # Fallback: copy the pixels of the screen the window lives on
        # (only correct when the window is actually visible there).
        $scr = [System.Windows.Forms.Screen]::FromHandle($target)
        $bitmap = New-Object System.Drawing.Bitmap $scr.Bounds.Width, $scr.Bounds.Height
        $g = [System.Drawing.Graphics]::FromImage($bitmap)
        $g.CopyFromScreen($scr.Bounds.Location, [System.Drawing.Point]::Empty, $scr.Bounds.Size)
        $g.Dispose()
        $method = "ScreenCopy($($scr.DeviceName)) after PrintWindow failure"
    }

    # ── OCR-driven DPS tab switch (Stage 2) ──────────────────────────────
    # Runs only when enabled AND we have a genuine PrintWindow client capture
    # (OCR box coords must map to client pixels for the click to land). If the
    # value strip is already populated it does nothing; otherwise it switches
    # to DPS, re-captures, and restores the operator's previous tab.
    if (($OcrTabSwitch -or $TestSwitch) -and -not $Calibrate -and -not $CalibrateTabs -and
        $method -like 'PrintWindow*' -and (Initialize-WinRtOcr)) {
        if ($TestSwitch) {
            $script:TabDiag = $true
            $beforeFile = Join-Path $ScriptDir 'testswitch-before.png'
            $bitmap.Save($beforeFile, [System.Drawing.Imaging.ImageFormat]::Png)
            Write-CapLog "TESTSWITCH before -> $beforeFile ($($bitmap.Width)x$($bitmap.Height)) title='$targetTitle'"
        }
        $sw = Invoke-DpsOcrSwitch $bitmap $target $targetTitle
        $tabInfo = $sw.Info
        if ($sw.Replaced -and $sw.Bitmap) {
            if ($TestSwitch) {
                $afterFile = Join-Path $ScriptDir 'testswitch-after.png'
                $sw.Bitmap.Save($afterFile, [System.Drawing.Imaging.ImageFormat]::Png)
                Write-CapLog "TESTSWITCH after  -> $afterFile"
            }
            $bitmap.Dispose(); $bitmap = $sw.Bitmap
        } elseif ($TestSwitch) {
            Write-CapLog "TESTSWITCH no image replacement (result: $tabInfo)"
        }
    }

    # Put a window we un-minimized back the way we found it — done LAST so the
    # switch above can re-capture it while it is still shown.
    if ($wasMinimized) {
        [Win32Cap]::ShowWindow($target, 7) | Out-Null   # SW_SHOWMINNOACTIVE
    }
} else {
    if ($InspectTabs) {
        Write-CapLog "INSPECT aborted: no window matches pattern '$pattern'"
        Write-Output "No aView window found - check `$DefaultWindowTitle."
        return
    }
    if ($TestSwitch) {
        Write-CapLog "TESTSWITCH aborted: no window matches $matchDesc"
        Write-Output "TestSwitch: no aView window found."
        return
    }
    # No window matched: dump every process + title so the config can be
    # fixed from the log, then fall back to a raw screen copy so the hour
    # is not lost.
    Write-CapLog "WARN no window matches $matchDesc. Visible windows:"
    foreach ($entry in $allWindows) {
        $parts = $entry.Split([char]'|', 3)
        Write-CapLog ("       [{0}.exe] {1}" -f $procMap[[uint32]$parts[1]], $parts[2])
    }
    $screens = [System.Windows.Forms.Screen]::AllScreens
    $scr = $screens | Where-Object { -not $_.Primary } | Select-Object -First 1
    if ($FallbackScreen -ne 'Secondary' -or -not $scr) {
        $scr = [System.Windows.Forms.Screen]::PrimaryScreen
    }
    $bitmap = New-Object System.Drawing.Bitmap $scr.Bounds.Width, $scr.Bounds.Height
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.CopyFromScreen($scr.Bounds.Location, [System.Drawing.Point]::Empty, $scr.Bounds.Size)
    $g.Dispose()
    $method = "ScreenCopy($($scr.DeviceName)) - window not found"
}

# ── TestSwitch mode: one-shot switch-and-restore, no hourly file written ──
if ($TestSwitch) {
    Write-CapLog "TESTSWITCH done: tab=$tabInfo method=$method"
    if ($bitmap) { $bitmap.Dispose() }
    Write-Output "TestSwitch complete: $tabInfo"
    Write-Output "  Details in capture.log; images: testswitch-before.png / testswitch-after.png"
    return
}

# ── OCR tab-discovery mode ───────────────────────────────────────────────
# Save the whole frame and log every OCR token (text + box + colour) so the
# tab strip, the DPS tab's click point and the active-tab highlight can be
# measured from a single run. Switches nothing; safe to run any time.
if ($CalibrateTabs) {
    $calFile = if ($OutFile) { $OutFile } else { Join-Path $ScriptDir 'calibrate-tabs.png' }
    $bitmap.Save($calFile, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-CapLog "CALTABS $method tab=$tabInfo frame=$($bitmap.Width)x$($bitmap.Height) -> $calFile"

    if (Initialize-WinRtOcr) {
        try {
            $langs = [Windows.Media.Ocr.OcrEngine]::AvailableRecognizerLanguages |
                     ForEach-Object { $_.LanguageTag }
            Write-CapLog ("CALTABS OCR languages: {0}" -f ($langs -join ', '))
        } catch { }

        $tokens = Get-OcrTokens $bitmap
        if ($null -eq $tokens) {
            Write-CapLog 'CALTABS OCR produced no result (see WARN above) - measure the tabs from the PNG'
        } else {
            Write-CapLog "CALTABS $($tokens.Count) token(s):"
            foreach ($t in $tokens) {
                $col = $bitmap.GetPixel(
                    [Math]::Min($bitmap.Width - 1, [Math]::Max(0, $t.Cx)),
                    [Math]::Min($bitmap.Height - 1, [Math]::Max(0, $t.Cy)))
                Write-CapLog ("CALTABS   '{0}' box=({1},{2},{3},{4}) mid=({5},{6}) rgb=({7},{8},{9})" -f `
                    $t.Text, $t.X, $t.Y, $t.W, $t.H, $t.Cx, $t.Cy, $col.R, $col.G, $col.B)
            }
            $hits = @($tokens | Where-Object { $_.Text -match '(?i)dps|local|remote' })
            foreach ($h in $hits) {
                Write-CapLog ("CALTABS >> DPS-candidate '{0}' click~({1},{2})" -f $h.Text, $h.Cx, $h.Cy)
            }
        }
    } else {
        Write-CapLog 'CALTABS OCR unavailable - PNG saved; measure the tab strip manually from it'
    }

    $bitmap.Dispose()
    Write-Output "Saved $calFile - tab OCR tokens written to capture.log."
    return
}

# ── Calibration mode: save the whole frame and stop ──────────────────────
if ($Calibrate) {
    $calFile = Join-Path $ScriptDir 'calibrate-full.png'
    $bitmap.Save($calFile, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-CapLog "CALIBRATE $method tab=$tabInfo -> $calFile ($($bitmap.Width)x$($bitmap.Height))"
    $bitmap.Dispose()
    Write-Output "Saved full frame to $calFile - measure CropX/CropY/CropW/CropH there."
    return
}

# ── Crop and save ────────────────────────────────────────────────────────
$rect = New-Object System.Drawing.Rectangle $CropX, $CropY, $CropW, $CropH
$bounds = New-Object System.Drawing.Rectangle 0, 0, $bitmap.Width, $bitmap.Height
$rect.Intersect($bounds)
if ($rect.Width -le 0 -or $rect.Height -le 0) {
    Write-CapLog "ERROR crop rect ($CropX,$CropY,$CropW,$CropH) is outside the $($bitmap.Width)x$($bitmap.Height) capture - nothing saved"
    $bitmap.Dispose()
    return
}
if ($rect.Width -ne $CropW -or $rect.Height -ne $CropH) {
    Write-CapLog "WARN crop rect clipped to $rect (capture is $($bitmap.Width)x$($bitmap.Height))"
}

$cropped = $bitmap.Clone($rect, $bitmap.PixelFormat)
$cropped.Save($file, [System.Drawing.Imaging.ImageFormat]::Png)
Write-CapLog "OK hour=$hourStr method=$method tab=$tabInfo -> $file"

$bitmap.Dispose()
$cropped.Dispose()
