# Substation OCR — architecture

Two applications gather the hourly readings from two SCADA displays. There is no
operator application any more: nothing is printed, nothing is exported to Excel.
The system gathers values and hands them to a phone as a QR code.

## The two nodes

```
 ┌─────────────────────────────────────┐      ┌─────────────────────────────────────┐
 │  33 kV SERVER          [ CLIENT ]   │      │  132 kV SERVER         [ SERVER ]   │
 │                                     │      │                                     │
 │  main screen   — operator desktop   │      │  main screen   — operator desktop   │
 │  SECOND screen — 33 kV wall view    │      │  SECOND screen — 132 kV wall view   │
 │            │                        │      │            │              ▲         │
 │            ▼                        │      │            ▼              │         │
 │  SubstationOcrClient.exe            │      │  SubstationOcrServer.exe  │         │
 │   nodeId "S33"                      │      │   nodeId "S132"           │         │
 │   • reads its SECOND screen at :02  │      │   • reads its SECOND      │         │
 │   • OCRs only the marked boxes      │      │     screen at :02         │         │
 │   • PUSHES the values ─────────────────────▶ • LISTENS on TCP 5115     │         │
 │   • queues and retries if the       │      │   • merges both nodes     │         │
 │     link is down                    │      │   • Ctrl+Shift+7+8+9 ─────┘         │
 │                                     │      │     flashes the QR on the MAIN      │
 │   no window, no tray icon           │      │     screen for 3-5 s, then hides    │
 └─────────────────────────────────────┘      └─────────────────────────────────────┘
```

| | 33 kV node | 132 kV node |
|---|---|---|
| Executable | `SubstationOcrClient.exe` | `SubstationOcrServer.exe` |
| Socket role | client — connects out and pushes | server — listens on TCP 5115 |
| Screen read | its own **secondary** screen | its own **secondary** screen |
| Holds the session | its own hours only | **both** nodes' hours |
| QR display | — | flashes on the **main** screen |
| Hotkey | — | Ctrl+Shift+7+8+9 |

Both are the same underneath: the capture, OCR and storage code lives in
`Shared/` and is compiled into each. Only the socket role, the QR display and
the hotkey differ.

## Screens

Each node reads the **secondary** screen — the head carrying the SCADA wall
view — and leaves the main screen alone. That is `"captureScreen": "secondary"`
in both configs, which resolves to the first non-primary display.

The one place the main screen is used is the QR flash on the 132 kV node. It
appears centred, borderless and on top, and it takes **no focus**: the SCADA
application underneath keeps the keyboard the whole time.

If a node has only one display attached, "secondary" has nothing to resolve to.
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
**Starting the next session is a manual act**: someone runs the two executables
again.

That is deliberate, and it is why neither node schedules itself for tomorrow.

By default a node that reaches 07:00 stays in memory but idle, so the QR hotkey
still works at exactly the moment the day is read. Set `exitWhenSessionEnds` to
`true` in either config if you would rather the process quit.

## Only the marked boxes are read

Each node carries an ROI file listing the boxes it reads. Nothing else on the
screen is touched — these are the red-outlined boxes on the two wall views.

**132 kV wall view** — `SubstationOcrServer/Config/roi-132kv.json`

| ROI | SCADA label | Rows | → columns |
|---|---|---|---|
| `YARMJA` | OHL-1 OLD YARMJA | KV, A, MW, MVAR | 1–4 |
| `QAYARA` | OHL-2 QAYARA | KV, A, MW, MVAR | 5–8 |

**33 kV wall view** — `SubstationOcrClient/Config/roi-33kv.json`

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
column map in `server.config.json` binds each of the 24 columns to one channel
on one node:

```json
{ "col": 11, "source": "S33", "channel": "T1.MW", "label": "T1 MW", "busClass": "33" }
```

Only the 132 kV node needs this map — it is the one that builds the QR payload.

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

## The link

The client pushes; the server never calls out. Each push is a `push_batch`
carrying every hour the client has not had acknowledged, so one round trip
drains a whole backlog.

- Every message is signed with HMAC-SHA256 under a shared secret and carries a
  timestamp; anything more than two minutes old is rejected.
- `allowedClients` on the server restricts which hosts may connect at all.
- The server refuses any frame whose `nodeId` matches its own, so a
  misconfigured client cannot overwrite the 132 kV values.

**Values cross the wire, never screen captures.** An hour costs about two
kilobytes.

If the link is down the client keeps reading and storing on its own disk and
retries every `retrySeconds` (default 120). A link that returns at 03:00 still
delivers the whole night.

## The QR handoff

Hold **Ctrl** and **Shift**, then press **7**, **8**, **9**. The 132 kV node
builds the payload from everything gathered so far and flashes it on the main
screen for `qrSeconds` (3–5, default 5), then hides it. A click or any key
dismisses it early.

Windows can only register modifiers plus *one* key as a hotkey, so this uses a
low-level keyboard hook instead, tracking which keys are down. It accepts the
combination two ways, because three simultaneous digits ghost on many keyboards:

- all three digits held together while Ctrl+Shift are held, **or**
- 7 then 8 then 9 in order while Ctrl+Shift stay held.

The hook never swallows the keystroke — on a live SCADA desktop, a hook that
eats input would be far worse than a missed QR code.

Payload format is in [QR-ANDROID.md](QR-ANDROID.md).

## Files

```
Shared/Protocol/     wire types, compiled into both nodes
  Json.cs            JSON via System.Web.Extensions — no NuGet needed
  AgentProtocol.cs   framing, HMAC signing, command names
  ReadingFrame.cs    what a node reports for one hour
  RoiDefinition.cs   the ROI file model
  ColumnMap.cs       channel → column bindings
  NumberFormat.cs    OCR text → number
Shared/Capture/      capture and OCR, compiled into both nodes
  NodeConfig.cs      the settings both nodes share
  SessionClock.cs    the 07:00 → 07:00 window and hour ordering
  ScreenGrabber.cs   secondary-screen resolution, capture, crop, upscale
  RoiOcrEngine.cs    row banding, binarisation, Tesseract
  CaptureService.cs  one hourly reading, calibration overlay
  HourStore.cs       on-disk store, keyed by hour AND node
  HourlyScheduler.cs the :02 trigger, bounded by the run window
  NodeLog.cs         day-stamped log file
  HiddenHost.cs      windowless host, optional tray icon
Shared/Qr/
  QrEncoder.cs       self-contained QR encoder
  QrPayload.cs       the payload the phone scans

SubstationOcrServer/   132 kV
  Program.cs           start-up and QR assembly
  ServerConfig.cs      server.config.json
  ReadingServer.cs     TCP listener, accepts pushes
  MergedStore.cs       both nodes' hours → 24-column rows
  HotkeyListener.cs    Ctrl+Shift+7+8+9
  QrFlashWindow.cs     the only thing ever shown on the main screen

SubstationOcrClient/   33 kV
  Program.cs           start-up
  ClientConfig.cs      client.config.json
  ReadingUplink.cs     push with queue and retry
```

`WinLogosheet/` is the original operator application. It is **not part of this
pipeline** — no printing, no Excel export, no grid. It is left in the repository
untouched, and the solution still builds it, but nothing here depends on it.
