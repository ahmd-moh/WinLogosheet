# Build Prompt — Android Companion App for "WinLogosheet" (offline QR → grid) — **JAVA**

> Copy this **entire file** into a new chat with the Android Studio AI assistant (or any AI),
> or hand it to an Android developer. It is a complete, self-contained specification.
> Build the app exactly as described, in **Java** (no Kotlin).

---

You are an expert Android engineer. Build, **from scratch**, a small **offline** Android
app (**Java**, Android Studio) that scans a QR code produced by a Windows desktop app
("WinLogosheet") and displays the substation hourly logsheet **as a grid, laid out exactly
like the PC**.

There is **no network, no backend, no accounts**. The QR is scanned by the camera, the text
is parsed locally, and the grid is drawn on screen. That's the whole app.

## 0. Summary of the one screen

- A title / top bar with a small **"Scan QR"** button and a date label.
- When **Scan QR** is tapped → open the camera → scan the QR → parse the text → render the
  grid below.
- If nothing scanned yet, show a friendly empty state ("Tap *Scan QR* to load a logsheet").
- The grid is wider and taller than the phone, so it must **scroll horizontally AND
  vertically**.
- A malformed / non-LS1 QR shows a readable error message, never a crash.

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
  - `HH` = two-digit hour, values `08..24` then `01..07` (see §2 for the canonical order).
  - A leading `-` (before `HH`) means **this hour is marked "skip"** on the PC (excluded
    from the printout). Show it, but render it struck-through / faded.
  - After the `:` there are **always exactly 24 cells**, separated by `.`.
  - A cell is an **integer string** (e.g. `132`, `44`) or **empty** (no value). Empty cells
    still occupy their position, so you see adjacent dots: `12..7` means `[12] [empty] [7]`.
- Not every hour is always present. The QR contains **1..24 rows** — only the hours the
  operator has data for. Render whatever rows arrive, always in the canonical order of §2.

### 1.1 Test vector (use this to develop before you have a real QR)

```
LS1 20260430*08:132.250.80.10.133.205.62.8.33.410.92.12.33.380.85.11.32.300.70.9.45.30.20.15*09:131.240..9.....33.400.90.12.........44..19.14*-10:132.250.80.10.133.205.62.8.33.410.92.12.33.380.85.11.32.300.70.9.45.30.20.15
```

Decodes to: date **2026-04-30**, three rows — hour **08** (full), hour **09** (several empty
cells), hour **10** (marked **skip**). Each row expands to exactly 24 cells.

> Tip while developing: add a hidden "paste test text" path, or hard-code the string above,
> so you can build the grid before wiring the camera.

---

## 2. Data model — columns, groups, hours ("coordinates")

There are **24 data columns** per hour, grouped into 6 sections. The **cell index is 0-based**
into the 24 cells of a row (cell 0 = the first cell after `HH:`).

| cell idx | Group     | Metric     | Notes                    |
|:-------:|:----------|:-----------|:-------------------------|
| 0       | Yarmja    | KV         | 132 kV bus               |
| 1       | Yarmja    | A          |                          |
| 2       | Yarmja    | MW         |                          |
| 3       | Yarmja    | MV (=MVAR) |                          |
| 4       | Qayra     | KV         | 132 kV bus               |
| 5       | Qayra     | A          |                          |
| 6       | Qayra     | MW         |                          |
| 7       | Qayra     | MV (=MVAR) |                          |
| 8       | T1        | KV         | 33 kV bus                |
| 9       | T1        | A          |                          |
| 10      | T1        | MW         |                          |
| 11      | T1        | MV (=MVAR) |                          |
| 12      | T2        | KV         | 33 kV bus                |
| 13      | T2        | A          |                          |
| 14      | T2        | MW         |                          |
| 15      | T2        | MV (=MVAR) |                          |
| 16      | T3        | KV         | 33 kV bus                |
| 17      | T3        | A          |                          |
| 18      | T3        | MW         |                          |
| 19      | T3        | MV (=MVAR) |                          |
| 20      | Domez     | WM         | feeder (single value)    |
| 21      | Summer    | WM         | feeder (single value)    |
| 22      | Salam 1   | WM         | feeder (single value)    |
| 23      | Salam 2   | WM         | feeder (single value)    |

