using System;
using System.Collections.Generic;
using System.Globalization;
using Substation.Capture;
using Substation.Qr;
using Substation.Shared;

namespace SubstationOcrServer
{
    /// <summary>
    /// Holds both nodes' readings for the session and folds them into the 24
    /// columns the QR payload carries.
    ///
    /// The server's own readings arrive from its scheduler; the client's arrive
    /// over the socket. Both are written to the same hour store, keyed by node,
    /// so a restart picks the session back up from disk.
    /// </summary>
    public sealed class MergedStore
    {
        private readonly ServerConfig _config;
        private readonly HourStore _store;
        private readonly object _gate = new object();

        public MergedStore(ServerConfig config, HourStore store)
        {
            _config = config;
            _store = store;
        }

        /// <summary>Records one node's reading for one hour.</summary>
        public void Accept(ReadingFrame frame)
        {
            if (frame == null || string.IsNullOrEmpty(frame.SessionDate)) return;

            lock (_gate) _store.Save(frame);
        }

        public bool Has(string sessionDate, string nodeId, int hour)
        {
            lock (_gate) return _store.Has(sessionDate, hour, nodeId);
        }

        /// <summary>
        /// Every hour of the session as 24-column rows, ready for the QR payload.
        /// A column with no reading is left empty rather than zeroed — the phone
        /// must be able to tell "not recorded" from "zero".
        /// </summary>
        public Dictionary<int, string[]> BuildRows(string sessionDate, out int hoursHeld)
        {
            var rows = new Dictionary<int, string[]>();

            lock (_gate)
            {
                // Group every stored frame by hour, then by the node that sent it.
                var byHour = new Dictionary<int, Dictionary<string, ReadingFrame>>();

                foreach (ReadingFrame frame in _store.LoadDay(sessionDate))
                {
                    Dictionary<string, ReadingFrame> nodes;
                    if (!byHour.TryGetValue(frame.Hour, out nodes))
                    {
                        nodes = new Dictionary<string, ReadingFrame>(StringComparer.OrdinalIgnoreCase);
                        byHour[frame.Hour] = nodes;
                    }
                    nodes[frame.ServerId] = frame;
                }

                foreach (var pair in byHour)
                {
                    var values = new string[LogsheetQr.ColumnCount];
                    bool any = false;

                    for (int column = 1; column <= LogsheetQr.ColumnCount; column++)
                    {
                        ColumnBinding binding = _config.Columns.For(column);
                        if (binding == null) { values[column - 1] = ""; continue; }

                        ReadingFrame frame;
                        if (!pair.Value.TryGetValue(binding.ServerId, out frame)) { values[column - 1] = ""; continue; }

                        ChannelReading reading = frame.Find(binding.Channel);
                        if (reading == null || !reading.Ok) { values[column - 1] = ""; continue; }

                        values[column - 1] = Trim(reading.Value);
                        any = true;
                    }

                    if (any) rows[pair.Key] = values;
                }
            }

            hoursHeld = rows.Count;
            return rows;
        }

        /// <summary>
        /// Why <see cref="BuildRows"/> came back empty, in one line the operator
        /// standing at the screen can act on. There are only three ways to get
        /// here and they need three different people: nobody has read an hour
        /// yet, every read threw, or the reads worked and recognised nothing —
        /// which is a calibration problem, not a broken node.
        /// </summary>
        public string DiagnoseEmpty(string sessionDate)
        {
            int frames = 0, failed = 0;
            string lastError = "";

            lock (_gate)
            {
                foreach (ReadingFrame frame in _store.LoadDay(sessionDate))
                {
                    frames++;
                    if (string.IsNullOrEmpty(frame.Error)) continue;

                    failed++;
                    lastError = frame.Error;
                }
            }

            if (frames == 0)
                return "No hour has been read yet. Readings are taken at " +
                       _config.CaptureMinute.ToString("00", CultureInfo.InvariantCulture) +
                       " minutes past each hour.";

            if (failed == frames)
                return string.Format(CultureInfo.InvariantCulture,
                    "All {0} attempted reading(s) failed: {1}", frames, lastError);

            return string.Format(CultureInfo.InvariantCulture,
                "{0} hour(s) were read but no value was recognised — check the ROI calibration " +
                "against the wall view.", frames);
        }

        /// <summary>
        /// The LS1 cell grammar carries integers only — the phone's parser keeps
        /// just the digits of each cell — so the fractional part is dropped and
        /// the sign left off here, the same shape the printed logsheet used.
        /// </summary>
        private static string Trim(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            string text = value.Trim();
            if (text.StartsWith("-", StringComparison.Ordinal)) text = text.Substring(1);

            int point = text.IndexOf('.');
            if (point >= 0) text = text.Substring(0, point);

            return text.Length == 0 ? "0" : text;
        }

        public string Describe(string sessionDate)
        {
            int held;
            BuildRows(sessionDate, out held);
            return string.Format(CultureInfo.InvariantCulture, "{0}: {1} hour(s) gathered", sessionDate, held);
        }
    }
}
