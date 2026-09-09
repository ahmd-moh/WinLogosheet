# WinLogosheet V2 — architecture

## What changed, and why

V1 assumed one screen. A Windows scheduled task screenshotted a fixed strip
once an hour, Tesseract read every number it could find in that strip, and the
numbers were dropped into the 24 logsheet columns **in the order OCR happened to
return them**.

That stopped being true the moment the readings lived on two separate SCADA
displays, each driven by its own server. It also had a failure mode that cost
the operator time every day: if OCR missed one value or picked up one extra, the
whole row shifted and every column after the mistake was wrong.

V2 addresses both:

| | V1 | V2 |
|---|---|---|
| Displays | one | two, one per server |
| What is captured | a fixed screen strip | only the marked measurement boxes |
| What crosses the network | nothing (single machine) | **numbers, never images** |
| Column assignment | position in the OCR output | an explicit channel → column map |
| Scheduling | Windows Task Scheduler, re-registered daily | resident agent watching the clock |
| Missing hour | blocked navigation | agents store every hour; backfill on demand |
| Mobile handoff | QR of the day | QR of the day, framed, CRC-checked, auto-split |

## The two applications

```
 ┌──────────────────────────────┐        ┌──────────────────────────────┐
 │  132 kV SERVER               │        │  33 kV SERVER                │
 │                              │        │                              │
 │  SCADA wall view (132 kV)    │        │  SCADA wall view (33 kV)     │
 │            │                 │        │            │                 │
 │            ▼                 │        │            ▼                 │
 │  SubstationOcrAgent.exe      │        │  SubstationOcrAgent.exe      │
 │   serverId "S132"            │        │   serverId "S33"             │
 │   • grabs its own screen     │        │   • grabs its own screen     │
 │     at :02 every hour        │        │     at :02 every hour        │
 │   • crops ONLY the marked    │        │   • crops ONLY the marked    │
 │     boxes (the red squares)  │        │     boxes (the red squares)  │
 │   • OCRs each box row by row │        │   • OCRs each box row by row │
 │   • stores the numbers       │        │   • stores the numbers       │
 │   • listens on TCP 5115      │        │   • listens on TCP 5115      │
 └──────────────┬───────────────┘        └──────────────┬───────────────┘
                │                                       │
                │        JSON over TCP — values only    │
                └───────────────┬───────────────────────┘
                                ▼
                  ┌──────────────────────────────┐
                  │  WinLogosheet.exe  (V2)      │
                  │   • pulls both agents at :03 │
                  │   • merges into the 24       │
                  │     logsheet columns         │
                  │   • existing validation,     │
                  │     Excel export, printing   │
                  │   • QR handoff to Android    │
                  └──────────────────────────────┘
```

WinLogosheet runs on **one** of the two servers (or a third operator station).
It always talks to both agents the same way — the local one over `127.0.0.1`,
the remote one over the substation LAN. There is no special case for "my own
server", which is what keeps the configuration symmetric and the failure modes identical.

## Only the marked boxes are read

Each agent carries an ROI file listing the boxes it reads. Nothing else on the
screen is touched. For this substation, that is exactly the red-outlined boxes
on the two wall views:

**132 kV wall view** (`Config/roi-132kv.json`)

| ROI | SCADA label | Rows read | → logsheet columns |
|---|---|---|---|
| `YARMJA` | OHL-1 OLD YARMJA | KV, A, MW, MVAR | 1–4 |
| `QAYARA` | OHL-2 QAYARA | KV, A, MW, MVAR | 5–8 |

**33 kV wall view** (`Config/roi-33kv.json`)

| ROI | SCADA label | Rows read | → logsheet columns |
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

### About the three busbar boxes

The red boxes on the 33 kV view carry **A, MW and MVAR only** — there is no kV
row inside them. The logsheet still has a KV column for T1, T2 and T3, so those
three columns are fed from the busbar voltage boxes that supply each incomer
(BUS 1A → T1, BUS 2A → T2, BUS 2B → T3). They are the only ROIs that are not
red-outlined, and they are marked as such in the ROI file. If your sheet does
not log the 33 kV bus voltage, set `"enabled": false` on those three ROIs and
the columns stay blank.

## Channels and the column map

An agent does not know what a logsheet is. It reports **channels**, named
`<ROI>.<row>` — `T1.MW`, `YARMJA.KV`, `FDR_SALAM1.MW`. WinLogosheet's
`winlogosheet.v2.json` binds each of the 24 columns to one channel on one agent:

```json
{ "col": 11, "source": "S33", "channel": "T1.MW", "label": "T1 MW", "busClass": "33" }
```