**Header layout (two rows):** the 5 transformer groups (Yarmja, Qayra, T1, T2, T3) each span
**4 sub-columns** (KV / A / MW / MV). The 4 feeders (Domez, Summer, Salam 1, Salam 2) are
**1 column each**, sub-label **WM**. Left-most fixed column is the hour (**Hrs**).

```
        │      Yarmja       │       Qayra       │        T1         │ ...  │ Domez │Summer │Salam1 │Salam2 │
  Hrs   │ KV │ A │ MW │ MV  │ KV │ A │ MW │ MV  │ KV │ A │ MW │ MV  │ ...  │  WM   │  WM   │  WM   │  WM   │
```

**Hour order (canonical):** `8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24, 1,2,3,4,5,6,7`.
Always display rows in this order regardless of the order they appear in the QR.

---

## 3. Colors (for visual parity with the PC — ARGB hex)

Column group background tints:

| Group    | Cell background | Group-header (darker) |
|:---------|:----------------|:----------------------|
| Yarmja   | `0xFFE6E6FA`    | `0xFFC3C3D7`          |
| Qayra    | `0xFFE6FFE6`    | `0xFFC3DCC3`          |
| T1       | `0xFFFFF0F5`    | `0xFFDCCDD2`          |
| T2       | `0xFFF0FFF0`    | `0xFFCDDCCD`          |
| T3       | `0xFFF0F8FF`    | `0xFFCDD5DC`          |
| Feeders  | `0xFFFFF5EE`    | `0xFFDCD2CB`          |
| Hrs col  | header `0xFFD7D7D7` / cells white |         |

Grid lines: light gray `0xFF808080`, 1px. Text: black, centered in each cell.
(The group-header color is simply the cell color with 35 subtracted from each RGB channel.)

---

## 4. Validation coloring (matches the PC exactly)

The PC tints individual cells when a reading looks wrong. A cell's validation color
**overrides** its group tint. Values are treated as numbers.

Colors: **Missing** `0xFFFF6E6E` (red) · **Suspect** `0xFFFFF564` (yellow) ·
**Overload / High-MVAR** `0xFFFFAA32` (orange).

Rules (apply per cell; the tolerance for the transformer/feeder comparisons is `1.0`):

- **Missing (red):** cell is empty but its hour-row has at least one other non-empty cell.
- If a non-empty cell is not a valid number → no color.
- **Feeder columns (cell idx 20–23, WM):**
  - `value < 0` or `value > 500` → **yellow**.
  - Else if `|value| > |parent transformer MW| + 1.0` → **orange**.
    Parent MW cell: Domez(20)→T1 MW(10), Summer(21)→T2 MW(14), Salam 1(22)→T2 MW(14),
    Salam 2(23)→T3 MW(18). (If the parent MW cell is empty/non-numeric, skip this check.)
- **Transformer columns** — sub-type = `cellIdx % 4`: `0=KV, 1=A, 2=MW, 3=MV`.
  `132 kV` groups = cell idx 0–7 (Yarmja, Qayra). `33 kV` groups = cell idx 8–19 (T1,T2,T3).
  - **KV (subtype 0):** 132 kV group outside **118..145** → yellow; 33 kV group outside
    **28..38** → yellow.
  - **A (subtype 1):** `value < 0` → yellow.
  - **MW (subtype 2), 33 kV only:**
    - `|value| > 150` → yellow (checked first, takes priority).
    - else if `sum(|child feeder WM|) > |value| + 1.0` → **orange** (load-balance).
      Children: T1 MW(10)→{Domez 20}; T2 MW(14)→{Summer 21, Salam 1 22}; T3 MW(18)→{Salam 2 23}.
      (Only feeders that are present/numeric count; if none present, skip.)
    - 132 kV MW is intentionally **not** validated.
  - **MV / MVAR (subtype 3), 33 kV only:** if `|value| > 100` → look at the same group's KV
    (cell idx − 3). If that KV is in **28..38** → **orange** (High-MVAR), otherwise → yellow.

If you want to ship faster, you can skip §4 at first and just use the group tints from §3 —
the app is still fully usable. But §6.3 gives you the exact code, so prefer to include it.

---

## 5. Tech stack

- **Java**, Android **Views** (XML layout + programmatic table), **AppCompatActivity**.
- **min SDK 24**, compile/target SDK 34 (or latest stable).
- Scanning: **ZXing Android Embedded** (`com.journeyapps:zxing-android-embedded`) — fully
  offline, launches its own camera scan screen, requests the camera permission for you, and
  returns the decoded string.
