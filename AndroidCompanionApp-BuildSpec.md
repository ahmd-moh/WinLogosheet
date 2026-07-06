# Build Prompt — Android Companion App for "WinLogosheet" (offline QR → grid)

> Copy this entire file into a new chat with Claude (or any AI) inside Android Studio.
> It is a complete, self-contained specification. Build the app exactly as described.

---

You are an expert Android engineer. Build, **from scratch**, a small **offline** Android
app that scans a QR code produced by a Windows desktop app ("WinLogosheet") and displays
the substation hourly logsheet **as a grid, laid out exactly like the PC**.

There is **no network, no backend, no accounts**. The QR is scanned by the camera, the text
is parsed locally, and the grid is drawn on screen. That's the whole app.

## 0. Summary of the one screen

- A title bar.
- A small **"Scan QR"** button.
- When tapped → open the camera → scan the QR → parse the text → render the grid below.
- If nothing scanned yet, show a friendly empty state ("Tap *Scan QR* to load a logsheet").
- The grid is wider than the phone, so it must **scroll horizontally and vertically**.

---

## 1. The QR payload format (THE CONTRACT — must match byte-for-byte)

The desktop encodes the whole logsheet as one plain-text string, kept inside the QR
"alphanumeric" character set on purpose (`0-9 A-Z space $ % * + - . / :`). Format:

```
LS1 <YYYYMMDD>*<row>*<row>*<row>...
```

- **Header**: the literal `LS1`, a single space, then the sheet date as `YYYYMMDD`.
- Rows are separated by `*`.
- Each **row** is: `[-]HH:c1.c2.c3. ... .c24`
  - `HH` = two-digit hour, values `08..24` then `01..07` (see §2 for the order).
  - A leading `-` (before `HH`) means **this hour is marked "skip"** on the PC
    (excluded from the printout). Show it, but you may render it faded / struck-through.
  - After the `:` there are **always exactly 24 cells**, separated by `.`.
  - A cell is an **integer string** (e.g. `132`, `44`) or **empty** (no value).
    Empty cells still occupy their position, so you see adjacent dots:
    `12..7` means `[12] [empty] [7]`.
- Not every hour is always present. The QR contains **1..24 rows** — only the hours the
  operator has data for. Render whatever rows arrive, in the canonical order of §2.

### 1.1 Test vector (use this to develop before you have a real QR)

```
LS1 20260430*08:132.250.80.10.133.205.62.8.33.410.92.12.33.380.85.11.32.300.70.9.45.30.20.15*09:131.240..9.....33.400.90.12.........44..19.14*-10:132.250.80.10.133.205.62.8.33.410.92.12.33.380.85.11.32.300.70.9.45.30.20.15
```

This decodes to: date **2026-04-30**, three rows — hour **08** (full), hour **09**
(several empty cells), hour **10** (marked **skip**). Each row expands to exactly 24 cells.

---

## 2. Data model — columns, groups, hours ("coordinates")

There are **24 data columns** per hour, grouped into 6 sections. The **cell index is
0-based** into the 24 cells of a row (cell 0 = first cell after `HH:`).

| cell idx | Group     | Metric          | Notes                         |
|:-------:|:----------|:----------------|:------------------------------|
| 0       | Yarmja    | KV              | 132 kV bus                    |
| 1       | Yarmja    | A               |                               |
| 2       | Yarmja    | MW              |                               |
| 3       | Yarmja    | MV (=MVAR)      |                               |
| 4       | Qayra     | KV              | 132 kV bus                    |
| 5       | Qayra     | A               |                               |
| 6       | Qayra     | MW              |                               |
| 7       | Qayra     | MV (=MVAR)      |                               |
| 8       | T1        | KV              | 33 kV bus                     |
| 9       | T1        | A               |                               |
| 10      | T1        | MW              |                               |
| 11      | T1        | MV (=MVAR)      |                               |
| 12      | T2        | KV              | 33 kV bus                     |
| 13      | T2        | A               |                               |
| 14      | T2        | MW              |                               |
| 15      | T2        | MV (=MVAR)      |                               |
| 16      | T3        | KV              | 33 kV bus                     |
| 17      | T3        | A               |                               |
| 18      | T3        | MW              |                               |
| 19      | T3        | MV (=MVAR)      |                               |
| 20      | Domez     | WM              | feeder (single value)         |
| 21      | Summer    | WM              | feeder (single value)         |
| 22      | Salam 1   | WM              | feeder (single value)         |
| 23      | Salam 2   | WM              | feeder (single value)         |

