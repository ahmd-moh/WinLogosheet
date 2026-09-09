# QR handoff to the Android app

Same idea as before — the operator holds up the phone, scans, and the day is on
it. What changed in V2 is that the payload is now **framed, checksummed and
splittable**, because a full 24-hour day from two servers is larger than one QR
symbol can always hold.

## Using it

**QR → Mobile** on the V2 strip opens the QR window.

| Control | Effect |
|---|---|
| *Whole day / This hour only* | the full sheet, or just the hour you are on |
| *Error correction* | L fits the most data per code; H survives the most damage |
| *◀ Part / Part ▶* | when the day needs more than one code |
| *Save PNG…* | write the code to a file |
| *Print* | print it, for a code that goes in the folder |
| *Copy payload* | the raw text, for testing the phone side |

Under the code you get the payload size, the QR version and the module count.

**A full day is a dense code.** Typical readings fit one symbol at error
correction **L**; at **M** they usually need two. If phones struggle to scan,
in order of effectiveness: switch to L, maximise the window (the code is drawn
as large as it fits), clean the monitor, or scan hour by hour.

## Payload format

One string per QR code:

```
WLS2|<substation>|<yyyy-MM-dd>|<part>/<total>|<crc32>|<chunk>
```

| Field | Example | Notes |
|---|---|---|
| magic | `WLS2` | format version. V1 payloads do not start with this |
| substation | `MSL-E` | `substationCode` from `winlogosheet.v2.json`; letters, digits, `-`, `_` only |
| date | `2026-09-09` | the **session date** — the day the sheet opened at 08:00 |
| part | `1/2` | 1-based part number and the total |
| crc32 | `3F2A9C41` | CRC-32 of the **complete body**, uppercase hex, 8 digits |
| chunk | `H8:129,0,…` | this part's slice of the body |

### The body

Concatenate the chunks in part order — no separator, no trimming — and you have
the body:

```
row := "H" <hour> ":" v1 "," v2 "," … "," v24
body := row (";" row)*
```

- `hour` is the logsheet hour: `8`…`24`, then `1`…`7`. Midnight is `24`.
- Exactly 24 values per row, in logsheet column order (see the table below).
- An **empty field means no value was recorded** for that column that hour. It
  is not a zero.
- Hours with nothing recorded, and hours the operator marked "blank on print",
  are left out of the body entirely.
- Splitting prefers row boundaries, so a part usually ends just after a `;` and
  parses as whole hours on its own. Do not rely on that — a very long single row
  can be split mid-row.

### Column order

| # | Measurement | | # | Measurement |
|---|---|---|---|---|
| 1 | Yarmja KV | | 13 | T2 KV |
| 2 | Yarmja A | | 14 | T2 A |
| 3 | Yarmja MW | | 15 | T2 MW |
| 4 | Yarmja MVAR | | 16 | T2 MVAR |
| 5 | Qayra KV | | 17 | T3 KV |
| 6 | Qayra A | | 18 | T3 A |
| 7 | Qayra MW | | 19 | T3 MW |
| 8 | Qayra MVAR | | 20 | T3 MVAR |
| 9 | T1 KV | | 21 | Domez MW |
| 10 | T1 A | | 22 | Summer MW |
| 11 | T1 MW | | 23 | Salam 1 MW |
| 12 | T1 MVAR | | 24 | Salam 2 MW |

This is the same order as the on-screen grid and the Excel sheet. If the
substation's column map is edited, this order follows it — the map in
`winlogosheet.v2.json` is the single source of truth.

### Worked example

Two parts:

```
WLS2|MSL-E|2026-09-09|1/2|3F2A9C41|H8:129,0,0,0,128,171,35,9,33,200,10,2,…;H9:…;
WLS2|MSL-E|2026-09-09|2/2|3F2A9C41|H14:…;H15:…
```

Reassembled body:

```
H8:129,0,0,0,128,171,35,9,33,200,10,2,…;H9:…;H14:…;H15:…
```

CRC-32 of that string, uppercase hex, must equal `3F2A9C41`.

## What the Android app has to do

1. **Scan.** Split the text on `|` into at most 6 pieces — the chunk itself may
   contain no `|`, so a plain 6-way split is safe.
2. **Check the magic.** `WLS2` means this format. Anything else is a V1 payload
   or a foreign code; reject it with a clear message rather than guessing.
3. **Group by `substation` + `date` + `crc32`.** Three fields together identify
   one day's transfer. A code from a different day or a re-exported day has a
   different key, so a stale part can never mix into a fresh scan.
4. **Collect parts** until you hold 1…N. Show "2 of 3 scanned" so the operator
   knows to keep going.
5. **Reassemble and verify.** Concatenate in part order and compare CRC-32
   against the header. On mismatch, discard the whole group and ask for a
   re-scan — never import a partial day.
6. **Parse the body** as above. Treat an empty field as absent, not zero.
7. **Import**, keyed on substation + session date, so re-scanning the same day
   replaces rather than duplicates it.

### Migrating from the V1 app

The magic prefix is what makes this safe: a V1 app that does not know `WLS2`
sees an unrecognised string, and a V2 app can support both by branching on the
prefix. If your existing app cannot be updated yet, set

```json
"qr": { "format": "csv", "ecc": "L" }
```

which emits plain unframed text instead — a date line, then one
`hour,v1,…,v24` line per hour. There is no framing, no CRC and **no splitting**,
so a day that exceeds one symbol will not be exportable in this mode. It is a
compatibility path, not the intended one.

### URL mode

For a workflow that hands the data to a web endpoint instead:

```json
"qr": { "format": "url", "ecc": "M", "urlTemplate": "https://logsheet.example/import?d={payload}" }
```

`{payload}` is replaced with the percent-encoded framed string. Percent-encoding
inflates the payload by roughly a third, so expect more parts.

## Verifying the codes

The encoder in `WinLogosheet/V2/QrEncoder.cs` is a full ISO/IEC 18004
implementation — byte mode, versions 1–40, all four error-correction levels —
written out rather than taken from a package, because these servers have no
internet access and a build that needs one more download is a build that does
not happen. Its output was checked module-for-module against a reference
implementation across all 160 version/level combinations.

To check the phone end without a substation: press **Copy payload**, paste the
string into any QR generator, and scan that. The framing is plain text and
independent of how the code was drawn.
