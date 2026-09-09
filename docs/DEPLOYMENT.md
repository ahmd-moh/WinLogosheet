# Commissioning guide

## 1. Build

Open `WinLogosheet.sln`. Build **Release**. Two executables come out:

| Project | Output | Goes on |
|---|---|---|
| `SubstationOcrServer` | `SubstationOcrServer.exe` | the **132 kV** server |
| `SubstationOcrClient` | `SubstationOcrClient.exe` | the **33 kV** server |

No new NuGet package was added — both reuse Tesseract 5.2.0, JSON goes through
`System.Web.Extensions` (part of the .NET Framework), and the QR encoder is
written out in full. Restore packages once and build.

The third project, `WinLogosheet`, is the old operator application. It is not
part of this pipeline; you can ignore it or unload it.

## 2. Pick a shared secret

Any long random string. The same value goes in **both** config files. Without
it the server accepts unsigned pushes from anything on the LAN, and it logs a
warning at start-up saying so.

## 3. Install on the 132 kV server

1. Copy the `SubstationOcrServer` release output to e.g. `C:\SubstationOcr\`.
2. Copy `Config\server.config.json` beside the exe (the build does this).
3. Edit it:
   - `sharedSecret` — from step 2.
   - `tessDataPath` — where Tesseract is installed on this machine.
   - `captureScreen` — leave `"secondary"`. Use `"primary"` or a screen index
     only if the wall view is not on the second head.
   - `allowedClients` — leave `[]` until the link is proven, then set it to
     `["<the 33 kV server's IP>"]`.
   - `qrSeconds` — 3 to 5.
4. Open **TCP 5115 inbound** in Windows Firewall for this program.

## 4. Install on the 33 kV server

1. Copy the `SubstationOcrClient` release output to e.g. `C:\SubstationOcr\`.
2. Copy `Config\client.config.json` beside the exe.
3. Edit it:
   - `sharedSecret` — the **same** string.
   - `serverHost` — the 132 kV server's LAN address.
   - `tessDataPath`, `captureScreen` — as above.

No inbound firewall rule is needed here; the client only connects out.

## 5. Calibrate the boxes on each node

The shipped rectangles were measured on a 1885 × 941 reference capture of each
wall view. If a server runs at a different resolution the rectangles are scaled
proportionally, but check before trusting a whole night.

Temporarily set `"showTrayIcon": true` in the config and start the node. From
the tray icon:

1. **Write calibration overlay** — saves a capture of the secondary screen with
   every ROI outlined and named, under `Calibration\`.
2. Every box should sit on the black value panel, tight but not clipping digits.
3. If one is off, edit its `rect` in the ROI file, then **Reload ROI
   configuration**. No restart needed.
4. **Read this hour now**, then check the day's log under `Logs\` — it records
   how many values were read and the mean confidence.

Repeat until every value reads correctly, then set `showTrayIcon` back to
`false` so the node runs invisible.

### Adjusting a box

| Field | When to change it |
|---|---|
| `rect` | the box moved, or the display resolution changed |
| `rows` | the panel gained or lost a value row — the count must match exactly |
| `scale` | digits misread; raise to 6–8 (costs OCR time, nothing else) |
| `rowPadding` | the separator line between cells bleeds into a row; raise to 2 |
| `colorChannel` | `"gray"` if the panel text is not green |
| `invert` | `false` if the panel is dark text on a light background |
| `enabled` | `false` to stop reading a box entirely |

## 6. Daily operation

**At 07:00 each morning, start both executables by hand.** Order does not
matter — the client queues anything it cannot deliver yet.

That is the whole daily routine. From then on:

- Each node reads its secondary screen at :02 past every hour.
- The client pushes its values to the 132 kV node; a failed push is retried
  every two minutes until it lands.
- Neither node shows anything. No window, no taskbar entry, no tray icon.
- At **07:00 the next morning** both nodes stop reading and go quiet. They do
  not start the next session on their own.

### Reading the day

On the **132 kV server**, hold **Ctrl** and **Shift**, then press **7**, **8**,
**9**. The QR code appears on the main screen for 3–5 seconds, then hides. Scan
it with the Android app. Click it or press any key to dismiss it early.

You can do this at any point during the session, not only at the end — the code
carries everything gathered so far, and the caption under it says how many hours
that is.

If the day needs more than one code, they are shown one after another, each for
the same few seconds, labelled "part 1 of 2" and so on. Scan them in one pass.

### Starting a session automatically

If you would rather not start them by hand, use a Task Scheduler task on each
server: trigger **Daily at 07:00**, **Run only when user is logged on**, action
the executable.

Do **not** register either node as a Windows service — a service cannot capture
the screen.

## 7. Troubleshooting

Everything goes to `Logs\node-YYYY-MM-DD.log` on each node.

| Symptom | Where to look |
|---|---|
| Nothing gathered at all | the log's start-up line names the screen being read; if it says "no secondary screen is attached", `captureScreen` needs changing |
| Client log: "No answer from host:5115" | firewall, wrong `serverHost`, or the server node is not running |
| Client log: "signature mismatch" | the two `sharedSecret` values differ |
| Client log: "timestamp outside the accepted window" | the servers' clocks differ by more than 2 minutes |
| Server log: "Refused connection … not in allowedClients" | add the 33 kV server's IP |
| Server log: "Discarded a frame … claiming to be this node" | both configs have the same `nodeId` |
| Values read but wrong | ROI boxes are off — go back to step 5 |
| Ctrl+Shift+7+8+9 does nothing | check the log for "Hotkey armed"; if it says "Hotkey not available", another program holds a conflicting hook. Try the sequence form: hold Ctrl+Shift, then press 7, 8, 9 one after another |
| QR says nothing gathered | no hour has been read yet for the current session |

The hotkey needs the 132 kV node running. After 07:00 it is idle but still
listening, so the code can still be shown — unless `exitWhenSessionEnds` is set.

## 8. Housekeeping

Each node keeps `retentionDays` (default 120) of readings under `Data\` and the
same span of logs under `Logs\`, pruning both on the first reading of each
session. Readings are small — a full session is well under a megabyte.

`keepFullScreenshots: true` also keeps the raw screen captures. Useful while
commissioning; turn it off afterwards.
