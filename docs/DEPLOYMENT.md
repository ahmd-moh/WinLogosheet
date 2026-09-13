# Commissioning guide

## 1. Build

Open `WinLogosheet.sln`. Build **Release**. One program comes out, and the same
build goes on **both** PCs:

| Output | Goes on | With config |
|---|---|---|
| `SubstationOcr\bin\Release\` | the **132 kV** PC | `Config\node-132kv.config.json` |
| `SubstationOcr\bin\Release\` | the **33 kV** PC | `Config\node-33kv.config.json` |

It targets **.NET Framework 4.7**, because that is what the substation PCs run.
An exe built for 4.8 does not start on them at all: Windows asks for 4.8 before
a line of the node runs, so not even its log gets written.

Tesseract 5.2.0 is the only package that ships. The 4.7 reference assemblies come
from `Microsoft.NETFramework.ReferenceAssemblies.net47`, a build-time package, so
the build works without the 4.7 targeting pack. If Visual Studio offers to
retarget the project to 4.8 when the solution opens, decline — or install
**.NET Framework 4.7 targeting pack** from the Visual Studio Installer
(*Individual components*) and it stops asking. If a retarget slips through, the
build stops with an error saying what to set back: `TargetFrameworkVersion` in
`SubstationOcr.csproj` and the `sku` in `App.config` must both say 4.7, and
`requireReinstallation="true"` can be removed from `packages.config` again.
JSON goes through
`System.Web.Extensions` (part of the .NET Framework), and the QR encoder is
written out in full. Restore packages once and build.

### What both PCs need

| Needs | Check | Without it |
|---|---|---|
| .NET Framework 4.7 or later | `reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release` — 460798 or higher | Windows says a newer .NET Framework is required and the node never starts; `Logs\` stays empty |
| Visual C++ 2015-2022 Redistributable (x64), 14.33 or later | *Apps & features* | the start-up lines say `OCR cannot start` and name the missing file; every reading fails |
| Tesseract language data | `eng.traineddata` in `tessDataPath` | the log says `No Tesseract language data found`; a `tessdata` folder beside the exe also works |

Each node writes what it found as its second log line — `Running on .NET
Framework 4.7.2 (release 461814), … x64 process, Visual C++ runtime 14.40…` — so
one look at a node's log settles which of these that PC has.

**Nothing on the network.** The two PCs never connect to each other, so there is
no port to open, no firewall rule, no shared secret and no shared folder. The
program runs from a standard account; only installing the Visual C++ runtime
needs an administrator, on a PC that lacks it.

The `WinLogosheet/` folder is the old operator application. It is not part of
this pipeline or of the solution.

## 2. Install on each PC

Do the same on both PCs:

1. Copy the whole `SubstationOcr\bin\Release` folder to e.g. `C:\SubstationOcr\`
   — any folder the SCADA account can write to. Not under *Program Files*,
   which needs an administrator.
2. In that folder, copy the config for **this** PC out of `Config\` and rename
   the copy `node.config.json`:
   - 132 kV PC: `Config\node-132kv.config.json`
   - 33 kV PC: `Config\node-33kv.config.json`

   Started without `node.config.json`, the program shows a message saying
   exactly this and exits.
3. Edit `node.config.json`:
   - `tessDataPath` — where `eng.traineddata` is on this PC.
   - `captureScreen` — leave `"secondary"`. Use `"primary"` or a screen index
     only if the wall view is not on the second head.
   - `qrSeconds` — 3 to 5.
   - `qrScreen` — `"primary"` (the operator's own screen, the default),
     `"secondary"`, or a screen index.
   - `showTrayIcon` — `true` while commissioning; see step 3.
   - `runAtLogon` — `true` to have the node start when the SCADA account logs
     on. See [below](#surviving-a-reboot-runatlogon).
   - Leave `nodeId`, `roiConfigPath` and `columns` as shipped. They are what
     make this copy the 132 kV or the 33 kV one.

## 3. Check the boxes on each PC

The shipped rectangles were measured on a 1885 x 941 reference capture of each
wall view. If a PC runs at a different resolution the rectangles are scaled
proportionally, but check before trusting a whole night. The node captures in
physical pixels, so Windows display scaling (125%, 150%) does not change what
it reads.

Each PC checks its own wall view, so do this on both. With `"showTrayIcon":
true` in `node.config.json`, start the node. From its tray icon:

1. **Check this PC's boxes (S132)** — or **(S33)** — reads this screen, runs the
   OCR and opens the capture with every box drawn on it. The shot is also kept
   under `Calibration\`.
2. **Open calibration folder** — every shot this PC has taken, as PNGs.

In the window: **green** means every row in that box was read, **amber** some,
**red** none, **grey** switched off. The value each row produced is printed
beside its box. Wheel or `+`/`-` zooms, dragging pans, double-click or `1` shows
it at 100%, `F` fits it again, `Esc` closes it.

So, box by box:

1. Every box should sit on the black value panel, tight but not clipping digits.
2. Every enabled box should be green, with the numbers beside it matching what
   the wall view shows.
3. If one is off, edit its `rect` in this PC's ROI file (`Config\roi-132kv.json`
   or `Config\roi-33kv.json`) — the rectangles the picture is drawn from are in
   real screen pixels — then **Reload ROI configuration**. No restart needed.
4. Check the boxes again and confirm.

Repeat until every value reads correctly. Then set `showTrayIcon` back to
`false` if the node should run invisible — but keep it `true` on a PC where the
old WinLogosheet still runs, because it holds the QR hotkey and the tray is then
the only way to show the code.

### From the command line

`SubstationOcr.exe --calibrate` writes one overlay under `Calibration\` and
quits. `SubstationOcr.exe --capture-once` reads the current hour once, stores
it, and quits.

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

## 4. Daily operation

**At 07:00 each morning, start the program on both PCs by hand.** Order does
not matter — they never wait for each other.

That is the whole daily routine. From then on:

- Each PC reads its secondary screen at :02 past every hour and keeps the hour
  on its own disk.
- Neither shows anything unasked. No window, no taskbar entry.
- At **07:00 the next morning** both stop reading and go quiet. They do not
  start the next session on their own.

### Reading the day

1. On the **132 kV PC**, hold **Ctrl+Shift** and press **7**, then **8**, then
   **9** — each press within three seconds of the last. The QR code fills the
   main screen for 3–5 seconds, then hides. Scan it with the Android app.
2. Do the same on the **33 kV PC**, and scan its code too.

Either PC can go first. The caption above each code names the node (`S132` or
`S33`), the session date and how many hours the code carries. The app merges a
scan into the sheet already on screen when both have the same date, so after
both scans the sheet has all 24 columns; until then the other PC's columns are
empty.

The same sequence while the code is up takes it down. So do **Esc** and a click.
Both the top-row and numpad 7/8/9 work, and the tray menu's **Show QR code
now** does the same.

You can do this at any point during the session, not only at the end — each
code carries everything that PC has gathered so far. Scanning a PC again later
just brings its newer hours in.

Each PC's code is a **single** code: the payload is the `LS1` format the
companion app already parses, and that app has no notion of multi-part codes. If
a session ever will not fit at error correction M the node drops to L
automatically.

### Surviving a reboot: `runAtLogon`

Set `"runAtLogon": true` in `node.config.json` and the node registers itself
under the per-user `Run` key, so it comes back when the SCADA account logs on.
No administrator rights are needed, and the node lands in the interactive
session — which it must, because a background session can neither capture the
screen nor own a global hotkey.

The setting is authoritative in both directions: setting it back to `false` and
starting the node once **removes** the entry. The registered command is
re-checked every start, so a rebuilt or moved executable heals itself, and a
node launched with `--config <path>` registers that same argument.

**This is not the daily restart.** It covers a reboot or a logoff. A node that
reaches 07:00 goes quiet and stays quiet, and on a machine that is never logged
out the entry never fires again — starting tomorrow's session is still a manual
act, as specified.

If you want the session started for you as well, use a Task Scheduler task on
each PC instead: trigger **Daily at 07:00**, **Run only when user is logged
on**, action the executable.

Do **not** register the node as a Windows service — a service cannot capture
the screen.

## 5. Troubleshooting

Everything goes to `Logs\node-YYYY-MM-DD.log` on each PC.

| Symptom | Where to look |
|---|---|
| Message box: `Could not read …\node.config.json` | the message says why: the file is missing (copy this PC's config out of `Config\`, step 2), `nodeId` or `roiConfigPath` is empty, or the JSON is broken |
| `Logs\` stays empty after starting the node | the node never started. Usually the .NET Framework is older than 4.7 — see [What both PCs need](#what-both-pcs-need). A second copy already running also exits without a word: check Task Manager |
| Log: `OCR cannot start` | the line names the cause — most often the Visual C++ runtime is missing or too old |
| Node did not come back after a reboot | `runAtLogon` must be `true` **and** the node must have been started once since; the log records `Autostart registered`. It only fires on logon, not on a machine left logged in |
| Nothing gathered at all | the log's start-up line names the screen being read; if it says "no secondary screen is attached", `captureScreen` needs changing |
| Log: `No column in node.config.json has "source": …` | the `columns` block does not belong to this `nodeId` — use the shipped config for this PC |
| Values read but wrong | ROI boxes are off — go back to step 3 |
| The code shows the other PC's columns empty | expected: each PC fills only its own. Scan the other PC's code as well |
| On the phone, the second scan wiped the first | the app still replaces its sheet on every scan and needs the merge change — see [QR-ANDROID.md](QR-ANDROID.md#two-codes-one-sheet) |
| Ctrl+Shift+7,8,9 does nothing | the log records every press as `Hotkey Ctrl+Shift+N stage X to Y`. No line at all means the registration failed — look for `QR hotkey is NOT armed`: another program, usually the old WinLogosheet, owns that combination. Close it and restart the node, or use **Show QR code now** from the tray. A line that resets to stage 0 means the presses were out of order or more than three seconds apart |
| QR says `NO READINGS TO ENCODE` | the line under it says why: no hour read yet, every read failed, or reads worked but recognised nothing |

The hotkey needs the node running. After 07:00 it is idle but still listening,
so the code can still be shown — unless `exitWhenSessionEnds` is set.

**The Android app needs two changes**: merging two scans of the same date, and
one line for the 07:00 day, or the 07:00 row renders at the bottom of its grid.
See [QR-ANDROID.md](QR-ANDROID.md).

## 6. Housekeeping

Each PC keeps `retentionDays` (default 120) of readings under `Data\` and the
same span of logs under `Logs\`, pruning both on the first reading of each
session. Readings are small — a full session is well under a megabyte.

`keepFullScreenshots: true` also keeps the raw screen captures. Useful while
commissioning; turn it off afterwards.

`Calibration\` is **not** pruned: a shot is only ever written because somebody
asked for one, and they are worth keeping as the record of how a node was set
up. Delete the folder by hand when it stops being interesting.
