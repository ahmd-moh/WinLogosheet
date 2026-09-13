using System;
using System.Collections.Generic;
using System.Globalization;
using Substation.Capture;
using Substation.Qr;
using Substation.Shared;

namespace SubstationOcr
{
    /// <summary>
    /// This PC's readings for the session, folded into the 24 columns the QR
    /// payload carries.
    ///
    /// Only the columns bound to this node's id are filled. The other PC's
    /// columns stay empty on purpose: there is no link between the two PCs, so
    /// each shows its own code and the phone merges the two scans of the same
    /// date into one sheet (docs/QR-ANDROID.md).
    /// </summary>
    public sealed class SessionSheet
    {
        private readonly NodeConfig _config;
        private readonly HourStore _store;

        public SessionSheet(NodeConfig config, HourStore store)
        {
            _config = config;
            _store = store;
        }

        /// <summary>How many of the 24 columns this PC fills.</summary>
        public int OwnColumnCount
        {
            get
            {
                int n = 0;
                for (int column = 1; column <= LogsheetQr.ColumnCount; column++)
                    if (OwnBinding(column) != null) n++;
                return n;
            }
        }

        /// <summary>
        /// Every hour of the session as 24-column rows, ready for the QR payload.
        /// A column with no reading is left empty rather than zeroed — the phone
        /// must be able to tell "not recorded" from "zero", and an empty cell is
        /// also what lets the other PC's scan fill it in.
        /// </summary>
        public Dictionary<int, string[]> BuildRows(string sessionDate, out int hoursHeld)
        {
            var rows = new Dictionary<int, string[]>();

            foreach (ReadingFrame frame in _store.LoadDay(sessionDate, _config.NodeId))
            {
                var values = new string[LogsheetQr.ColumnCount];
                bool any = false;

                for (int column = 1; column <= LogsheetQr.ColumnCount; column++)
                {
                    values[column - 1] = "";

                    ColumnBinding binding = OwnBinding(column);
                    if (binding == null) continue;

                    ChannelReading reading = frame.Find(binding.Channel);
                    if (reading == null || !reading.Ok) continue;

                    values[column - 1] = Trim(reading.Value);
                    any = true;
                }

                if (any) rows[frame.Hour] = values;
            }

            hoursHeld = rows.Count;
            return rows;
        }

        /// <summary>
        /// Why <see cref="BuildRows"/> came back empty, in one line the operator
        /// standing at the screen can act on. There are only four ways to get
        /// here and they need different people: the config gives this PC no
        /// columns, nobody has read an hour yet, every read threw, or the reads
        /// worked and recognised nothing — which is a calibration problem, not a
        /// broken node.
        /// </summary>
        public string DiagnoseEmpty(string sessionDate)
        {
            if (OwnColumnCount == 0)
                return "No column in " + NodeConfig.FileName + " has \"source\": \"" + _config.NodeId +
                       "\", so this PC has nothing to put in the code.";

            int frames = 0, failed = 0;
            string lastError = "";

            foreach (ReadingFrame frame in _store.LoadDay(sessionDate, _config.NodeId))
            {
                frames++;
                if (string.IsNullOrEmpty(frame.Error)) continue;

                failed++;
                lastError = frame.Error;
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

        private ColumnBinding OwnBinding(int column)
        {
            ColumnBinding binding = _config.Columns.For(column);
            if (binding == null) return null;

            return string.Equals(binding.ServerId, _config.NodeId, StringComparison.OrdinalIgnoreCase)
                ? binding
                : null;
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
    }
}
