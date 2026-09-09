# WinLogosheet

A Windows desktop application for automated hourly logsheet management at electrical power substations. It reads SCADA screen values via OCR, validates the data, and exports a completed daily logsheet to Excel or print.

**V2** splits the work across the two SCADA servers. A small agent runs on each,
reads only the marked measurement boxes on its own wall view, and serves the
**numbers** — not screenshots — to WinLogosheet over a socket.

- [Architecture](docs/V2-ARCHITECTURE.md) — how the two applications fit together
- [Commissioning guide](docs/V2-DEPLOYMENT.md) — installing and calibrating on site
- [Socket protocol](docs/V2-PROTOCOL.md) — the wire format
- [QR handoff to Android](docs/V2-QR-ANDROID.md) — the payload the phone scans

## Features

- **Two-server collection (V2)** — an OCR agent on each SCADA server reads its own wall view at minute :02 and serves the values over TCP; WinLogosheet merges both into one logsheet row at :03.
- **Region-of-interest OCR (V2)** — only the marked measurement boxes are read, and each box declares its own rows, so a missed value can no longer shift every column after it.
- **Values on the wire, not images (V2)** — an hour costs about two kilobytes; screenshots never leave the server that took them.
- **Resilient to an outage (V2)** — each agent keeps reading and storing on its own disk; **Backfill day** fills the gaps once the link returns.
- **Automated screen capture (V1)** — registers a Windows Task Scheduler job that screenshots the SCADA display at minute :02 of every hour (08:00–24:00).
- **OCR extraction** — uses Tesseract OCR to read numeric values (KV, A, MW, MVAR).
- **Hourly data grid** — color-coded ListView showing all 24 hourly readings across six substation sections.
- **Real-time validation** — cells are highlighted automatically:
  - Red — missing value in a row that has other data
  - Orange — abnormally high reactive power (|MVAR| > 100)
  - Yellow — suspect reading (voltage out of range, negative current, overload)
- **Manual correction** — select any hour, view its source image, and edit values in the text boxes.
- **Excel export** — fills the official `Exact_Substation_Log_v4.xlsx` template and creates an automatic backup.
- **Print preview** — generates a formatted logsheet with coordinate-calibrated layout, blocked until all validation issues are resolved.
- **Section visibility toggles** — show/hide individual substation groups (Yarmja, Qayra, T1, T2, T3, Feeders).
- **QR handoff to mobile** — the day as a QR code the Android app scans; framed, CRC-checked, and split across several codes when one will not hold it.

## Substation Sections

| Group | Columns | Bus Voltage | Measurements |
|-------|---------|-------------|--------------|
| Yarmja | 1–4 | 132 kV | KV, A, MW, MVAR |
| Qayra | 5–8 | 132 kV | KV, A, MW, MVAR |
| T1 | 9–12 | 33 kV | KV, A, MW, MVAR |
| T2 | 13–16 | 33 kV | KV, A, MW, MVAR |
| T3 | 17–20 | 33 kV | KV, A, MW, MVAR |
| Feeders (Domez, Summer, Salam 1, Salam 2) | 21–24 | — | WM (MW) |

## Requirements

- Windows 10/11 on both SCADA servers
- .NET Framework 4.8
- [Tesseract OCR](https://github.com/tesseract-ocr/tesseract) installed
  - Release build expects tessdata at `C:\Users\DCS_User\AppData\Local\Programs\Tesseract-OCR\tessdata`
  - Debug build expects `C:\Program Files\Tesseract-OCR\tessdata`
- Microsoft Excel (for the Excel print path via Interop)
- The Excel template file `Exact_Substation_Log_v4.xlsx` placed next to the executable

## Getting Started

1. Clone or download the repository.
2. Open `WinLogosheet.sln` in Visual Studio 2019 or later.
3. Restore NuGet packages (EPPlus, Tesseract, TaskScheduler, Microsoft.Office.Interop.Excel).
4. Build the solution (`Release` for production).
5. Copy `Exact_Substation_Log_v4.xlsx` into the output directory alongside the `.exe`.
6. Run the application. On first launch, click **Start Task** to register the hourly screenshot scheduler.

## How It Works (V2)

1. At minute :02 of every hour, the agent on each SCADA server grabs its own
   display, crops **only** the marked measurement boxes, OCRs each box row by
   row, and stores the numbers locally.
2. At minute :03, WinLogosheet asks both agents for that hour over TCP and
   merges the answers into the 24 logsheet columns using the column map.
3. Every column is reported with its source, channel and OCR confidence.
   Anything low-confidence or out of range is flagged rather than silently
   trusted, and a value the operator typed is never overwritten.
4. The operator reviews the grid, corrects anything wrong, and uses **Copy from
   Previous/Next** for unchanged hours.
5. At 07:00 the next morning the **Print** button unlocks and runs the full
   validation pass — if any issues remain it shows a detailed error list (in
   Arabic) and blocks printing.
6. **Export to Excel** writes all rows into the day's sheet inside the monthly
   workbook and saves a backup copy. **QR → Mobile** hands the day to the
   Android app.

See the [commissioning guide](docs/V2-DEPLOYMENT.md) for setup, and the
[architecture notes](docs/V2-ARCHITECTURE.md) for what V1 did differently.

## Dependencies

| Package | Version |
|---------|---------|
| EPPlus | 8.3.1 |
| Tesseract | 5.2.0 |
| TaskScheduler | 2.12.2 |
| Microsoft.Office.Interop.Excel | 16.0 |

## License

See [LICENSE.txt](LICENSE.txt).