**Header layout (two rows):** the 5 transformer groups (Yarmja, Qayra, T1, T2, T3) each
span **4 sub-columns** (KV / A / MW / MV). The 4 feeders (Domez, Summer, Salam 1, Salam 2)
are **1 column each**, sub-label **WM**. Left-most fixed column is the hour (**Hrs**).

So the header reads:

```
        │      Yarmja       │       Qayra       │        T1         │ ...  │ Domez │Summer │Salam1 │Salam2 │
  Hrs   │ KV │ A │ MW │ MV  │ KV │ A │ MW │ MV  │ KV │ A │ MW │ MV  │ ...  │  WM   │  WM   │  WM   │  WM   │
```

(The PC uses a compact single-row header where the KV column shows the group name; the
clearer two-row grouped header above is the target for mobile and carries the same data.)

**Hour order (canonical):** `8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24, 1,2,3,4,5,6,7`.
Always display rows in this order regardless of the order they appear in the QR.

---

## 3. Colors (for visual parity with the PC)

Column group background tints (ARGB hex):

| Group    | Cell background | Group-header (darker) |
|:---------|:----------------|:----------------------|
| Yarmja   | `#FFE6E6FA`     | `#FFC3C3D7`           |
| Qayra    | `#FFE6FFE6`     | `#FFC3DCC3`           |
| T1       | `#FFFFF0F5`     | `#FFDCCDD2`           |
| T2       | `#FFF0FFF0`     | `#FFCDDCCD`           |
| T3       | `#FFF0F8FF`     | `#FFCDD5DC`           |
| Feeders  | `#FFFFF5EE`     | `#FFDCD2CB`           |
| Hrs col  | `#FFD7D7D7` (header) / white cells |            |

Grid lines: light gray (`#FF808080`), 1px. Text: black, centered in each cell.

---

## 4. Validation coloring (OPTIONAL — do this only after the grid works)

The PC tints individual cells when a reading looks wrong. Replicate for full parity.
A cell background overrides the group tint. Values are integers.

Colors: **Missing** `#FFFF6E6E` (red) · **Suspect** `#FFFFF564` (yellow) ·
**Overload/High-MVAR** `#FFFFAA32` (orange).

Rules (apply per cell; a row only gets colored if it has at least one non-empty cell):

- **Missing (red):** cell is empty but its hour-row has at least one other value.
- **Suspect (yellow):**
  - KV of a 132 kV group (Yarmja/Qayra) outside **118..145**.
  - KV of a 33 kV group (T1/T2/T3) outside **28..38**.
  - A (ampere) `< 0`.
  - MW of a 33 kV transformer with `|value| > 150`.
  - Feeder WM `< 0` or `> 500`.
- **Overload / High-MVAR (orange):**
  - MVAR (MV) of a 33 kV transformer with `|value| > 100`, when that group's KV is in 28..38.
  - Feeder WM greater than its **parent transformer MW**: Domez→T1, Summer→T2,
    Salam 1→T2, Salam 2→T3.
  - A transformer's MW less than the **sum of its feeder WMs** (T1 vs Domez; T2 vs
    Summer+Salam 1; T3 vs Salam 2).

If you skip §4, just use the group tints from §3 — the app is still fully usable.

---

## 5. Tech stack

- **Kotlin**, **Jetpack Compose** for UI, **min SDK 24**, target latest stable.
- Scanning: **ZXing Android Embedded** (`com.journeyapps:zxing-android-embedded`) — fully
  offline, launches its own camera scan screen, returns the decoded string. It also
  requests the camera permission at runtime for you.
  - Alternative (if you prefer): Google **ML Kit Barcode Scanning** + **CameraX**. Either is fine.
- No other dependencies required.

### 5.1 Gradle (module `:app`, `build.gradle.kts`)

```kotlin
dependencies {
    implementation("com.journeyapps:zxing-android-embedded:4.3.0")

    val composeBom = platform("androidx.compose:compose-bom:2024.09.02")
    implementation(composeBom)
    implementation("androidx.compose.material3:material3")
    implementation("androidx.compose.ui:ui")
    implementation("androidx.activity:activity-compose:1.9.2")
}
```

Enable Compose in the module (`buildFeatures { compose = true }`) and set a Kotlin
compiler extension version compatible with your Kotlin version.

### 5.2 Manifest

```xml
<uses-permission android:name="android.permission.CAMERA" />
<uses-feature android:name="android.hardware.camera" android:required="false" />
```

---

## 6. Parsing (Kotlin)

