# Commissioning guide

## 1. Build

Open `WinLogosheet.sln`. Build **Release**. Two executables come out:

| Project | Output | Goes on |
|---|---|---|
| `SubstationOcrServer` | `SubstationOcrServer.exe` | the **132 kV** server |
| `SubstationOcrClient` | `SubstationOcrClient.exe` | the **33 kV** server |

Both target **.NET Framework 4.7**, because that is what the substation servers
run. An exe built for 4.8 does not start on them at all: Windows asks for 4.8
before a line of the node runs, so not even its log gets written.

Tesseract 5.2.0 is the only package that ships. The 4.7 reference assemblies come
from `Microsoft.NETFramework.ReferenceAssemblies.net47`, a build-time package, so
the build works without the 4.7 targeting pack. If Visual Studio offers to
retarget the projects to 4.8 when the solution opens, decline — or install
**.NET Framework 4.7 targeting pack** from the Visual Studio Installer
(*Individual components*) and it stops asking. JSON goes through
`System.Web.Extensions` (part of the .NET Framework), and the QR encoder is
written out in full. Restore packages once and build.

### What both servers need

| Needs | Check | Without it |
|---|---|---|
| .NET Framework 4.7 or later | `reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release` — 460798 or higher | Windows says a newer .NET Framework is required and the node never starts; `Logs\` stays empty |
| Visual C++ 2015-2022 Redistributable (x64), 14.33 or later | *Apps & features* | the start-up lines say `OCR cannot start` and name the missing file; every reading fails |
| Tesseract language data | `eng.traineddata` in `tessDataPath` | the log says `No Tesseract language data found`; a `tessdata` folder beside the exe also works |

Each node writes what it found as its second log line — `Running on .NET
Framework 4.7.2 (release 461814), … x64 process, Visual C++ runtime 14.40…` — so
one look at a node's log settles which of these that server has.

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
   - `qrScreen` — `"primary"` (the operator's own screen, the default),
     `"secondary"`, or a screen index.
   - `clientNodeId` — the 33 kV node's `nodeId`, `"S33"` unless you renamed it.
     This is the node this one asks for a calibration.
   - `calibrationMaxWidth` — how wide a calibration picture to ask that node
     for. 1920 is plenty; 0 asks for the screen's full resolution.
   - `runAtLogon` — `true` to have this node start when the SCADA account logs
     on. See [below](#surviving-a-reboot-runatlogon).
4. Open **TCP 5115 inbound** in Windows Firewall for this program.

## 4. Install on the 33 kV server

1. Copy the `SubstationOcrClient` release output to e.g. `C:\SubstationOcr\`.
2. Copy `Config\client.config.json` beside the exe (the build does this).
3. Edit it:
   - `sharedSecret` — the **same** string.
   - `serverHost` — the 132 kV server's LAN address.
   - `tessDataPath`, `captureScreen`, `runAtLogon` — as above.
   - `pollSeconds` — how often this node asks the 132 kV node whether it wants
     anything, 20 by default. It is what lets the boxes here be checked from
     that seat; 0 switches it off.

No inbound firewall rule is needed here; the client only connects out.

## 5. Check the boxes on both nodes

The shipped rectangles were measured on a 1885 x 941 reference capture of each
wall view. If a server runs at a different resolution the rectangles are scaled
proportionally, but check before trusting a whole night. The nodes capture in
physical pixels, so Windows display scaling (125%, 150%) does not change what
they read.

**Do this from the 132 kV seat.** Both wall views can be checked from there —
the 33 kV node is asked for its own picture and sends it back — so nobody has to
sit at the other server.

Set `"showTrayIcon": true` in `server.config.json` and start the node. From its
tray icon:

1. **Check this node's boxes (S132)** — reads this screen, runs the OCR and
   opens the capture with every box drawn on it.
2. **Ask S33 for its boxes** — the request waits until that node's next poll
   (about 20 s) and then the same window opens with the 33 kV wall view. The
   33 kV node needs no tray icon and nobody needs to be in front of it.
3. **Open calibration folder** — every shot, both nodes', kept as a PNG.

In the window: **green** means every row in that box was read, **amber** some,
**red** none, **grey** switched off. The value each row produced is printed
beside its box. Wheel or `+`/`-` zooms, dragging pans, double-click or `1` shows
it at 100%, `F` fits it again, `Esc` closes it.

So, box by box:

1. Every box should sit on the black value panel, tight but not clipping digits.
2. Every enabled box should be green, with the numbers beside it matching what
   the wall view shows.
3. If one is off, edit its `rect` in that node's ROI file — the rectangles the
   picture is drawn from are in real screen pixels — then **Reload ROI
   configuration** on that node. No restart needed.
4. Ask for the picture again and confirm.

Repeat until every value reads correctly, then set `showTrayIcon` back to
`false` so the node runs invisible.

### If nothing comes back

The 132 kV node says so 90 seconds after you ask, and which of the two it is:

| It says | Means |
|---|---|
| *has not asked for work since* | that node is not running, its `nodeId` is not `clientNodeId`, or its `pollSeconds` is 0 |
| *took the request but sent nothing back* | it collected the request and its capture or OCR failed — its log says why |

A calibration request is dropped if nobody collects it within ten minutes, so a
click made while the other node was switched off cannot surprise you at 03:00.

### From the node itself

If you are standing at either machine instead, set `showTrayIcon` there and use
**Write calibration overlay** — the same picture, written under `Calibration\`.
The 33 kV node also offers **Send calibration to the 132 kV node**, which pushes
it to the other seat unasked. Either exe also takes `--calibrate` on the command
line: it writes one overlay and quits.

### Adjusting a box

| Field | When to change it |
|---|---|
| `rect` | the box moved, or the display resolution changed |
| `rows` | the panel gained or lost a value row — the count must match exactly |
| `scale` | digits misread; raise to 6-8 (costs OCR time, nothing else) |
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

On the **132 kV server**, hold **Ctrl+Shift** and press **7**, then **8**, then
**9** — each press within three seconds of the last. The QR code fills the main
screen for 3–5 seconds, then hides. Scan it with the Android app.

The same sequence while the code is up takes it down. So do **Esc** and a click.
Both the top-row and numpad 7/8/9 work.

You can do this at any point during the session, not only at the end — the code
carries everything gathered so far, and the caption above it says how many hours
that is.

It is always a **single** code: the payload is the `LS1` format the companion app
already parses, and that app has no notion of multi-part codes. If a session
ever will not fit at error correction M the node drops to L automatically.

### Surviving a reboot: `runAtLogon`

Set `"runAtLogon": true` in either config and that node registers itself under
the per-user `Run` key, so it comes back when the SCADA account logs on. No
administrator rights are needed, and the node lands in the interactive session —
which it must, because a background session can neither capture the screen nor
own a global hotkey.

The setting is authoritative in both directions: setting it back to `false` and
starting the node once **removes** the entry. The registered command is
re-checked every start, so a rebuilt or moved executable heals itself, and a
node launched with `--config <path>` registers that same argument.

**This is not the daily restart.** It covers a reboot or a logoff. A node that
reaches 07:00 goes quiet and stays quiet, and on a machine that is never logged
out the entry never fires again — starting tomorrow's session is still a manual
act, as specified.

If you want the session started for you as well, use a Task Scheduler task on
each server instead: trigger **Daily at 07:00**, **Run only when user is logged
on**, action the executable.

Do **not** register either node as a Windows service — a service cannot capture
the screen.

## 7. Troubleshooting

Everything goes to `Logs\node-YYYY-MM-DD.log` on each node.

| Symptom | Where to look |
|---|---|
| `Logs\` stays empty after starting a node | the node never started. Usually the .NET Framework is older than 4.7 — see [What both servers need](#what-both-servers-need). A second copy already running also exits without a word: check Task Manager |
| Log: `OCR cannot start` | the line names the cause — most often the Visual C++ runtime is missing or too old |
| Node did not come back after a reboot | `runAtLogon` must be `true` **and** the node must have been started once since; the log records `Autostart registered`. It only fires on logon, not on a machine left logged in |
| Nothing gathered at all | the log's start-up line names the screen being read; if it says "no secondary screen is attached", `captureScreen` needs changing |
| Client log: "No answer from host:5115" | firewall, wrong `serverHost`, or the server node is not running |
| Client log: "signature mismatch" | the two `sharedSecret` values differ |
| Client log: "timestamp outside the accepted window" | the servers' clocks differ by more than 2 minutes |
| Server log: "Refused connection … not in allowedClients" | add the 33 kV server's IP |
| Server log: "Discarded a frame … claiming to be this node" | both configs have the same `nodeId` |
| Values read but wrong | ROI boxes are off — go back to step 5 |
| Asked S33 for its boxes and nothing came | see [If nothing comes back](#if-nothing-comes-back) — the node says which of the two reasons it is |
| Ctrl+Shift+7,8,9 does nothing | the log records every press as `Hotkey Ctrl+Shift+N stage X to Y`. No line at all means the registration failed — look for `RegisterHotKey failed`, which means another program owns that combination. A line that resets to stage 0 means the presses were out of order or more than three seconds apart |
| QR says nothing gathered | no hour has been read yet for the current session |

The hotkey needs the 132 kV node running. After 07:00 it is idle but still
listening, so the code can still be shown — unless `exitWhenSessionEnds` is set.

**The Android app needs one line changed** for the 07:00 day, or the 07:00 row
renders at the bottom of its grid. See [QR-ANDROID.md](QR-ANDROID.md#hour-order).

## 8. Housekeeping

Each node keeps `retentionDays` (default 120) of readings under `Data\` and the
same span of logs under `Logs\`, pruning both on the first reading of each
session. Readings are small — a full session is well under a megabyte.

`keepFullScreenshots: true` also keeps the raw screen captures. Useful while
commissioning; turn it off afterwards.

`Calibration\` is **not** pruned: a shot is only ever written because somebody
asked for one, and they are worth keeping as the record of how a node was set
up. Delete the folder by hand when it stops being interesting.
