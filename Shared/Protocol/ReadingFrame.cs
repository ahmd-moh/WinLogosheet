using System;
using System.Collections.Generic;
using System.Globalization;

namespace Substation.Shared
{
    /// <summary>One value read out of one ROI row.</summary>
    public sealed class ChannelReading
    {
        /// <summary>"T1.MW", "YARMJA.KV", ...</summary>
        public string Key = "";

        /// <summary>Normalised numeric text ("-10.77"), or "" when unreadable.</summary>
        public string Value = "";

        /// <summary>Exactly what Tesseract returned, kept for troubleshooting.</summary>
        public string RawText = "";

        /// <summary>Tesseract mean confidence for the row, 0-100.</summary>
        public double Confidence;

        public string RoiId = "";

        /// <summary>Row name inside the ROI: KV, A, MW, MVAR.</summary>
        public string Row = "";

        /// <summary>True when the value parsed as a number at acceptable confidence.</summary>
        public bool Ok
        {
            get
            {
                double d;
                return !string.IsNullOrEmpty(Value) &&
                       double.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out d);
            }
        }

        public double AsDouble(double fallback = 0)
        {
            double d;
            return double.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : fallback;
        }

        public Dictionary<string, object> ToJson()
        {
            return new Dictionary<string, object>
            {
                { "key", Key },
                { "value", Value },
                { "raw", RawText },
                { "conf", Math.Round(Confidence, 1) },
                { "roi", RoiId },
                { "row", Row }
            };
        }

        public static ChannelReading FromJson(Dictionary<string, object> node)
        {
            return new ChannelReading
            {
                Key = Json.Str(node, "key"),
                Value = Json.Str(node, "value"),
                RawText = Json.Str(node, "raw"),
                Confidence = Json.Dbl(node, "conf", 0),
                RoiId = Json.Str(node, "roi"),
                Row = Json.Str(node, "row")
            };
        }
    }

    /// <summary>
    /// Everything one node read from its display for one hour. This — not a
    /// screenshot — is what is stored and what the QR code is built from.
    /// </summary>
    public sealed class ReadingFrame
    {
        public string ServerId = "";

        /// <summary>Logsheet session date (the 08:00 workday start), yyyy-MM-dd.</summary>
        public string SessionDate = "";

        /// <summary>Logsheet hour: 8..24 on the session date, then 1..7 the next morning.</summary>
        public int Hour;

        public DateTime CapturedUtc = DateTime.UtcNow;

        public string AgentVersion = "";

        /// <summary>Non-empty when the capture or OCR failed outright.</summary>
        public string Error = "";

        public List<ChannelReading> Channels = new List<ChannelReading>();

        public double MeanConfidence
        {
            get
            {
                if (Channels.Count == 0) return 0;
                double sum = 0;
                int n = 0;
                foreach (var c in Channels)
                {
                    if (!c.Ok) continue;
                    sum += c.Confidence;
                    n++;
                }
                return n == 0 ? 0 : sum / n;
            }
        }

        public ChannelReading Find(string key)
        {
            foreach (var c in Channels)
                if (string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase)) return c;
            return null;
        }

        public bool TryGetValue(string key, out string value)
        {
            var c = Find(key);
            value = c == null ? null : c.Value;
            return c != null && c.Ok;
        }

        public Dictionary<string, object> ToJson()
        {
            var channels = new List<object>();
            foreach (var c in Channels) channels.Add(c.ToJson());

            return new Dictionary<string, object>
            {
                { "serverId", ServerId },
                { "sessionDate", SessionDate },
                { "hour", Hour },
                { "capturedUtc", CapturedUtc.ToString("o", CultureInfo.InvariantCulture) },
                { "agentVersion", AgentVersion },
                { "error", Error },
                { "meanConfidence", Math.Round(MeanConfidence, 1) },
                { "channels", channels }
            };
        }

        public static ReadingFrame FromJson(Dictionary<string, object> node)
        {
            var frame = new ReadingFrame
            {
                ServerId = Json.Str(node, "serverId"),
                SessionDate = Json.Str(node, "sessionDate"),
                Hour = Json.Int(node, "hour", 0),
                AgentVersion = Json.Str(node, "agentVersion"),
                Error = Json.Str(node, "error")
            };

            DateTime ts;
            if (DateTime.TryParse(Json.Str(node, "capturedUtc"), CultureInfo.InvariantCulture,
                                  DateTimeStyles.RoundtripKind, out ts))
                frame.CapturedUtc = ts.ToUniversalTime();

            foreach (object c in Json.List(node, "channels"))
                frame.Channels.Add(ChannelReading.FromJson(Json.Dict(c)));

            return frame;
        }
    }
}