```kotlin
import java.time.LocalDate
import java.time.format.DateTimeFormatter

data class LogsheetRow(
    val hour: Int,
    val skipped: Boolean,
    val cells: List<String>   // always size 24; "" = empty cell
)

data class Logsheet(
    val date: LocalDate?,
    val rows: List<LogsheetRow>
)

object LogsheetQrParser {

    private val HOUR_ORDER = (8..24).toList() + (1..7).toList()

    fun parse(payload: String): Logsheet {
        val text = payload.trim()
        val parts = text.split('*')
        require(parts.isNotEmpty()) { "Empty payload" }

        val header = parts[0].trim().split(' ')
        require(header.size >= 2 && header[0] == "LS1") { "Not an LS1 payload" }

        val date = runCatching {
            LocalDate.parse(header[1], DateTimeFormatter.ofPattern("yyyyMMdd"))
        }.getOrNull()

        val rows = parts.drop(1).mapNotNull { seg ->
            val s = seg.trim()
            if (s.isEmpty()) return@mapNotNull null

            val skipped = s.startsWith("-")
            val body = if (skipped) s.substring(1) else s

            val colon = body.indexOf(':')
            require(colon > 0) { "Malformed row: $seg" }

            val hour = body.substring(0, colon).toIntOrNull()
                ?: throw IllegalArgumentException("Bad hour in: $seg")

            // split preserves empty cells; pad/trim to exactly 24
            val raw = body.substring(colon + 1).split('.')
            val cells = List(24) { i -> raw.getOrElse(i) { "" }.filter { it.isDigit() } }

            LogsheetRow(hour, skipped, cells)
        }.sortedBy { HOUR_ORDER.indexOf(it.hour).let { i -> if (i < 0) Int.MAX_VALUE else i } }

        return Logsheet(date, rows)
    }
}
```

Column definitions to drive the header and cell coloring:

```kotlin
enum class Metric { KV, A, MW, MV, WM }

data class ColDef(val group: String, val metric: Metric, val cellIndex: Int)

val TRANSFORMER_GROUPS = listOf("Yarmja", "Qayra", "T1", "T2", "T3")
val FEEDERS = listOf("Domez", "Summer", "Salam 1", "Salam 2")

val COLUMNS: List<ColDef> = buildList {
    var idx = 0
    for (g in TRANSFORMER_GROUPS) {
        add(ColDef(g, Metric.KV, idx++))
        add(ColDef(g, Metric.A,  idx++))
        add(ColDef(g, Metric.MW, idx++))
        add(ColDef(g, Metric.MV, idx++))
    }
    for (f in FEEDERS) add(ColDef(f, Metric.WM, idx++))
}   // 24 entries, cellIndex 0..23
```

---

## 7. Scanning + screen (Compose)

```kotlin
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.journeyapps.barcodescanner.ScanContract
import com.journeyapps.barcodescanner.ScanOptions

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent { MaterialTheme { LogsheetScreen() } }
    }
}

@Composable
fun LogsheetScreen() {
    var sheet by remember { mutableStateOf<Logsheet?>(null) }
    var error by remember { mutableStateOf<String?>(null) }

    val scanLauncher = rememberLauncherForActivityResult(ScanContract()) { result ->
        val contents = result.contents ?: return@rememberLauncherForActivityResult
        runCatching { LogsheetQrParser.parse(contents) }
            .onSuccess { sheet = it; error = null }
            .onFailure { sheet = null; error = it.message ?: "Could not read QR" }
    }

    Column(Modifier.fillMaxSize().padding(12.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Button(onClick = {
                scanLauncher.launch(ScanOptions().apply {
                    setDesiredBarcodeFormats(ScanOptions.QR_CODE)
                    setPrompt("Point at the WinLogosheet QR")
                    setBeepEnabled(true)
                    setOrientationLocked(false)
                })
            }) { Text("Scan QR") }

            Spacer(Modifier.width(12.dp))
            sheet?.date?.let { Text("Date: $it") }
        }

        Spacer(Modifier.height(12.dp))

        when {
            error != null -> Text("⚠ $error", color = MaterialTheme.colorScheme.error)
            sheet == null -> Text("Tap “Scan QR” to load a logsheet.")
            else -> LogsheetGrid(sheet!!)
        }
    }
}
```

---

## 8. Grid rendering with matching coordinates (Compose)

Wrap the table in a vertical + horizontal scroll. Use **fixed cell widths** so columns line
up (that is the "coordinates" requirement): hour column ~48dp, each data column ~56dp.