- No other third-party dependencies required.

### 5.1 Gradle (module `:app`, `build.gradle`, Groovy DSL)

```groovy
android {
    namespace 'com.example.winlogosheetviewer'
    compileSdk 34

    defaultConfig {
        applicationId "com.example.winlogosheetviewer"
        minSdk 24
        targetSdk 34
        versionCode 1
        versionName "1.0"
    }
    compileOptions {
        sourceCompatibility JavaVersion.VERSION_1_8
        targetCompatibility JavaVersion.VERSION_1_8
    }
    buildFeatures { viewBinding false }
}

dependencies {
    implementation 'androidx.appcompat:appcompat:1.7.0'
    implementation 'com.google.android.material:material:1.12.0'
    implementation 'com.journeyapps:zxing-android-embedded:4.3.0'
}
```

### 5.2 Manifest (`AndroidManifest.xml`)

```xml
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
    package="com.example.winlogosheetviewer">

    <uses-permission android:name="android.permission.CAMERA" />
    <uses-feature android:name="android.hardware.camera" android:required="false" />

    <application
        android:allowBackup="true"
        android:label="Logsheet Viewer"
        android:theme="@style/Theme.Material3.DayNight.NoActionBar">
        <activity android:name=".MainActivity" android:exported="true">
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>
        </activity>
    </application>
</manifest>
```

ZXing's own `CaptureActivity` is merged in automatically by the library — you do not declare it.

---

## 6. Java source

Package everything under `com.example.winlogosheetviewer`.

### 6.1 Data model — `LogsheetRow.java`, `Logsheet.java`

```java
package com.example.winlogosheetviewer;

import java.util.List;

public class LogsheetRow {
    public final int hour;
    public final boolean skipped;
    public final List<String> cells;   // always size 24; "" = empty cell

    public LogsheetRow(int hour, boolean skipped, List<String> cells) {
        this.hour = hour;
        this.skipped = skipped;
        this.cells = cells;
    }
}
```

```java
package com.example.winlogosheetviewer;

import java.util.List;

public class Logsheet {
    public final String dateText;          // "yyyy-MM-dd" or null
    public final List<LogsheetRow> rows;   // in canonical hour order

    public Logsheet(String dateText, List<LogsheetRow> rows) {
        this.dateText = dateText;
        this.rows = rows;
    }
}
```

### 6.2 Parser — `LogsheetQrParser.java`

```java
package com.example.winlogosheetviewer;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

/** Parses the "LS1 ..." offline payload produced by WinLogosheet (LogsheetQr.cs). */
public final class LogsheetQrParser {

    private static final int[] HOUR_ORDER = {
        8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
        1, 2, 3, 4, 5, 6, 7
    };

    private LogsheetQrParser() {}

    public static Logsheet parse(String payload) {
        if (payload == null) throw new IllegalArgumentException("Empty payload");
        String text = payload.trim();

        // limit -1 keeps trailing empty fields (empty cells / trailing rows)
        String[] parts = text.split("\\*", -1);
        if (parts.length == 0) throw new IllegalArgumentException("Empty payload");

        String[] header = parts[0].trim().split(" ");
        if (header.length < 2 || !header[0].equals("LS1"))
            throw new IllegalArgumentException("Not an LS1 payload");

        String date = formatDate(header[1]);

        List<LogsheetRow> rows = new ArrayList<>();
        for (int p = 1; p < parts.length; p++) {
            String s = parts[p].trim();
            if (s.isEmpty()) continue;

            boolean skipped = s.startsWith("-");
            String body = skipped ? s.substring(1) : s;

            int colon = body.indexOf(':');
            if (colon <= 0) throw new IllegalArgumentException("Malformed row: " + parts[p]);

            Integer hour = tryParseInt(body.substring(0, colon));
            if (hour == null) throw new IllegalArgumentException("Bad hour in: " + parts[p]);

            String[] raw = body.substring(colon + 1).split("\\.", -1);  // keep empties
            List<String> cells = new ArrayList<>(24);
            for (int i = 0; i < 24; i++)
                cells.add(digitsOnly(i < raw.length ? raw[i] : ""));

            rows.add(new LogsheetRow(hour, skipped, cells));
        }

        Collections.sort(rows, (a, b) ->
            Integer.compare(orderIndex(a.hour), orderIndex(b.hour)));

        return new Logsheet(date, rows);
    }

    private static int orderIndex(int hour) {
        for (int i = 0; i < HOUR_ORDER.length; i++)
            if (HOUR_ORDER[i] == hour) return i;
        return Integer.MAX_VALUE;   // unknown hours sort to the end
    }

    private static String formatDate(String yyyymmdd) {
        if (yyyymmdd != null && yyyymmdd.length() == 8) {
            return yyyymmdd.substring(0, 4) + "-" +
                   yyyymmdd.substring(4, 6) + "-" +
                   yyyymmdd.substring(6, 8);
        }
        return yyyymmdd;
    }

    private static String digitsOnly(String raw) {
        if (raw == null || raw.isEmpty()) return "";
        StringBuilder sb = new StringBuilder(raw.length());
        for (int i = 0; i < raw.length(); i++) {
            char c = raw.charAt(i);
            if (c >= '0' && c <= '9') sb.append(c);
        }
        return sb.toString();
    }

    private static Integer tryParseInt(String s) {
        try { return Integer.parseInt(s.trim()); }
        catch (NumberFormatException e) { return null; }
    }
}
```

