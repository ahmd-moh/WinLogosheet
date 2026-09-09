# WinLogosheet V2 — commissioning guide

Follow this once per substation. Steps 1–3 are done at the desk; steps 4 onward
are on site.

---

## 1. Build

Open `WinLogosheet.sln` in Visual Studio 2019 or later. The solution now holds
two projects:

| Project | Output | Installed on |
|---|---|---|
| `SubstationOcrAgent` | `SubstationOcrAgent.exe` | **both** SCADA servers |
| `WinLogosheet` | `WinLogosheet.exe` | the operator station (one of the two servers, or a third PC) |

1. Restore NuGet packages. No new package was added — the agent reuses
   Tesseract 5.2.0, and JSON goes through `System.Web.Extensions`, which is part
   of the .NET Framework. The QR encoder is written out in full in
   `WinLogosheet/V2/QrEncoder.cs`, so there is nothing to fetch for it either.
2. Build **Release**.

> `SubstationOcrAgent` needs `AllowUnsafeBlocks` (already set in its `.csproj`)
> for the pixel loops that binarise each box.

---

## 2. Decide the two server identities

| Server | `serverId` | Reads |
|---|---|---|
| the one showing the 132 kV wall view | `S132` | OHL-1 Yarmja, OHL-2 Qayara |
| the one showing the 33 kV wall view | `S33` | T1/T2/T3 incomers, 4 cable feeders, 3 busbar voltages |

These strings appear in three places and must match exactly: the agent's
`agent.config.json`, the ROI file's `serverId`, and the `source` of every column
binding in `winlogosheet.v2.json`.

---

## 3. Choose a shared secret

Any long random string. The same value goes in all three files. Without it the
agents accept unsigned requests from anything on the LAN, and they log a warning
at start-up saying so.

---

## 4. Install the agent on the 132 kV server

