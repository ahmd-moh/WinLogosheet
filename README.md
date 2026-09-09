# Substation OCR

Two Windows applications that gather the hourly readings from a substation's two
SCADA wall views and hand them to a phone as a QR code.

| | 33 kV server | 132 kV server |
|---|---|---|
| Application | `SubstationOcrClient.exe` | `SubstationOcrServer.exe` |
| Socket role | client — pushes its values | server — listens on TCP 5115 |
| Reads | its own **secondary** screen | its own **secondary** screen |
| Shows | nothing | the QR code, on the **main** screen |

- [Architecture](docs/ARCHITECTURE.md) — how the two applications fit together
- [Commissioning guide](docs/DEPLOYMENT.md) — installing, calibrating, daily use
- [Socket protocol](docs/PROTOCOL.md) — the wire format
- [QR handoff to Android](docs/QR-ANDROID.md) — the payload the phone scans

## How it works

1. **Start both applications at 07:00.** They run until 07:00 the next morning
   and then stop. Starting the next session is a manual act.
2. At **:02** past every hour each application captures its own **secondary**
   screen — the head showing the wall view — and reads only the marked
   measurement boxes with Tesseract OCR.
3. The 33 kV client pushes its values to the 132 kV server over TCP. Values
   cross the wire, never screen captures. A failed push is queued and retried
   every two minutes, so a link that comes back at 03:00 still delivers the
   whole night.
4. Both applications run **fully hidden** — no window, no taskbar entry, no tray
   icon. Everything goes to a day-stamped log file.
5. On the 132 kV server, hold **Ctrl+Shift** and press **7**, **8**, **9**. The
   gathered session appears as a QR code on the main screen for 3–5 seconds,
   then hides. Scan it with the Android app.

That is the whole system. Nothing is printed and nothing is exported to Excel.

## Only the marked boxes are read

Each application carries an ROI file naming the boxes it reads — the
red-outlined measurement panels on its wall view, plus the three 33 kV busbar
voltage boxes that supply the incomer KV columns. Nothing else on the screen is
touched, and each box declares its own rows, so a value that fails to read
leaves an empty cell instead of shifting every column after it.

## Requirements

- Windows 10/11 on both SCADA servers, each with the wall view on a second
  display
- .NET Framework 4.8
- [Tesseract OCR](https://github.com/tesseract-ocr/tesseract) installed on both,
  with the `tessdata` path set in each config
- TCP 5115 open inbound on the 132 kV server

## Building

Open `WinLogosheet.sln`, restore packages, build **Release**. Deploy
`SubstationOcrServer` to the 132 kV machine and `SubstationOcrClient` to the
33 kV machine. See the [commissioning guide](docs/DEPLOYMENT.md).

## WinLogosheet

`WinLogosheet/` is the original operator application — the hourly grid,
validation, Excel export and printing. It is **not part of this pipeline** and
nothing here depends on it. It is left in the repository, and the solution still
builds it, in case it is wanted again.

## License

See [LICENSE.txt](LICENSE.txt).