### 6.3 Validation — `LogsheetValidator.java`

```java
package com.example.winlogosheetviewer;

/** Mirrors GetValidationColor / CheckFeederAgainstTransformer /
 *  CheckTransformerLoadBalance from the PC's Form1.cs.
 *  Returns an ARGB color for the cell, or 0 for "no override" (use group tint). */
public final class LogsheetValidator {

    public static final int MISSING  = 0xFFFF6E6E; // red
    public static final int SUSPECT  = 0xFFFFF564; // yellow
    public static final int OVERLOAD = 0xFFFFAA32; // orange
    private static final double TOL  = 1.0;

    private LogsheetValidator() {}

    /** cellIndex 0..23. Returns 0 when there is no validation override. */
    public static int cellColor(int cellIndex, LogsheetRow row) {
        String value = row.cells.get(cellIndex);
        boolean empty = value == null || value.trim().isEmpty();

        // 1. Missing value in a row that has other data
        if (empty) return rowHasAnyData(row) ? MISSING : 0;

        Double parsed = tryParseDouble(value);
        if (parsed == null) return 0;
        double val = parsed;

        // Feeders (WM): cell idx 20..23
        if (cellIndex >= 20) {
            if (val < 0 || val > 500) return SUSPECT;
            int parentMw = feederParentMwIndex(cellIndex);
            Double pmw = cellNum(row, parentMw);
            if (pmw != null && Math.abs(val) > Math.abs(pmw) + TOL) return OVERLOAD;
            return 0;
        }

        int subType = cellIndex % 4;                 // 0 KV, 1 A, 2 MW, 3 MV
        boolean is132 = cellIndex <= 7;              // Yarmja + Qayra
        boolean is33  = cellIndex >= 8 && cellIndex <= 19; // T1, T2, T3

        switch (subType) {
            case 0: // KV
                if (is132 && (val < 118 || val > 145)) return SUSPECT;
                if (is33  && (val < 28  || val > 38))  return SUSPECT;
                break;
            case 1: // A
                if (val < 0) return SUSPECT;
                break;
            case 2: // MW (33 kV only)
                if (is33 && Math.abs(val) > 150) return SUSPECT;
                if (is33) {
                    int bal = transformerBalance(cellIndex, val, row);
                    if (bal != 0) return bal;
                }
                break;
            case 3: // MV / MVAR (33 kV only)
                if (is33 && Math.abs(val) > 100) {
                    Double kv = cellNum(row, cellIndex - 3);   // group KV cell
                    boolean kvOk = kv != null && kv >= 28 && kv <= 38;
                    return kvOk ? OVERLOAD : SUSPECT;
                }
                break;
        }
        return 0;
    }

    private static boolean rowHasAnyData(LogsheetRow row) {
        for (String c : row.cells)
            if (c != null && !c.trim().isEmpty()) return true;
        return false;
    }

    // Domez→T1 MW(10), Summer→T2 MW(14), Salam1→T2 MW(14), Salam2→T3 MW(18)
    private static int feederParentMwIndex(int feederIdx) {
        switch (feederIdx) {
            case 20: return 10; // Domez  → T1
            case 21: return 14; // Summer → T2
            case 22: return 14; // Salam1 → T2
            case 23: return 18; // Salam2 → T3
            default: return -1;
        }
    }

    // MW cell vs the sum of its child feeders (orange if feeders exceed transformer).
    private static int transformerBalance(int mwIdx, double xfmrMw, LogsheetRow row) {
        double sum = 0; boolean any = false;
        int[] children;
        switch (mwIdx) {
            case 10: children = new int[]{20};      break; // T1 ← Domez
            case 14: children = new int[]{21, 22};  break; // T2 ← Summer, Salam1
            case 18: children = new int[]{23};      break; // T3 ← Salam2
            default: return 0;
        }
        for (int c : children) {
            Double v = cellNum(row, c);
            if (v != null) { sum += Math.abs(v); any = true; }
        }
        if (!any) return 0;
        return sum > Math.abs(xfmrMw) + TOL ? OVERLOAD : 0;
    }

    private static Double cellNum(LogsheetRow row, int idx) {
        if (idx < 0 || idx >= row.cells.size()) return null;
        return tryParseDouble(row.cells.get(idx));
    }

    private static Double tryParseDouble(String s) {
        if (s == null || s.trim().isEmpty()) return null;
        try { return Double.parseDouble(s.trim()); }
        catch (NumberFormatException e) { return null; }
    }
}
```

