# Substation OCR — architecture

One program gathers the hourly readings from a SCADA display and hands them to a
phone as a QR code. It runs on both substation PCs — the 132 kV and the 33 kV —
with a different config on each. There is no operator application any more:
nothing is printed, nothing is exported to Excel.

## One program, two PCs

```
 ┌─────────────────────────────────────┐      ┌─────────────────────────────────────┐
 │  132 kV PC                          │      │  33 kV PC                           │
 │                                     │      │                                     │
 │  main screen   — operator desktop   │      │  main screen   — operator desktop   │
 │  SECOND screen — 132 kV wall view   │      │  SECOND screen — 33 kV wall view    │
 │            │              ▲         │      │            │              ▲         │
 │            ▼              │         │      │            ▼              │         │
 │  SubstationOcr.exe        │         │      │  SubstationOcr.exe        │         │
 │   nodeId "S132"           │         │      │   nodeId "S33"            │         │
 │   • reads its SECOND      │         │      │   • reads its SECOND      │         │
 │     screen at :02         │         │      │     screen at :02         │         │
 │   • keeps the hour on     │         │      │   • keeps the hour on     │         │
 │     its own disk          │         │      │     its own disk          │         │
 │   • Ctrl+Shift+7+8+9 ─────┘         │      │   • Ctrl+Shift+7+8+9 ─────┘         │
 │     flashes columns 1–8             │      │     flashes columns 9–24            │
 │     as a QR on the MAIN screen      │      │     as a QR on the MAIN screen      │
 └──────────────────┬──────────────────┘      └──────────────────┬──────────────────┘
                    │                                            │
                    │   no network link between the PCs          │
                    └─────────────────────┬──────────────────────┘
                                          ▼
                                Android companion app
                             scans both codes and merges
                             them by date into one sheet
```

| | 132 kV PC | 33 kV PC |
|---|---|---|
| Executable | `SubstationOcr.exe` | `SubstationOcr.exe` |
| Config beside the exe | `node.config.json` ← `Config\node-132kv.config.json` | `node.config.json` ← `Config\node-33kv.config.json` |
| `nodeId` | `S132` | `S33` |
| ROI file | `Config\roi-132kv.json` | `Config\roi-33kv.json` |
| Screen read | its own **secondary** screen | its own **secondary** screen |
| QR columns | 1–8 | 9–24 |
| QR display | full screen on the **main** screen | full screen on the **main** screen |
| Hotkey | Ctrl+Shift+7 → 8 → 9 | Ctrl+Shift+7 → 8 → 9 |

The two copies are the same build; only `node.config.json` differs. The capture,
OCR, storage and QR code live in `Shared/`, and the program around them in
`SubstationOcr/`.

A config is never written for you. Started without `node.config.json` the
program says which file to copy and exits, because a default would have to be
one of the two PCs, and a 33 kV PC quietly running as 132 kV reads the wrong
boxes all night.

## Why there is no link

The previous design had the 33 kV node push its values to the 132 kV node over
TCP 5115, and one code carried the whole sheet. The substation PCs rule that
out: both run local accounts without administrator rights, Windows Firewall
blocks inbound connections to either of them (ping answers, a TCP connection
does not), and adding a firewall rule or creating a shared folder both need an
administrator. Nothing the node could do from a standard account gets a value
from one PC to the other.

So the join moved to the phone, which already carries the logsheet: each PC
shows its own half, and the app fills one sheet from both scans. The networked
version is kept, unchanged, on the `V2-client-server` branch.

## Screens

Each node reads the **secondary** screen — the head carrying the SCADA wall
view — and leaves the main screen alone. That is `"captureScreen": "secondary"`
in both configs, which resolves to the first non-primary display.

The main screen is used only when someone asks for something: the QR flash, and
the calibration window. The QR appears centred, borderless and on top, and it
takes **no focus**: the SCADA application underneath keeps the keyboard the
whole time.

If a PC has only one display attached, "secondary" has nothing to resolve to.
It falls back to the primary screen and says so in the log rather than silently
reading the wrong thing for days.

## The run window: 07:00 to 07:00

A session runs from **07:00 to 07:00 the next morning** — 24 readings, taken at
**:02** past each hour, in this order:

```
07 08 09 10 11 12 13 14 15 16 17 18 19 20 21 22 23 00 01 02 03 04 05 06
```

The session is pinned when a node starts and never recomputed. A node left
running past 07:00 goes quiet — it does not roll into the next day on its own.
**Starting the next session is a manual act**: someone starts the program on
both PCs again.

That is deliberate, and it is why the node never schedules itself for tomorrow.
Both PCs work out the session date the same way, which matters now: it is the
date the phone merges the two scans by.