1. Copy the `SubstationOcrAgent` release output to, for example,
   `C:\SubstationOcrAgent\`.
2. Copy `Config\agent.config.132kv.json` to `agent.config.json` beside the exe.
3. Edit it:
   - `sharedSecret` — the string from step 3.
   - `tessDataPath` — where Tesseract is installed on this server.
   - `monitorIndex` — `0` for the primary display; `1` if the wall view lives on
     the second head.
   - `allowedClients` — leave `[]` until the link is proven, then set it to the
     WinLogosheet station's IP.
4. Confirm `Config\roi-132kv.json` sits next to the exe (the `.csproj` copies it).
5. Open **TCP 5115 inbound** in Windows Firewall for this program.
6. Run `SubstationOcrAgent.exe`. A tray icon appears.

Repeat on the 33 kV server using `agent.config.33kv.json` and `roi-33kv.json`.

### Make it start with the server

The agent must run in the **logged-in console session** — Windows will not let a
service capture the screen. So use one of:

- a shortcut in `shell:startup` for the operator account, or
- a Task Scheduler task with trigger *At log on*, *Run only when user is logged
  on*, action `C:\SubstationOcrAgent\SubstationOcrAgent.exe`.

Do **not** register it as a Windows service.

---

## 5. Calibrate the ROI boxes on each server

The shipped rectangles were measured on a 1885 × 941 reference capture of each
wall view. If your server runs at a different resolution the agent scales them
proportionally, but the SCADA layout itself may also differ. Check before
trusting the first day:

1. Tray icon → **Write calibration overlay**. The agent saves a screenshot with
   every ROI outlined and named under `Calibration\`, and offers to open it.
2. Every red box should sit on the black value panel, tight but not clipping the
   digits. A little slack above and below is fine; the row split is proportional.
3. If a box is off, edit its `rect` in the ROI file, then tray icon → **Reload
   ROI configuration**. No restart needed.
4. Tray icon → **Read this hour now**, then **Show last reading…** and compare
   every value against the screen.

Repeat until all values read correctly at confidence well above 55 %.

### Adjusting a box

```json
{
  "id": "T1",
  "label": "TRANSFORMER INCOMER-1",
  "channel": "T1",
  "enabled": true,
  "rect": { "x": 369, "y": 165, "w": 56, "h": 31 },
  "rows": ["A", "MW", "MVAR"],
  "scale": 6,
  "invert": true,
  "colorChannel": "green",
  "minConfidence": 55.0,
  "rowPadding": 1
}
```

| Field | When to change it |
|---|---|
| `rect` | the box moved, or the display resolution changed |
| `rows` | the panel gained or lost a value row — the count must match exactly |
| `scale` | digits are small and misread; raise to 6–8 (costs OCR time, nothing else) |
| `rowPadding` | the separator line between cells is bleeding into a row; raise to 2 |
| `colorChannel` | `"gray"` if the panel text is not green |
| `invert` | `false` if the panel is dark text on a light background |
| `enabled` | `false` to stop reading a box entirely |

---

## 6. Configure WinLogosheet

Edit `winlogosheet.v2.json` beside `WinLogosheet.exe`:

```json
"sharedSecret": "the same string as the agents",
"agents": [
  { "id": "S132", "host": "192.168.0.1", "port": 5115, "label": "132 kV server", "enabled": true },
  { "id": "S33",  "host": "192.168.0.2", "port": 5115, "label": "33 kV server",  "enabled": true }
]
```

Use `127.0.0.1` for whichever server WinLogosheet itself runs on.

Other settings worth knowing:

| Setting | Default | Meaning |
|---|---|---|
| `collectMinute` | `3` | minute past the hour to pull the agents; keep it after their `captureMinute` |
| `autoCollect` | `true` | set `false` to only ever collect on the button |
| `preserveManualEdits` | `true` | a value already on the sheet is never overwritten |
| `lowConfidenceThreshold` | `60` | below this a value is filled in but flagged for checking |
| `timeoutMs` | `15000` | per-agent socket timeout |
| `substationCode` | `MSL-E` | stamped into the QR payload |
| `enabled` | `true` | set `false` to run V1 behaviour with no V2 strip at all |

---

## 7. Prove the link

Start WinLogosheet. A **V2** strip appears at the bottom right of the window:

| Button | What it does |
|---|---|
| **Collect hour** | asks both agents for the selected hour, right now |
| **Backfill day** | pulls every hour both agents hold today, filling only gaps |
| **Agents…** | connection state and where every column's value came from |
| **QR → Mobile** | the day as a QR code for the Android app |

1. **Agents… → Test agents.** Both rows should turn green and name the display,
   the agent version, the machine and the ROI count.
2. **Collect hour.** Fill the current hour and check the column list: every row
   should read *ok*, with a source and a confidence.
3. Compare the 24 values against the two screens once, by eye. This is the only
   full manual check the system needs.

### If a test fails

| Symptom | Cause |
|---|---|
| "No answer from host:5115 within 15 s" | firewall, wrong IP, or the agent is not running |
| "signature mismatch (shared secret differs)" | the secrets do not match |
| "timestamp outside the accepted window" | the two servers' clocks differ by more than 2 minutes |
| "Refused connection … not in allowedClients" | add the WinLogosheet station's IP to that agent's `allowedClients` |
| agent answers, all columns "not read" | ROI boxes are off — go back to step 5 |
| some columns "agent unreachable" | that agent is down; the other server's columns still filled |

---

## 8. Daily operation

The operator's day is unchanged apart from where the numbers come from:

1. **08:00** — open WinLogosheet, press **New** to start the day's sheet.
2. Every hour, at `:03`, the row fills itself. Nothing to press.
3. Check the **V2** indicator: green means everything read cleanly, amber means
   at least one value wants checking, red means no agent answered. Click
   **Agents…** to see which.
4. Correct anything wrong by typing over it. A typed value is never overwritten
   by a later collection.
5. If the link was down for a while, press **Backfill day** — the agents kept
   reading and stored every hour on their own disks.
6. **07:00** the next morning, the Print button unlocks as before, after the
   same validation pass.
7. **QR → Mobile** hands the day to the Android app.

Excel export, printing, section visibility and the "blank on print" hour
toggles all behave exactly as they did in V1.

---

## 9. Housekeeping

Each agent keeps `retentionDays` (default 120) of stored readings under `Data\`
and the same span of logs under `Logs\`, and prunes both on the first capture
of each workday. Readings are small — a full day is well under a megabyte.

`keepFullScreenshots: true` makes an agent also keep the raw screen capture.
Useful during commissioning; turn it off afterwards, it is by far the largest
thing the agent writes.
