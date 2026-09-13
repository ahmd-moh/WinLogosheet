# QR handoff to the Android app

The payload format is **`LS1`** — the same contract the existing companion app
already parses. It is taken unchanged from the PreV2 branch
(`WinLogosheet/LogsheetQr.cs` and `AndroidCompanionApp-BuildSpec.md`), so the
app's parser reads what the nodes produce without a change.

> **Two changes are needed on the phone.**
>
> 1. [Two codes, one sheet](#two-codes-one-sheet). Each substation PC now shows
>    its own code, and the app replaces its sheet on every scan, so the second
>    PC's scan wipes the first. A scan of the same date has to be merged in.
> 2. [Hour order](#hour-order). The day now starts at 07:00, and the app's
>    canonical hour list still starts at 08. Without the fix the 07:00 row
>    renders last instead of first. Nothing is lost; only that row's position
>    is wrong.

## Showing the code

On **either PC**, hold **Ctrl+Shift** and press **7**, then **8**, then **9** —
each within three seconds of the last. The code appears full screen for
`qrSeconds` (3–5, default 5), then hides. The same sequence while it is up takes
it down; so do **Esc** and a click. The caption above it names the substation,
the node (`S132` or `S33`) and the session date.

That is the PreV2 mechanism kept as-is: three separate `RegisterHotKey`
registrations turned into a sequence, rather than a low-level keyboard hook. A
hook would sit in the input path of the whole SCADA desktop; this does not. Both
the top-row and numpad 7/8/9 work. Ctrl rather than Alt on purpose — Alt+Shift
is the input-language toggle on Arabic systems.

`qrScreen` chooses the display; it defaults to `"primary"`, the operator's own
screen, because the secondary head is the wall view being read.

## Payload format

```
LS1 <YYYYMMDD>*<row>*<row>*...
```

- **Header** — the literal `LS1`, one space, then the session date as `YYYYMMDD`.
- Rows are separated by `*`.
- Each **row** is `[-]HH:c1.c2.c3. ... .c24`
  - `HH` — two-digit hour. **Midnight is `24`, never `00`.**
  - A leading `-` marks the hour "skip". The nodes never emit it — there is no
    operator UI to mark an hour any more — but the app should keep handling it.
  - After the `:` there are **always exactly 24 cells**, separated by `.`.
  - A cell is an integer string or **empty**. Empty cells still hold their
    position: `12..7` means `[12] [empty] [7]`.
- Only hours that have values appear. The code carries 1–24 rows.
- Each PC fills **only its own cells** and leaves the other PC's empty.

Every character stays inside the QR alphanumeric table
(`0-9 A-Z space $ % * + - . / :`) on purpose. That keeps the symbol in
**alphanumeric mode**, which packs two characters into 11 bits instead of eight
bits each. Measured with every cell three digits wide, a full day is version 23
on the 132 kV PC and version 29 on the 33 kV PC at ECC M.

Values are **whole numbers, unsigned** — the sign and the fractional part are
dropped, because the phone's parser keeps only the digits of each cell.

### Error correction

**M first, L as a fallback.** M is robust for a phone camera; the node steps
down to L only if the session will not fit at M. There is deliberately **no
splitting across several codes** — the companion app parses one payload per
scan and has no notion of parts.

### Column order

Cell index is 0-based into the 24 cells of a row.

| idx | Group | Metric | | idx | Group | Metric |
|---|---|---|---|---|---|---|
| 0 | Yarmja | KV | | 12 | T2 | KV |
| 1 | Yarmja | A | | 13 | T2 | A |
| 2 | Yarmja | MW | | 14 | T2 | MW |
| 3 | Yarmja | MV (MVAR) | | 15 | T2 | MV (MVAR) |
| 4 | Qayra | KV | | 16 | T3 | KV |
| 5 | Qayra | A | | 17 | T3 | A |
| 6 | Qayra | MW | | 18 | T3 | MW |
| 7 | Qayra | MV (MVAR) | | 19 | T3 | MV (MVAR) |
| 8 | T1 | KV | | 20 | Domez | WM |
| 9 | T1 | A | | 21 | Summer | WM |
| 10 | T1 | MW | | 22 | Salam 1 | WM |
| 11 | T1 | MV (MVAR) | | 23 | Salam 2 | WM |

Identical to PreV2. Cells 0–7 come from the **132 kV PC**, 8–23 from the
**33 kV PC**; the `columns` block in each PC's `node.config.json` is what binds
them.

### Test vector

The PreV2 spec's vector still parses unchanged:

```
LS1 20260430*08:132.250.80.10.133.205.62.8.33.410.92.12.33.380.85.11.32.300.70.9.45.30.20.15*09:131.240..9.....33.400.90.12.........44..19.14*-10:132.250.80.10.133.205.62.8.33.410.92.12.33.380.85.11.32.300.70.9.45.30.20.15
```

Date 2026-04-30; hour 08 full, hour 09 with gaps, hour 10 marked skip.

## Two codes, one sheet

There is no network link between the two substation PCs, so neither can build
the whole sheet. Each shows its own code, with the same session date, filling
only its own cells. These two came out of the nodes for the same hour:

```
132 kV PC:  LS1 20260913*07:129.0.0.0.128.171.35.9................
33 kV PC:   LS1 20260913*07:........33.200.10.3.33.185.9.2.33.170.8.2.4..2.1
```

Scanned in either order, they must leave one row on the phone:

```
07:  129 0 0 0 128 171 35 9 | 33 200 10 3 33 185 9 2 33 170 8 2 4 _ 2 1
```

(`_` is the Summer feeder, which did not read — it stays empty.)

Today the app does `sheet = it` on every successful scan (`LogsheetScreen` in the
Kotlin spec, `showSheet(parse(...))` in the Java one), so the second scan
replaces the first. The rule to put in its place:

- **No sheet yet, a different date, or a date that did not parse** — take the
  scan as it is, which is today's behaviour.
- **Same date** — merge row by row, matched on the hour:
  - a row in only one of them is kept as it is;
  - for a row in both, each cell takes the **scanned value when it is not
    empty**, and otherwise **keeps the value already there**;
  - a row is skipped if either side marks it skipped.
- Sort the merged rows by the canonical hour order, as the parser does.
- A scan that fails to parse shows its error but **keeps the sheet** — it must
  not throw away the first PC's scan.

An empty cell never erases a value, so scanning the same PC again later in the
day only brings in its newer hours, and never blanks the other PC's columns.

The existing "missing" colouring (red when a cell is empty but its row has other
values) needs no change: after both scans it marks only cells that truly did not
read. Between the two scans it marks the other PC's columns, which is a fair
reminder that one code is still to be scanned.

### Kotlin (Compose spec)

In `LogsheetQrParser`, next to `HOUR_ORDER`, expose the position it already sorts
by:

```kotlin
fun position(hour: Int): Int = HOUR_ORDER.indexOf(hour).let { if (it < 0) Int.MAX_VALUE else it }
```

Add the merge:

```kotlin
fun Logsheet.mergedWith(scan: Logsheet): Logsheet {
    if (date == null || date != scan.date) return scan

    val byHour = rows.associateBy { it.hour }.toMutableMap()
    for (row in scan.rows) {
        val old = byHour[row.hour]
        byHour[row.hour] = if (old == null) row else LogsheetRow(
            hour = row.hour,
            skipped = old.skipped || row.skipped,
            cells = List(24) { i -> row.cells[i].ifEmpty { old.cells[i] } }
        )
    }
    return Logsheet(date, byHour.values.sortedBy { LogsheetQrParser.position(it.hour) })
}
```

And in `LogsheetScreen`, merge instead of replacing, and keep the sheet when a
scan fails:

```kotlin
runCatching { LogsheetQrParser.parse(contents) }
    .onSuccess { scanned -> sheet = sheet?.mergedWith(scanned) ?: scanned; error = null }
    .onFailure { error = it.message ?: "Could not read QR" }
```

### Java spec

In `LogsheetQrParser`, make `orderIndex` visible to the package — drop
`private` from `private static int orderIndex(int hour)`. Then add:

```java
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public final class LogsheetMerge {
    private LogsheetMerge() {}

    /** A scan of the same date fills the sheet on screen; anything else replaces it. */
    public static Logsheet merge(Logsheet current, Logsheet scan) {
        if (current == null || current.dateText == null || !current.dateText.equals(scan.dateText))
            return scan;

        Map<Integer, LogsheetRow> byHour = new HashMap<>();
        for (LogsheetRow r : current.rows) byHour.put(r.hour, r);

        for (LogsheetRow r : scan.rows) {
            LogsheetRow old = byHour.get(r.hour);
            if (old == null) { byHour.put(r.hour, r); continue; }

            List<String> cells = new ArrayList<>(24);
            for (int i = 0; i < 24; i++)
                cells.add(r.cells.get(i).isEmpty() ? old.cells.get(i) : r.cells.get(i));
            byHour.put(r.hour, new LogsheetRow(r.hour, old.skipped || r.skipped, cells));
        }

        List<LogsheetRow> rows = new ArrayList<>(byHour.values());
        Collections.sort(rows, (a, b) -> Integer.compare(
                LogsheetQrParser.orderIndex(a.hour), LogsheetQrParser.orderIndex(b.hour)));
        return new Logsheet(current.dateText, rows);
    }
}
```

In `MainActivity`, hold the sheet on screen in a field and merge into it:

```java
private Logsheet currentSheet;

// in the scan result handler, replacing the parse-and-show lines:
try {
    currentSheet = LogsheetMerge.merge(currentSheet, LogsheetQrParser.parse(contents));
    showSheet(currentSheet);
} catch (Exception ex) {
    showError(ex.getMessage() == null ? "Could not read QR" : ex.getMessage());
}
```

## Hour order

The session runs 07:00 → 07:00, so a full day emits hours in this order:

```
07 08 09 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 01 02 03 04 05 06
```

The app's canonical list is still the old 08:00 day:

```kotlin
private val HOUR_ORDER = (8..24).toList() + (1..7).toList()   // Kotlin spec
```
```java
private static final int[] HOUR_ORDER = { 8, …, 24, 1, …, 7 };  // Java spec
```

It sorts rows by position in that list, so hour **07** — the first reading of
the day — lands at the **bottom** of the grid instead of the top. Every value is
still present and correct; only that row's position is wrong.

The fix is one line, in whichever spec you built from:

```kotlin
private val HOUR_ORDER = (7..24).toList() + (1..6).toList()
```
```java
private static final int[] HOUR_ORDER = { 7, 8, …, 24, 1, 2, …, 6 };
```

The parser already tolerates any hour value — an unknown one sorts last rather
than throwing — so an un-updated app still imports the whole day. The merge
above sorts through the same list, so it picks the fix up as well.

## Verifying without a substation

The payload is plain text. Use the test vector or the two codes above, or build
one by hand, and paste it into any QR generator to test the phone end. Scanning
the two example codes one after the other is the quickest check of the merge.

The encoder in `Shared/Qr/QrEncoder.cs` is a full ISO/IEC 18004 implementation —
byte and alphanumeric modes, versions 1–40, all four error-correction levels —
written out rather than taken from a package, because these PCs have no
internet access. Both modes were verified module-for-module against a reference
implementation: 680 byte-mode matrices across all 160 version/level
combinations, and 464 alphanumeric-mode matrices.
