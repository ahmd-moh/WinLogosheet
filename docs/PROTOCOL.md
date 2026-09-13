# Socket protocol

Between the 33 kV client node and the 132 kV server node. Implemented by
`Shared/Protocol/AgentProtocol.cs`, `SubstationOcrServer/ReadingServer.cs` and
`SubstationOcrClient/ReadingUplink.cs`.

## Transport

Plain TCP, default port **5115**. One line per message, UTF-8, `\n` terminated:

```
<signature> <json>\n
```

`signature` is the lowercase hex HMAC-SHA256 of the JSON bytes under the shared
secret, or `-` when no secret is configured.

The client opens a connection, pushes, reads the acknowledgement and closes.
It may send several requests on the one connection — a drain and the poll that
follows it share one. Maximum line length is 8 MiB. A malformed frame, a bad
signature or a stale timestamp drops the connection.

**Only the client connects.** Anything the server wants done on the client is
parked until the client asks for it; see [`poll`](#poll--the-client-asks-for-work).

## Common fields

| Field | Type | Meaning |
|---|---|---|
| `proto` | int | protocol version, currently `3` |
| `ts` | int | unix seconds; rejected if more than 120 s from the receiver's clock |

Requests add `cmd`; responses add `ok`, and `error` when `ok` is false.

**The two servers' clocks must agree within two minutes.** After the firewall,
this is the most common commissioning failure.

## Commands

### `push_batch` — the client delivers its queue

```json
{
  "proto": 2, "cmd": "push_batch", "ts": 1789000000,
  "nodeId": "S33",
  "sessionDate": "2026-09-09",
  "frames": [ { "serverId": "S33", "sessionDate": "2026-09-09", "hour": 18,
                "capturedUtc": "2026-09-09T15:02:04.1230000Z",
                "agentVersion": "2.1.0", "error": "", "meanConfidence": 91.4,
                "channels": [
                  { "key": "T1.A", "value": "200.00", "raw": "200.00 A",
                    "conf": 93.1, "roi": "T1", "row": "A" }
                ] } ]
}
```

Response names the hours that were stored. Anything not listed stays queued and
is offered again on the next retry:

```json
{ "proto": 2, "ok": true, "accepted": [18] }
```

A frame is refused, and left out of `accepted`, when it has no node id or
session date, when its hour is outside the 07:00 → 07:00 sequence, or when its
`serverId` matches the receiving node — that last one catches two nodes
configured with the same `nodeId` before it corrupts a session.

### `push` — one hour

Same, with a single `frame` object instead of a `frames` array.

### `hello` — identity and ROI inventory

```json
{
  "proto": 2, "ok": true,
  "serverId": "S132",
  "displayName": "132 kV SWITCHGEAR SLD WALL VIEW",
  "agentVersion": "2.1.0",
  "machine": "SCADA-132KV",
  "captureScreen": "secondary 1920x1080 at (1920,0)",
  "captureMinute": 2,
  "referenceWidth": 1885, "referenceHeight": 941,
  "rois": [ { "id": "YARMJA", "label": "OHL-1 OLD YARMJA", "enabled": true,
              "rows": ["KV","A","MW","MVAR"],
              "channels": ["YARMJA.KV","YARMJA.A","YARMJA.MW","YARMJA.MVAR"] } ]
}
```

### `status` — health

```json
{ "proto": 2, "ok": true, "serverId": "S132", "agentVersion": "2.1.0",
  "localTime": "2026-09-09T18:31:00.0000000+03:00",
  "lastCapture": "2026-09-09T18:02:04.1230000+03:00",
  "lastPush": "2026-09-09T18:03:11.0000000+03:00",
  "lastError": "", "connectionsServed": 412 }
```

Comparing `localTime` against your own clock is the quickest way to diagnose a
timestamp rejection.

### `history` — every stored hour of a session

Request adds `sessionDate`. The response carries frames from **both** nodes,
ordered by session position (07 … 23, then 00 … 06) and then by node id.

### `poll` — the client asks for work

Sent by the client every `pollSeconds` (default 20), and on the back of every
push. Two small lines on an idle socket.

```json
{ "proto": 3, "cmd": "poll", "ts": 1789000000, "nodeId": "S33" }
```

The response carries whatever the server has parked for that node, and hands
each instruction out **once**:

```json
{ "proto": 3, "ok": true,
  "commands": [ { "id": "95737275", "cmd": "calibrate", "target": "S33",
                  "maxWidth": 1920, "queuedUtc": "2026-09-12T08:53:27.0000000Z" } ] }
```

`commands` is normally empty. An instruction nobody collects within ten minutes
is dropped. A node whose `nodeId` does not match `clientNodeId` on the server
never sees the request — the server logs the first poll from each node id, which
is the line that shows the two names really match.

### `calibrate` — the server asks to see the boxes

Never sent on its own; it arrives inside a `poll` response. The client reads its
own screen, OCRs the marked boxes, draws them on the capture and answers with
`calibration`. `maxWidth` is the widest picture the server wants back; 0 means
full resolution.

### `calibration` — the client sends the picture back

```json
{
  "proto": 3, "cmd": "calibration", "ts": 1789000018,
  "nodeId": "S33",
  "requestId": "95737275",
  "shot": {
    "nodeId": "S33", "displayName": "33 kV SWITCHGEAR SLD WALL VIEW",
    "machine": "SCADA-33KV", "agentVersion": "2.1.0",
    "screen": "secondary 1885x941 at (1536,0)",
    "screenWidth": 1885, "screenHeight": 941,
    "referenceWidth": 1885, "referenceHeight": 941,
    "takenUtc": "2026-09-12T08:53:37.0000000Z",
    "error": "", "imageFormat": "png", "image": "<base64>",
    "boxes": [ { "id": "T1", "label": "TRANSFORMER INCOMER-1", "enabled": true,
                 "rect": { "x": 301, "y": 151, "w": 46, "h": 28 },
                 "readings": [ { "key": "T1.A", "value": "200.00", "raw": "200.00",
                                 "conf": 93.1, "roi": "T1", "row": "A" } ] } ]
  }
}
```

```json
{ "proto": 3, "ok": true, "saved": "…\\Calibration\\S33-20260912-115337.png" }
```

`image` is the capture with the boxes and the values already drawn on it — PNG,
or JPEG when the PNG would exceed 3 MiB. `rect` is in **actual screen pixels**,
after any scaling from the ROI file's reference size, so it is the number to
edit back into the ROI file when a box has drifted.

The server refuses a shot whose `nodeId` matches its own, exactly as it refuses
such a reading frame. A shot whose `error` is set and that carries no picture is
still accepted: the reason is what the engineer needs to see.

This is the only message that carries an image. Hourly readings never do.

## Errors

```json
{ "proto": 3, "ok": false, "error": "Unknown command 'foo'." }
```

Connection-level failures close the socket instead of replying, and the reason
goes to the server's log:

| Condition | Log line |
|---|---|
| bad signature | `Dropping <ip>: Rejected frame: signature mismatch (shared secret differs).` |
| stale timestamp | `Dropping <ip>: Rejected frame: timestamp outside the accepted window …` |
| host not allowed | `Refused connection from <ip> (not in allowedClients).` |
