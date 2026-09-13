# QR handoff to the Android app

The payload format is **`LS1`** — the same contract the existing companion app
already parses. It is taken unchanged from the PreV2 branch
(`WinLogosheet/LogsheetQr.cs` and `AndroidCompanionApp-BuildSpec.md`), so the
app needs no change to read what the new nodes produce.

> **One change is needed on the phone.** See [Hour order](#hour-order) below —
> the day now starts at 07:00, and the app's canonical hour list still starts at
> 08. Without the fix the 07:00 row renders last instead of first. Nothing is
> lost; only that row's position is wrong.

## Showing the code

On the **132 kV server**, hold **Ctrl+Shift** and press **7**, then **8**, then
**9** — each within three seconds of the last. The code appears full screen for
`qrSeconds` (3–5, default 5), then hides. The same sequence while it is up takes
it down; so do **Esc** and a click.

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

Every character stays inside the QR alphanumeric table
(`0-9 A-Z space $ % * + - . / :`) on purpose. That keeps the symbol in
**alphanumeric mode**, which packs two characters into 11 bits instead of eight
bits each. It matters: a full 24-hour session is version 33 at ECC M in
alphanumeric mode against version 40 in byte mode — the difference between an
easy phone scan and one that will not fit at all.

Values are **whole numbers, unsigned** — the sign and the fractional part are
dropped, because the phone's parser keeps only the digits of each cell.

### Error correction

**M first, L as a fallback.** M is robust for a phone camera; the node steps
down to L only if the session will not fit at M. There is deliberately **no
splitting across several codes** — the companion app parses one payload and has
no notion of parts.

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

Identical to PreV2. Columns 0–7 come from the 132 kV node, 8–23 from the 33 kV
node; the `columns` block in `server.config.json` is what binds them.

### Test vector

The PreV2 spec's vector still parses unchanged:

```
LS1 20260430*08:132.250.80.10.133.205.62.8.33.410.92.12.33.380.85.11.32.300.70.9.45.30.20.15*09:131.240..9.....33.400.90.12.........44..19.14*-10:132.250.80.10.133.205.62.8.33.410.92.12.33.380.85.11.32.300.70.9.45.30.20.15
```

Date 2026-04-30; hour 08 full, hour 09 with gaps, hour 10 marked skip.

## Hour order

This is the **one thing the Android app needs changed.**

The session now runs 07:00 → 07:00, so a full day emits hours in this order:

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

Nothing else in the app changes. The parser already tolerates any hour value —
an unknown one sorts last rather than throwing — so an un-updated app still
imports the whole day.

## Verifying without a substation

The payload is plain text. Take one from the server's log line
(`QR shown: … chars …`), or build one by hand from the test vector, and paste it
into any QR generator to test the phone end.

The encoder in `Shared/Qr/QrEncoder.cs` is a full ISO/IEC 18004 implementation —
byte and alphanumeric modes, versions 1–40, all four error-correction levels —
written out rather than taken from a package, because these servers have no
internet access. Both modes were verified module-for-module against a reference
implementation: 680 byte-mode matrices across all 160 version/level
combinations, and 464 alphanumeric-mode matrices.