By default a node that reaches 07:00 stays in memory but idle, so the QR hotkey
still works at exactly the moment the day is read. Set `exitWhenSessionEnds` to
`true` if you would rather the process quit.

`runAtLogon` (off by default) registers the node under the per-user `Run` key so
it survives a reboot. That is a different thing from the daily restart: it fires
on logon, not at 07:00, so on a machine that stays logged in it never fires
again and tomorrow's session is still started by hand.

## Only the marked boxes are read

Each PC carries an ROI file listing the boxes it reads. Nothing else on the
screen is touched — these are the red-outlined boxes on the two wall views.

**132 kV wall view** — `SubstationOcr/Config/roi-132kv.json`

| ROI | SCADA label | Rows | → columns |
|---|---|---|---|
| `YARMJA` | OHL-1 OLD YARMJA | KV, A, MW, MVAR | 1–4 |
| `QAYARA` | OHL-2 QAYARA | KV, A, MW, MVAR | 5–8 |

**33 kV wall view** — `SubstationOcr/Config/roi-33kv.json`

| ROI | SCADA label | Rows | → columns |
|---|---|---|---|
| `BUS1A` | BUS 1A busbar voltage | KV | 9 |
| `T1` | TRANSFORMER INCOMER-1 | A, MW, MVAR | 10–12 |
| `BUS2A` | BUS 2A busbar voltage | KV | 13 |
| `T2` | TRANSFORMER INCOMER-2 | A, MW, MVAR | 14–16 |
| `BUS2B` | BUS 2B busbar voltage | KV | 17 |
| `T3` | TRANSFORMER INCOMER-3 | A, MW, MVAR | 18–20 |
| `FDR_DOMEZ` | CABLE FEEDER-1 | A, MW, MVAR | 21 (MW) |
| `FDR_SUMMER` | CABLE FEEDER-6 | A, MW, MVAR | 22 (MW) |
| `FDR_SALAM1` | CABLE FEEDER-7 | A, MW, MVAR | 23 (MW) |
| `FDR_SALAM2` | CABLE FEEDER-12 | A, MW, MVAR | 24 (MW) |

The red boxes on the 33 kV view carry **A, MW and MVAR only** — no kV row. The
three busbar boxes fill the KV columns for T1, T2 and T3 (BUS 1A → T1,
BUS 2A → T2, BUS 2B → T3). They are the only ROIs that are not red-outlined,
and they are marked as such in the file. Set `"enabled": false` on them if those
columns are not wanted.

## Channels and columns

A node reports **channels**, named `<ROI>.<row>` — `T1.MW`, `YARMJA.KV`. The
`columns` block in `node.config.json` binds a logsheet column to one channel on
one node:

```json
{ "col": 11, "source": "S33", "channel": "T1.MW", "label": "T1 MW", "busClass": "33" }
```

A PC fills only the columns whose `source` is its own `nodeId`. The shipped
configs list just those — columns 1–8 in the 132 kV one, 9–24 in the 33 kV one.
A config listing all 24 works too: the other PC's columns simply stay empty. A
config that gives a PC no columns at all is reported in the log at start-up,
and again on the QR surface when someone asks for the code.

## How a value is read

1. Capture the secondary screen.
2. Crop the ROI. If the screen resolution differs from the one the ROIs were
   calibrated at, the rectangle is scaled proportionally first.
3. Split the crop into its declared rows — a three-row box becomes three bands.
4. Per band: upscale (6× for the small 33 kV boxes), take the **green channel**
   (the text is green phosphor on black, which separates far better than
   luminance), Otsu-binarise, invert to dark-on-light, add a quiet margin.
5. OCR that single line with page segmentation mode 7.
6. Normalise the text, correcting the glyph confusions this typeface produces
   (`O`→`0`, `l`→`1`, `S`→`5`, `,`→`.`, and the Unicode dashes Tesseract emits
   for a minus sign), then take the longest numeric run so a border artefact
   ahead of the value cannot win.

If a row comes back empty the whole box is re-read as one block and the gaps
filled positionally — but only if that pass found exactly as many numbers as the
box declares rows. Anything else would be guessing which number belongs to which
row.

Every reading is kept on the PC that took it, as
`Data\Readings-yyyy-MM-dd\HH-NODEID.json`. A node restarted mid-session picks
those hours back up, so its code still carries the whole session.

## Checking the boxes

On each PC the tray menu offers **Check this PC's boxes**. It reads the screen,
runs the OCR, and opens a window showing the capture with every ROI drawn on it:

- **green** every row in the box read, **amber** some, **red** none, **grey**
  switched off;
- the value each row produced, printed beside the box — `T1  A=200.00
  MW=-10.77  MVAR=3.20`, and `MW=?` where nothing came out;