```kotlin
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextDecoration

private val HRS_W  = 48.dp
private val DATA_W = 56.dp
private val ROW_H  = 30.dp
private val GRID   = Color(0xFF808080)

private fun groupBg(group: String) = when (group) {
    "Yarmja" -> Color(0xFFE6E6FA); "Qayra" -> Color(0xFFE6FFE6)
    "T1" -> Color(0xFFFFF0F5); "T2" -> Color(0xFFF0FFF0); "T3" -> Color(0xFFF0F8FF)
    else -> Color(0xFFFFF5EE)   // feeders
}
private fun groupHeaderBg(group: String) = when (group) {
    "Yarmja" -> Color(0xFFC3C3D7); "Qayra" -> Color(0xFFC3DCC3)
    "T1" -> Color(0xFFDCCDD2); "T2" -> Color(0xFFCDDCCD); "T3" -> Color(0xFFCDD5DC)
    else -> Color(0xFFDCD2CB)
}

@Composable
private fun Cell(text: String, width: androidx.compose.ui.unit.Dp, bg: Color,
                bold: Boolean = false, strike: Boolean = false) {
    Text(
        text = text,
        modifier = Modifier.width(width).height(ROW_H).background(bg)
            .border(0.5.dp, GRID).wrapContentHeight(),
        textAlign = TextAlign.Center,
        fontWeight = if (bold) FontWeight.Bold else FontWeight.Normal,
        textDecoration = if (strike) TextDecoration.LineThrough else null
    )
}

@Composable
fun LogsheetGrid(sheet: Logsheet) {
    Column(Modifier.verticalScroll(rememberScrollState())) {
        Column(Modifier.horizontalScroll(rememberScrollState())) {

            // Row 1: group names (transformers span 4, feeders span 1)
            Row {
                Cell("Hrs", HRS_W, Color(0xFFD7D7D7), bold = true)
                for (g in TRANSFORMER_GROUPS)
                    Cell(g, DATA_W * 4, groupHeaderBg(g), bold = true)
                for (f in FEEDERS)
                    Cell(f, DATA_W, groupHeaderBg("Feeders"), bold = true)
            }

            // Row 2: metric sub-headers
            Row {
                Cell("", HRS_W, Color(0xFFD7D7D7))
                for (col in COLUMNS) {
                    val label = if (col.metric == Metric.MV) "MV" else col.metric.name
                    Cell(label, DATA_W, groupBg(col.group), bold = true)
                }
            }

            // Data rows, one per hour (already sorted into canonical order)
            for (row in sheet.rows) {
                Row {
                    Cell(row.hour.toString().padStart(2, '0'), HRS_W, Color.White,
                        bold = true, strike = row.skipped)
                    for (col in COLUMNS) {
                        val value = row.cells[col.cellIndex]
                        val bg = cellColor(col, value, row) ?: groupBg(col.group)
                        Cell(value, DATA_W, bg)
                    }
                }
            }
        }
    }
}

// §4 validation coloring — return null to keep the plain group tint.
private fun cellColor(col: ColDef, value: String, row: LogsheetRow): Color? {
    // TODO (optional): implement the rules in section 4 and return
    //   Color(0xFFFF6E6E) missing / Color(0xFFFFF564) suspect / Color(0xFFFFAA32) overload
    return null
}
```

---

## 9. Acceptance criteria

1. Fresh install → one screen with a **Scan QR** button and an empty-state message.
2. Tapping **Scan QR** opens the camera (permission requested first run) and scans a QR.
3. Feeding the **test vector** from §1.1 shows a grid with date **2026-04-30** and rows
   **08, 09, 10**, columns grouped Yarmja/Qayra/T1/T2/T3 + Domez/Summer/Salam 1/Salam 2,
   sub-columns KV/A/MW/MV (WM for feeders), values placed at the correct columns, empty
   cells blank, hour **10** shown struck-through (skipped).
4. The grid scrolls horizontally and vertically; columns stay aligned.
5. Group background tints match §3.
6. A malformed / non-LS1 QR shows a readable error, not a crash.

## 10. Edge cases to handle

- **Missing hours:** QR may carry only some of the 24 hours → show only those rows.
- **Skipped hours:** leading `-` → render struck-through / faded, but still show values.
- **Empty cells:** consecutive dots → blank cells; keep column alignment (always 24 cells).
- **Large QR:** a full sheet is a dense QR (up to version 40, 177×177 modules). Ask the
  operator to display it large on the PC and hold the phone steady; ZXing handles it.
- **Non-digit junk** in a cell is stripped by the parser (values are integers only).

---

### Notes on the desktop side (for reference)
The Windows app builds this exact payload in `LogsheetQr.cs` (`BuildPayload`) and renders
the QR with the QRCoder library at error-correction level **M**. If the format ever
changes, the header tag will bump from `LS1` to `LS2`, etc. — check the tag and reject
unknown versions gracefully.
