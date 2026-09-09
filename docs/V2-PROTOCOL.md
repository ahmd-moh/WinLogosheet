# Agent socket protocol

Reference for anyone writing another client, or debugging a link with a raw
socket. Implemented by `Shared/Protocol/AgentProtocol.cs` (both sides),
`SubstationOcrAgent/AgentServer.cs` (server) and
`WinLogosheet/V2/RemoteAgentClient.cs` (client).

## Transport

Plain TCP, default port **5115**. One line per message, UTF-8, `\n` terminated:

```
<signature> <json>\n
```

`signature` is the lowercase hex HMAC-SHA256 of the JSON bytes under the shared
secret, or the single character `-` when no secret is configured.

Requests and responses alternate on one connection; the client may send several
in a row before closing. WinLogosheet opens a short connection per operation and
closes it — nothing holds a socket open overnight.

Maximum line length is 8 MiB. A malformed frame, a bad signature or a stale
timestamp drops the connection.

## Common fields

Every message carries:

| Field | Type | Meaning |
|---|---|---|
| `proto` | int | protocol version, currently `2` |
| `ts` | int | unix seconds; rejected if more than 120 s from the receiver's clock |

Requests add `cmd`. Responses add `ok` (bool) and, when `ok` is false, `error`.

**The clocks on both servers must agree within two minutes.** This is the most
common commissioning failure after the firewall.

## Commands

### `hello` — identity and ROI inventory

Request: `{"proto":2,"cmd":"hello","ts":1789000000}`

Response:

```json
{
  "proto": 2, "ok": true,
  "serverId": "S33",
  "displayName": "33 kV SWITCHGEAR SLD WALL VIEW",
  "agentVersion": "2.0.0",
  "machine": "SCADA-33KV",
  "workdayStartHour": 8,
  "captureMinute": 2,
  "autoCapture": true,
  "allowRoiImages": false,
  "referenceWidth": 1885,
  "referenceHeight": 941,
  "rois": [
    { "id": "T1", "label": "TRANSFORMER INCOMER-1", "enabled": true,
      "rows": ["A","MW","MVAR"],
      "channels": ["T1.A","T1.MW","T1.MVAR"] }
  ]
}
```

### `read` — one hour

| Field | Default | Meaning |
|---|---|---|
| `sessionDate` | the agent's current session date | `yyyy-MM-dd`; the 08:00 workday start, so hours 1–7 belong to the previous calendar day |
| `hour` | the agent's current logsheet hour | 8–24, then 1–7; midnight is hour **24**, not 0 |
| `fresh` | `false` | `true` re-reads the display now instead of returning what was stored at `:02` |

Response carries a **reading frame**:

```json
{
  "proto": 2, "ok": true, "cached": true,
  "frame": {
    "serverId": "S33",
    "sessionDate": "2026-09-09",
    "hour": 18,
    "capturedUtc": "2026-09-09T15:02:04.1230000Z",
    "agentVersion": "2.0.0",
    "error": "",
    "meanConfidence": 91.4,
    "channels": [
      { "key": "T1.A",    "value": "200.00",  "raw": "200.00 A",  "conf": 93.1, "roi": "T1", "row": "A" },
      { "key": "T1.MW",   "value": "-10.77",  "raw": "-10.77 MW", "conf": 90.8, "roi": "T1", "row": "MW" },
      { "key": "T1.MVAR", "value": "-2.34",   "raw": "-2.34 MVar","conf": 89.9, "roi": "T1", "row": "MVAR" }
    ]
  }
}
```

- `value` is the normalised number, or `""` when the row could not be read.
- `raw` is exactly what Tesseract returned, for troubleshooting.
- `conf` is the mean confidence for that row, 0–100.
- A non-empty `frame.error` means the capture itself failed; `channels` will be
  empty. The response is still `ok: true` — the request succeeded, the reading
  did not.

### `history` — every stored hour of a day

Request adds `sessionDate`. Response:

```json
{ "proto": 2, "ok": true, "sessionDate": "2026-09-09", "frames": [ ... ] }
```

Frames are ordered by logsheet position (8…24, then 1…7), not numerically.
This is what **Backfill day** uses.

### `status` — health, no capture

```json
{
  "proto": 2, "ok": true,
  "serverId": "S33", "agentVersion": "2.0.0",
  "localTime": "2026-09-09T18:31:00.0000000+03:00",
  "lastCapture": "2026-09-09T18:02:04.1230000+03:00",
  "lastError": "",
  "connectionsServed": 412
}
```

Cheap enough to poll. Comparing `localTime` against your own clock is the
quickest way to diagnose a timestamp rejection.

### `calibrate` — write an ROI overlay

Makes the agent save a screenshot with its ROI boxes drawn on it, **on its own
disk**. Only the path comes back:

```json
{ "proto": 2, "ok": true, "overlayPath": "C:\\SubstationOcrAgent\\Calibration\\overlay-20260909-183104.png", "machine": "SCADA-33KV" }
```

### `roi_image` — one ROI crop, on demand

| Field | Default | Meaning |
|---|---|---|
| `roi` | — | ROI id, e.g. `"T1"` |
| `prepared` | `true` | `true` returns the binarised image as Tesseract sees it; `false` the raw crop |

Response: `{ "ok": true, "roi": "T1", "prepared": true, "png": "<base64>" }`

**Disabled by default.** With `allowRoiImages: false` the agent answers:

```json
{ "proto": 2, "ok": false, "error": "ROI images are disabled on this agent. Set allowRoiImages to true in agent.config.json to troubleshoot." }
```

The hourly path never calls this. It exists so an operator chasing one bad
reading can see what the OCR saw.

## Errors

```json
{ "proto": 2, "ok": false, "error": "Unknown command 'foo'." }
```

Connection-level failures close the socket instead of replying, and the reason
is written to the agent's log:

| Condition | Log line |
|---|---|
| bad signature | `Dropping <ip>: Rejected frame: signature mismatch (shared secret differs).` |
| stale timestamp | `Dropping <ip>: Rejected frame: timestamp outside the accepted window …` |
| host not allowed | `Refused connection from <ip> (not in allowedClients).` |

## Talking to an agent by hand

With no `sharedSecret` set, the signature field is `-`, so a plain socket works:

```
-  {"proto":2,"cmd":"hello","ts":1789000000}
```

(one space between `-` and the JSON, newline at the end). With a secret set you
must compute the HMAC, so use `Agents… → Test agents` in WinLogosheet instead.