- a header stamping the node, machine, screen, ROI reference size and the time.

The window zooms (wheel, `+`/`-`, double-click for 100%) and pans, because a box
that clips a digit is a few pixels wrong, not obviously wrong.

Every shot is kept as a PNG under `Calibration\` on the PC that took it.

## The QR handoff

Hold **Ctrl+Shift**, then press **7**, **8**, **9** — each within three seconds
of the last. The node builds the payload from everything this PC has gathered
so far and shows it full screen for `qrSeconds` (3–5, default 5). The same
sequence takes it down again; so do Esc and a click. The caption above the code
names the substation, the node and the session date, so the operator can see
which PC's code is up.

The payload is **`LS1`**, the format the existing Android companion app already
parses, taken unchanged from the PreV2 branch:

```
LS1 <YYYYMMDD>*[-]HH:c1.c2. ... .c24*[-]HH:...
```

Each PC writes all 24 cells of every row and fills only its own:

```
132 kV PC:  LS1 20260913*07:129.0.0.0.128.171.35.9................
33 kV PC:   LS1 20260913*07:........33.200.10.3.33.185.9.2.33.170.8.2.4..2.1
```

The phone merges them: when a scan has the same date as the sheet on screen, its
non-empty cells overwrite and its empty cells keep what is already there. An
empty cell never erases a value, so scanning the same PC again later in the day
only adds its newer hours.

Every character stays inside the QR alphanumeric table, which keeps the symbol
in alphanumeric mode — two characters per 11 bits instead of eight bits each.
Measured with every cell three digits wide, a full day is version 23 on the
132 kV PC and version 29 on the 33 kV PC at ECC M. Error correction is M,
stepping down to L only if the session will not fit.
There is no splitting across codes: the app parses one payload per scan.

Full contract, the merge rule, and the one line the phone needs changed for the
07:00 day, in [QR-ANDROID.md](QR-ANDROID.md).

### The hotkey

`RegisterHotKey` binds modifiers plus exactly one key, so the combination is
registered as three separate hotkeys — Ctrl+Shift+7, +8, +9, on both the top row
and the numpad — and turned into a sequence: 7 arms, 8 confirms, 9 fires. Out of
order, or more than three seconds apart, resets it.

This is the PreV2 mechanism kept as-is. A low-level keyboard hook would also
work, but it sits in the input path of the entire SCADA desktop; this does not.
Ctrl rather than Alt on purpose — Alt+Shift is the input-language toggle on
Arabic systems.

The registrations are owned by a window that is created but never shown, and
re-registered if Windows ever recreates its handle. If another program already
holds the keys — the old WinLogosheet does — the log says so and the code can
still be shown from the tray menu.

## Files

```
Shared/Protocol/     data types
  Json.cs            JSON via System.Web.Extensions — no NuGet needed
  ReadingFrame.cs    what a node reads for one hour
  RoiDefinition.cs   the ROI file model
  ColumnMap.cs       channel → column bindings
  NumberFormat.cs    OCR text → number
Shared/Capture/      capture and OCR
  NodeConfig.cs      node.config.json
  SessionClock.cs    the 07:00 → 07:00 window and hour ordering
  ScreenGrabber.cs   secondary-screen resolution, capture, crop, upscale
  RoiOcrEngine.cs    row banding, binarisation, Tesseract
  CaptureService.cs  one hourly reading, calibration shot
  CalibrationShot.cs what a calibration is: boxes, values, the annotated picture
  HourStore.cs       on-disk store, keyed by hour and node
  HourlyScheduler.cs the :02 trigger, bounded by the run window
  NodeLog.cs         day-stamped log file
  NodeEnvironment.cs .NET, Windows and Visual C++ runtime, for the log
  TessData.cs        finds Tesseract's language data
  HiddenHost.cs      windowless host, optional tray icon
  LogonAutostart.cs  optional per-user Run entry
Shared/Qr/
  QrEncoder.cs       self-contained QR encoder, byte and alphanumeric modes
  LogsheetQr.cs      the LS1 payload the phone scans

SubstationOcr/         the program, the same on both PCs
  Program.cs           start-up, tray menu and QR assembly
  SessionSheet.cs      this PC's hours → 24-column rows, own columns only
  HotkeySink.cs        Ctrl+Shift+7 → 8 → 9, via RegisterHotKey
  QrFlashWindow.cs     the QR code, and why there is none
  CalibrationWindow.cs a calibration shot, zoomable
  Config/              node-132kv.config.json, node-33kv.config.json,
                       roi-132kv.json, roi-33kv.json
```

`WinLogosheet/` is the original operator application. It is **not part of this
pipeline** — no printing, no Excel export, no grid. It is left in the repository
untouched, outside the solution, and nothing here depends on it.