### 6.4 Grid renderer — `GridRenderer.java`

Builds the table into a vertical `LinearLayout` (the grid container). Each cell is a
fixed-width `TextView`, so columns line up exactly (the "coordinates" requirement). Group
name cells span 4 data columns by being 4× as wide.

```java
package com.example.winlogosheetviewer;

import android.content.Context;
import android.graphics.Paint;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.util.TypedValue;
import android.view.Gravity;
import android.widget.LinearLayout;
import android.widget.TextView;

import java.util.Locale;

public final class GridRenderer {

    private static final int HRS_W_DP  = 48;
    private static final int DATA_W_DP = 56;
    private static final int ROW_H_DP  = 30;
    private static final int GRID_LINE = 0xFF808080;
    private static final int HRS_HDR_BG = 0xFFD7D7D7;

    static final String[] TRANSFORMERS = {"Yarmja", "Qayra", "T1", "T2", "T3"};
    static final String[] FEEDERS      = {"Domez", "Summer", "Salam 1", "Salam 2"};

    private GridRenderer() {}

    /** container = a vertical LinearLayout inside the 2-axis scroll (see §6.5 layout). */
    public static void render(LinearLayout container, Logsheet sheet) {
        Context ctx = container.getContext();
        container.removeAllViews();
        container.setOrientation(LinearLayout.VERTICAL);

        int hrsW  = dp(ctx, HRS_W_DP);
        int dataW = dp(ctx, DATA_W_DP);
        int rowH  = dp(ctx, ROW_H_DP);

        // Row 1 — group names (transformers span 4 data columns, feeders span 1)
        LinearLayout r1 = row(ctx);
        r1.addView(cell(ctx, "Hrs", hrsW, rowH, HRS_HDR_BG, true, false));
        for (String g : TRANSFORMERS)
            r1.addView(cell(ctx, g, dataW * 4, rowH, groupHeaderColor(g), true, false));
        for (String f : FEEDERS)
            r1.addView(cell(ctx, f, dataW, rowH, groupHeaderColor("Feeders"), true, false));
        container.addView(r1);

        // Row 2 — metric sub-headers
        LinearLayout r2 = row(ctx);
        r2.addView(cell(ctx, "", hrsW, rowH, HRS_HDR_BG, false, false));
        for (int i = 0; i < 24; i++)
            r2.addView(cell(ctx, metricLabel(i), dataW, rowH, groupLightColor(i), true, false));
        container.addView(r2);

        // Data rows (already in canonical hour order)
        for (LogsheetRow lr : sheet.rows) {
            LinearLayout dr = row(ctx);
            String hourText = String.format(Locale.US, "%02d", lr.hour);
            dr.addView(cell(ctx, hourText, hrsW, rowH, 0xFFFFFFFF, true, lr.skipped));
            for (int i = 0; i < 24; i++) {
                String value = lr.cells.get(i);
                int vc = LogsheetValidator.cellColor(i, lr);
                int bg = vc != 0 ? vc : groupLightColor(i);
                dr.addView(cell(ctx, value, dataW, rowH, bg, false, false));
            }
            container.addView(dr);
        }
    }

    private static LinearLayout row(Context ctx) {
        LinearLayout ll = new LinearLayout(ctx);
        ll.setOrientation(LinearLayout.HORIZONTAL);
        return ll;
    }

    private static TextView cell(Context ctx, String text, int w, int h, int bg,
                                 boolean bold, boolean strike) {
        TextView tv = new TextView(ctx);
        tv.setText(text == null ? "" : text);
        tv.setLayoutParams(new LinearLayout.LayoutParams(w, h));
        tv.setGravity(Gravity.CENTER);
        tv.setTextColor(0xFF000000);
        tv.setTextSize(TypedValue.COMPLEX_UNIT_SP, 12);
        tv.setSingleLine(true);
        if (bold) tv.setTypeface(Typeface.DEFAULT_BOLD);
        if (strike) tv.setPaintFlags(tv.getPaintFlags() | Paint.STRIKE_THRU_TEXT_FLAG);

        GradientDrawable border = new GradientDrawable();
        border.setColor(bg);
        border.setStroke(1, GRID_LINE);   // 1px grid line
        tv.setBackground(border);
        return tv;
    }

    private static String metricLabel(int cellIndex) {
        if (cellIndex >= 20) return "WM";
        switch (cellIndex % 4) {
            case 0:  return "KV";
            case 1:  return "A";
            case 2:  return "MW";
            default: return "MV";
        }
    }

    private static int groupLightColor(int cellIndex) {
        if (cellIndex <= 3)  return 0xFFE6E6FA; // Yarmja
        if (cellIndex <= 7)  return 0xFFE6FFE6; // Qayra
        if (cellIndex <= 11) return 0xFFFFF0F5; // T1
        if (cellIndex <= 15) return 0xFFF0FFF0; // T2
        if (cellIndex <= 19) return 0xFFF0F8FF; // T3
        return 0xFFFFF5EE;                       // feeders
    }

    private static int groupHeaderColor(String group) {
        switch (group) {
            case "Yarmja": return 0xFFC3C3D7;
            case "Qayra":  return 0xFFC3DCC3;
            case "T1":     return 0xFFDCCDD2;
            case "T2":     return 0xFFCDDCCD;
            case "T3":     return 0xFFCDD5DC;
            default:       return 0xFFDCD2CB;   // feeders
        }
    }

    private static int dp(Context ctx, int value) {
        return (int) (value * ctx.getResources().getDisplayMetrics().density + 0.5f);
    }
}
```

