# QR handoff to the Android app

The 132 kV node flashes the gathered session on the main screen when the
operator holds **Ctrl+Shift** and presses **7**, **8**, **9**. It stays up for
3–5 seconds, then hides.

## Payload format

One string per QR code:

```
WLS2|<substation>|<yyyy-MM-dd>|<part>/<total>|<crc32>|<chunk>
```

| Field | Example | Notes |
|---|---|---|
| magic | `WLS2` | format version |
| substation | `MSL-E` | `substationCode` from `server.config.json` |
| date | `2026-09-09` | the **session date** — the day the 07:00 session opened |
| part | `1/2` | 1-based part number and the total |
| crc32 | `3F2A9C41` | CRC-32 of the **complete body**, uppercase hex, 8 digits |
| chunk | `H7:129,0,…` | this part's slice of the body |

### The body

Concatenate the chunks in part order — no separator, no trimming:

```
row  := "H" <hour> ":" v1 "," v2 "," … "," v24
body := row (";" row)*
```

- `hour` is the clock hour, `0`–`23`, in session order: 7, 8 … 23, 0, 1 … 6.
- Exactly 24 values per row, in the column order below.
- An **empty field means no value was recorded** for that column that hour. It
  is not a zero.
- Hours with nothing recorded are left out of the body entirely.
- Values are whole units, unsigned — the fractional part and the sign are
  dropped, which is what keeps a whole session inside one symbol.
- Splitting prefers row boundaries, so a part usually ends just after a `;`.
  Do not rely on it — a very long row can be split mid-row.

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

Columns 1–8 come from the 132 kV node, 9–24 from the 33 kV node. The `columns`
block in `server.config.json` is the single source of truth; if it is edited,
this order follows it.

## What the Android app has to do

1. **Scan.** Split on `|` into at most 6 pieces — the chunk may contain no `|`,
   so a 6-way split is safe.
2. **Check the magic.** `WLS2` means this format. Reject anything else with a
   clear message rather than guessing.
3. **Group by `substation` + `date` + `crc32`.** Those three together identify
   one transfer, so a stale part can never mix into a fresh scan.
4. **Collect parts** until you hold 1…N. Show "2 of 3 scanned" so the operator
   knows to keep the phone up — the codes cycle on screen automatically.
5. **Reassemble and verify.** Concatenate in part order and compare CRC-32
   against the header. On mismatch, discard the group and re-scan; never import
   a partial session.
6. **Parse the body.** Treat an empty field as absent, not zero.
7. **Import**, keyed on substation + session date, so re-scanning replaces
   rather than duplicates.

## Alternative formats

Plain unframed text, for a scanner that only wants lines:

```json
"qr": { "format": "csv", "ecc": "L" }
```

A date line, then one `hour,v1,…,v24` line per hour. No framing, no CRC and
**no splitting**, so a session that exceeds one symbol will not be exportable
this way. It is a compatibility path, not the intended one.

For a web endpoint:

```json
"qr": { "format": "url", "ecc": "M", "urlTemplate": "https://logsheet.example/import?d={payload}" }
```

`{payload}` is replaced with the percent-encoded framed string. That inflates
the payload by roughly a third, so expect more parts.

## Scanning notes

A full session at error correction **L** normally fits one code; at **M** it
usually needs two. The window draws the code at about 78 % of the shorter screen
dimension, which is large enough for a phone at arm's length on a typical
operator monitor.

If phones struggle: keep `ecc` at `L`, raise `qrSeconds` to 5, and clean the
monitor. The encoder is a full ISO/IEC 18004 implementation whose output was
verified module-for-module against a reference implementation across all 160
version and error-correction combinations, so a scan failure is an optics
problem, not an encoding one.

To test the phone end without a substation, copy a payload string out of the
server's log and paste it into any QR generator — the framing is plain text and
independent of how the code was drawn.
