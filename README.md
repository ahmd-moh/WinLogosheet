# Substation OCR

One Windows program that reads the hourly values off a substation's SCADA wall
view and hands them to a phone as a QR code. The same program runs on both SCADA
PCs, and each PC shows its own code.

| | 132 kV PC | 33 kV PC |
|---|---|---|
| Program | `SubstationOcr.exe` | `SubstationOcr.exe` |
| Config | `node.config.json`, copied from `Config\node-132kv.config.json` | `node.config.json`, copied from `Config\node-33kv.config.json` |
| Reads | its own **secondary** screen | its own **secondary** screen |
| QR code carries | columns 1–8 (Yarmja, Qayra) | columns 9–24 (T1–T3, four feeders) |
| Shows it on | the **main** screen | the **main** screen |

- [Architecture](docs/ARCHITECTURE.md) — how the program works on each PC
- [Commissioning guide](docs/DEPLOYMENT.md) — installing, calibrating, daily use
- [QR handoff to Android](docs/QR-ANDROID.md) — the payload the phone scans, and
  how it joins the two codes

## How it works

1. **Start the program on both PCs at 07:00.** It runs until 07:00 the next
   morning and then stops. Starting the next session is a manual act.
2. At **:02** past every hour it captures its own **secondary** screen — the
   head showing the wall view — and reads only the marked measurement boxes with
   Tesseract OCR. The values stay on that PC's disk.
3. It runs **fully hidden** — no window, no taskbar entry. Everything goes to a
   day-stamped log file.
4. On either PC, hold **Ctrl+Shift** and press **7**, then **8**, then **9**.
   That PC's part of the session fills the main screen as a QR code for 3–5
   seconds, then hides.
5. Scan **both** codes with the Android companion app, in either order. The
   payload is the same `LS1` format it already parses, and the app merges two
   scans of the same date into one 24-column sheet.

That is the whole system. Nothing is printed, nothing is exported to Excel, and
nothing crosses the network.

## Why the two PCs do not talk to each other

An earlier version had the 33 kV PC push its values to the 132 kV PC over TCP,
so that one code carried the whole sheet. On site that cannot work: both PCs run
local accounts without administrator rights, Windows Firewall blocks incoming
connections (ping answers, TCP 5115 does not), nobody can add a firewall rule,
and no shared folder is available. So each PC stands alone and the phone does
the joining. The networked version is kept on the `V2-client-server` branch.

## Only the marked boxes are read

Each PC's ROI file names the boxes it reads — the red-outlined measurement
panels on its wall view, plus the three 33 kV busbar voltage boxes that supply
the incomer KV columns. Nothing else on the screen is touched, and each box
declares its own rows, so a value that fails to read leaves an empty cell
instead of shifting every column after it.

## Requirements

- Windows 10/11 on both SCADA PCs, each with the wall view on a second display
- .NET Framework 4.7 or later — the program is built for 4.7, which is what the
  substation PCs run
- Microsoft Visual C++ 2015-2022 Redistributable (x64), 14.33 or later — the
  native half of Tesseract is linked against it
- Tesseract's English language data (`eng.traineddata`) on both PCs, at the
  `tessDataPath` in each config or in a `tessdata` folder beside the exe
- No network link and no firewall rule. The program itself needs no
  administrator rights; only installing the Visual C++ runtime does, on a PC
  that lacks it

## Building

Open `WinLogosheet.sln`, restore packages, build **Release**. Copy
`SubstationOcr\bin\Release` to both PCs and give each its own
`node.config.json`. See the [commissioning guide](docs/DEPLOYMENT.md).

## WinLogosheet

`WinLogosheet/` is the original operator application — the hourly grid,
validation, Excel export and printing. It is **not part of this pipeline** and
nothing here depends on it. It is left in the repository, outside the solution,
in case it is wanted again.

## License

See [LICENSE.txt](LICENSE.txt).