### 6.5 Layout — `res/layout/activity_main.xml`

A vertical page: the top bar, an empty-state / error message, and a **2-axis scroll**
(`HorizontalScrollView` wrapping a `ScrollView` wrapping the grid container).

```xml
<?xml version="1.0" encoding="utf-8"?>
<LinearLayout xmlns:android="http://schemas.android.com/apk/res/android"
    android:layout_width="match_parent"
    android:layout_height="match_parent"
    android:orientation="vertical"
    android:padding="12dp">

    <LinearLayout
        android:layout_width="match_parent"
        android:layout_height="wrap_content"
        android:gravity="center_vertical"
        android:orientation="horizontal">

        <Button
            android:id="@+id/btnScan"
            android:layout_width="wrap_content"
            android:layout_height="wrap_content"
            android:text="Scan QR" />

        <TextView
            android:id="@+id/dateLabel"
            android:layout_width="wrap_content"
            android:layout_height="wrap_content"
            android:layout_marginStart="12dp"
            android:textStyle="bold" />
    </LinearLayout>

    <TextView
        android:id="@+id/message"
        android:layout_width="match_parent"
        android:layout_height="wrap_content"
        android:layout_marginTop="12dp"
        android:text="Tap “Scan QR” to load a logsheet." />

    <HorizontalScrollView
        android:id="@+id/hScroll"
        android:layout_width="match_parent"
        android:layout_height="0dp"
        android:layout_weight="1"
        android:layout_marginTop="12dp"
        android:visibility="gone">

        <ScrollView
            android:layout_width="wrap_content"
            android:layout_height="match_parent">

            <LinearLayout
                android:id="@+id/grid"
                android:layout_width="wrap_content"
                android:layout_height="wrap_content"
                android:orientation="vertical" />
        </ScrollView>
    </HorizontalScrollView>
</LinearLayout>
```