This indirection is the whole point. Re-arranging the SCADA screen means editing
one rectangle in one ROI file. Re-arranging the *logsheet* means editing one
column binding. Neither can silently shift the other's values, which is the
class of error V1 could not detect.

## How a value is read

1. Grab the configured monitor.
2. Crop the ROI rectangle. If the server's resolution differs from the
   resolution the ROIs were calibrated at, the rectangle is scaled
   proportionally first.
3. Split the crop into its declared rows — a three-row box becomes three bands.
4. For each band: upscale (6× for the small 33 kV boxes), take the **green
   channel** (the panel text is green phosphor on black, and green separates it
   from the grey chrome far better than luminance), Otsu-binarise, invert to
   dark-on-light, add a quiet margin.
5. OCR that single line with page segmentation mode 7.
6. Normalise the text to a number, correcting the glyph confusions this
   typeface produces (`O`→`0`, `l`→`1`, `S`→`5`, `,`→`.`, and the several
   Unicode dashes Tesseract emits for a minus sign), then take the longest
   numeric run so a border artefact ahead of the value cannot win.

If a row comes back empty, the whole box is re-read as one block and the gaps
are filled positionally — but **only if the block pass found exactly as many
numbers as the box declares rows**. Anything else and we would be guessing which
number belongs to which row, which is precisely the V1 failure this design
exists to remove.

## Timing

| Time | What happens |
|---|---|
| `:02` | each agent reads its display and stores the hour |
| `:03` | WinLogosheet pulls both agents and fills the row |

The agent watches the clock itself rather than relying on a scheduled task, so
a reading missed because the machine was busy or asleep is taken as soon as the
agent is running again, still filed under the hour it belongs to.

Both minutes are configurable (`captureMinute`, `collectMinute`). Keep the
collector at least one minute behind the agents.

## Failure behaviour

- **One server down.** The other server's columns are filled normally. The
  missing columns are marked "agent unreachable" in the collector window, and
  the operator types them or waits for the link to return.
- **Link down for hours.** Each agent keeps reading and storing on its own disk.
  **Backfill day** pulls everything both agents hold for the session date and
  fills only the gaps.
- **A value already on the sheet** is never overwritten by a collection while
  `preserveManualEdits` is on. An operator correction stands.
- **Low confidence or an implausible value** is still filled in, but flagged
  in the collector window so it gets checked rather than silently trusted.

## Security on the link

Every message is signed with HMAC-SHA256 under a shared secret and carries a
timestamp; the receiver rejects anything more than two minutes old, so a
captured frame cannot be replayed. `allowedClients` restricts which hosts an
agent will talk to at all. The secret is not authentication of a *person* — it
keeps unconfigured software on the substation LAN from injecting readings.

## Images stay on their own server

The hourly path carries numbers only. Two operator-initiated exceptions exist,
and both are deliberate:

- **Write calibration overlay** makes an agent save a screenshot with its ROI
  boxes drawn on it — *on that agent's own disk*. Only the file path comes back.
- **Show ROI image** fetches one small ROI crop for troubleshooting. It is
  disabled by default (`allowRoiImages: false`) and never used by the hourly
  collection.

## Files

```
Shared/Protocol/          wire types compiled into BOTH applications
  Json.cs                 JSON via System.Web.Extensions — no NuGet needed
  AgentProtocol.cs        framing, HMAC signing, command names
  ReadingFrame.cs         what an agent reports for one hour
  RoiDefinition.cs        the ROI file model
  ColumnMap.cs            channel → logsheet column bindings
  NumberFormat.cs         OCR text → number, plus range sanity checks

SubstationOcrAgent/       the application installed on each SCADA server
  Program.cs              entry point, single-instance guard
  AgentConfig.cs          agent.config.json
  ScreenGrabber.cs        monitor capture, crop, upscale
  RoiOcrEngine.cs         row banding, binarisation, Tesseract
  CaptureService.cs       one hourly reading, calibration overlay
  HourStore.cs            on-disk hour store + logsheet hour rules
  HourlyScheduler.cs      the :02 trigger
  AgentServer.cs          the TCP server
  TrayHost.cs             tray icon and log window
  Config/                 sample agent configs and the calibrated ROI files

WinLogosheet/V2/          the collector side, inside the existing application
  V2Settings.cs           winlogosheet.v2.json
  RemoteAgentClient.cs    the TCP client
  HourCollector.cs        merge both agents into one logsheet row
  V2StatusForm.cs         agents and per-column provenance
  QrEncoder.cs            self-contained QR encoder
  QrPayload.cs            the payload the Android app scans
  QrForm.cs               the QR window
WinLogosheet/Form1.V2.cs  the V2 strip on the main form
```

The V1 code path is untouched. `Form1.Designer.cs` is not modified — the V2
buttons build themselves at run time.
