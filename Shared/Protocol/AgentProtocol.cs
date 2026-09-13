using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Substation.Shared
{
    /// <summary>
    /// Wire format between the 33 kV client node and the 132 kV server node.
    ///
    /// One request or response per line, UTF-8, newline terminated:
    ///
    ///     &lt;signature&gt; &lt;json&gt;\n
    ///
    /// signature is the lowercase hex HMAC-SHA256 of the JSON bytes under the
    /// shared secret, or "-" when no secret is configured. Every message carries
    /// a unix timestamp; the receiver rejects anything outside a two-minute
    /// window so a captured frame cannot be replayed later.
    /// </summary>
    public static class AgentProtocol
    {
        /// <summary>3 adds the reverse channel: poll / calibrate / calibration.
        /// Nothing validates it — the two nodes always ship together — but it
        /// tells a log reader which command set a node was built with.</summary>
        public const int ProtocolVersion = 3;
        public const int DefaultPort = 5115;

        /// <summary>Seconds a signed message stays acceptable.</summary>
        public const int ClockSkewSeconds = 120;

        /// <summary>Guards against a peer streaming an unbounded line at us.</summary>
        public const int MaxLineBytes = 8 * 1024 * 1024;

        // Commands
        public const string CmdHello = "hello";        // identity + ROI inventory
        public const string CmdPush = "push";          // client hands the server one hour's values
        public const string CmdPushBatch = "push_batch"; // client drains a backlog in one round trip
        public const string CmdRead = "read";          // ask a node for one stored hour
        public const string CmdHistory = "history";    // every stored hour of a session date
        public const string CmdStatus = "status";      // health only, no capture

        // The reverse channel. The client is the only one that can open a
        // socket, so anything the server wants done on the client node is
        // parked until the client asks for it.
        public const string CmdPoll = "poll";                 // client: anything for me?
        public const string CmdCalibrate = "calibrate";       // server → client: show me your boxes
        public const string CmdCalibration = "calibration";   // client → server: here they are

        public static readonly Encoding Wire = new UTF8Encoding(false);

        // ── Framing ────────────────────────────────────────────────────────

        public static void WriteMessage(Stream stream, Dictionary<string, object> message, string secret)
        {
            if (!message.ContainsKey("ts"))
                message["ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string json = Json.Write(message);
            string sig = Sign(json, secret);

            byte[] payload = Wire.GetBytes(sig + " " + json + "\n");
            stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }

        /// <summary>
        /// Reads one newline-terminated message. Returns null on a clean close.
        /// Throws IOException on a malformed frame, a bad signature or a stale
        /// timestamp — the caller drops the connection in all three cases.
        /// </summary>
        public static Dictionary<string, object> ReadMessage(Stream stream, string secret)
        {
            string line = ReadLine(stream);
            if (line == null) return null;

            int space = line.IndexOf(' ');
            if (space <= 0) throw new IOException("Malformed frame: missing signature separator.");

            string sig = line.Substring(0, space);
            string json = line.Substring(space + 1);

            string expected = Sign(json, secret);
            if (!FixedTimeEquals(sig, expected))
                throw new IOException("Rejected frame: signature mismatch (shared secret differs).");

            var message = Json.Parse(json);

            if (!string.IsNullOrEmpty(secret))
            {
                long ts = (long)Json.Dbl(message, "ts", 0);
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (ts == 0 || Math.Abs(now - ts) > ClockSkewSeconds)
                    throw new IOException("Rejected frame: timestamp outside the accepted window " +
                                          "(check the clocks on both servers).");
            }

            return message;
        }

        private static string ReadLine(Stream stream)
        {
            var buffer = new MemoryStream();
            var one = new byte[1];

            while (true)
            {
                int read = stream.Read(one, 0, 1);
                if (read <= 0) return buffer.Length == 0 ? null : Wire.GetString(buffer.ToArray());
                if (one[0] == (byte)'\n') return Wire.GetString(buffer.ToArray());
                if (one[0] == (byte)'\r') continue;

                buffer.WriteByte(one[0]);
                if (buffer.Length > MaxLineBytes) throw new IOException("Frame exceeds the maximum line length.");
            }
        }

        // ── Signing ────────────────────────────────────────────────────────

        public static string Sign(string json, string secret)
        {
            if (string.IsNullOrEmpty(secret)) return "-";

            using (var hmac = new HMACSHA256(Wire.GetBytes(secret)))
            {
                byte[] hash = hmac.ComputeHash(Wire.GetBytes(json));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }

        /// <summary>Length-independent comparison, so a mismatch leaks no timing.</summary>
        public static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            int diff = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        // ── Message builders ───────────────────────────────────────────────

        public static Dictionary<string, object> Request(string command)
        {
            return new Dictionary<string, object>
            {
                { "proto", ProtocolVersion },
                { "cmd", command }
            };
        }

        public static Dictionary<string, object> Ok(Dictionary<string, object> body = null)
        {
            var message = new Dictionary<string, object>
            {
                { "proto", ProtocolVersion },
                { "ok", true }
            };
            if (body != null)
                foreach (var kv in body) message[kv.Key] = kv.Value;
            return message;
        }

        public static Dictionary<string, object> Fail(string reason)
        {
            return new Dictionary<string, object>
            {
                { "proto", ProtocolVersion },
                { "ok", false },
                { "error", reason ?? "unknown error" }
            };
        }
    }

    /// <summary>
    /// One instruction parked on the server node for a client node to collect.
    ///
    /// The link only ever opens one way — the client connects out, the server
    /// listens — so the server cannot reach into the client when an engineer
    /// asks it to. It leaves the instruction here instead and the client picks
    /// it up on its next poll, a few seconds later.
    /// </summary>
    public sealed class RemoteCommand
    {
        public string Id = "";

        /// <summary>One of the Cmd* names, e.g. <see cref="AgentProtocol.CmdCalibrate"/>.</summary>
        public string Command = "";

        /// <summary>Node the instruction is meant for. Empty means whoever asks.</summary>
        public string Target = "";

        /// <summary>calibrate: widest image the server wants back, 0 for full size.</summary>
        public int MaxWidth;

        public DateTime QueuedUtc = DateTime.UtcNow;

        public static RemoteCommand New(string command, string target, int maxWidth)
        {
            return new RemoteCommand
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 8),
                Command = command,
                Target = target ?? "",
                MaxWidth = maxWidth
            };
        }

        /// <summary>True when this instruction is addressed to the asking node.</summary>
        public bool IsFor(string nodeId)
        {
            return string.IsNullOrEmpty(Target) ||
                   string.Equals(Target, nodeId, StringComparison.OrdinalIgnoreCase);
        }

        public Dictionary<string, object> ToJson()
        {
            return new Dictionary<string, object>
            {
                { "id", Id },
                { "cmd", Command },
                { "target", Target },
                { "maxWidth", MaxWidth },
                { "queuedUtc", QueuedUtc.ToString("o", CultureInfo.InvariantCulture) }
            };
        }

        public static RemoteCommand FromJson(Dictionary<string, object> node)
        {
            var command = new RemoteCommand
            {
                Id = Json.Str(node, "id"),
                Command = Json.Str(node, "cmd"),
                Target = Json.Str(node, "target"),
                MaxWidth = Json.Int(node, "maxWidth", 0)
            };

            DateTime queued;
            if (DateTime.TryParse(Json.Str(node, "queuedUtc"), CultureInfo.InvariantCulture,
                                  DateTimeStyles.RoundtripKind, out queued))
                command.QueuedUtc = queued.ToUniversalTime();

            return command;
        }
    }
}