### 6.6 Activity — `MainActivity.java`

Wires the button to ZXing's `ScanContract` (via the AndroidX ActivityResult API), parses the
result, and renders the grid. On any parse error it shows a readable message instead.

```java
package com.example.winlogosheetviewer;

import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.activity.result.ActivityResultLauncher;
import androidx.appcompat.app.AppCompatActivity;

import com.journeyapps.barcodescanner.ScanContract;
import com.journeyapps.barcodescanner.ScanIntentResult;
import com.journeyapps.barcodescanner.ScanOptions;

public class MainActivity extends AppCompatActivity {

    private LinearLayout grid;
    private View hScroll;
    private TextView dateLabel;
    private TextView message;

    private final ActivityResultLauncher<ScanOptions> scanLauncher =
        registerForActivityResult(new ScanContract(), this::onScanResult);

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        grid      = findViewById(R.id.grid);
        hScroll   = findViewById(R.id.hScroll);
        dateLabel = findViewById(R.id.dateLabel);
        message   = findViewById(R.id.message);

        Button scan = findViewById(R.id.btnScan);
        scan.setOnClickListener(v -> startScan());
    }

    private void startScan() {
        ScanOptions options = new ScanOptions();
        options.setDesiredBarcodeFormats(ScanOptions.QR_CODE);
        options.setPrompt("Point at the WinLogosheet QR");
        options.setBeepEnabled(true);
        options.setOrientationLocked(false);
        scanLauncher.launch(options);
    }

    private void onScanResult(ScanIntentResult result) {
        String contents = result.getContents();
        if (contents == null) return;   // user pressed back / cancelled
        try {
            Logsheet sheet = LogsheetQrParser.parse(contents);
            showSheet(sheet);
        } catch (Exception ex) {
            showError(ex.getMessage() == null ? "Could not read QR" : ex.getMessage());
        }
    }

    private void showSheet(Logsheet sheet) {
        dateLabel.setText(sheet.dateText == null ? "" : "Date: " + sheet.dateText);
        GridRenderer.render(grid, sheet);
        message.setVisibility(View.GONE);
        hScroll.setVisibility(View.VISIBLE);
    }

    private void showError(String msg) {
        grid.removeAllViews();
        hScroll.setVisibility(View.GONE);
        message.setVisibility(View.VISIBLE);
        message.setText("⚠ " + msg);
    }
}
```

---

## 7. Acceptance criteria

1. Fresh install → one screen with a **Scan QR** button and the empty-state message.
2. Tapping **Scan QR** opens the camera (permission requested on first run) and scans a QR.
3. Feeding the **test vector** from §1.1 shows a grid with **Date: 2026-04-30** and rows
   **08, 09, 10**, columns grouped Yarmja/Qayra/T1/T2/T3 + Domez/Summer/Salam 1/Salam 2,
   sub-columns KV/A/MW/MV (WM for feeders), values in the correct columns, empty cells blank,
   hour **10** shown struck-through (skipped).
4. The grid scrolls horizontally and vertically; columns stay aligned.
5. Group background tints match §3; validation tints match §4 (red/yellow/orange override the
   group tint on the offending cells).
6. A malformed / non-LS1 QR shows a readable error, not a crash.

## 8. Edge cases to handle

- **Missing hours:** the QR may carry only some of the 24 hours → show only those rows.
- **Skipped hours:** leading `-` → render the hour struck-through / faded, but still show its
  values.
- **Empty cells:** consecutive dots → blank cells; keep column alignment (always 24 cells).
- **Large QR:** a full sheet is a dense QR (up to version 40, 177×177 modules). Ask the
  operator to display it large on the PC and hold the phone steady; ZXing handles it.
- **Non-digit junk** in a cell is stripped by the parser (values are integers only).

---

### Notes on the desktop side (for reference)
The Windows app builds this exact payload in `LogsheetQr.cs` (`BuildPayload`) and renders the
QR with the QRCoder library at error-correction level **M**. The validation colors replicate
`GetValidationColor` in `Form1.cs`. If the format ever changes, the header tag bumps from
`LS1` to `LS2`, etc. — check the tag and reject unknown versions gracefully.
```
