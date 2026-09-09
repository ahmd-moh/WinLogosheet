using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Substation.Shared;

namespace SubstationOcrAgent
{
    /// <summary>
    /// Keeps every hour the agent has read on the agent's own disk, as
    /// Data\Readings-yyyy-MM-dd\HH.json.
    ///
    /// The store is what makes a network outage survivable: the agent goes on
    /// reading its display every hour whether or not WinLogosheet can reach it,
    /// and the collector picks the backlog up with a single history call once
    /// the link returns.
    /// </summary>
    public sealed class HourStore
    {
        private readonly string _root;
        private readonly object _gate = new object();

        public HourStore(string rootFolder)
        {
            _root = rootFolder;
            Directory.CreateDirectory(_root);
        }

        private string FolderFor(string sessionDate)
        {
            return Path.Combine(_root, "Readings-" + sessionDate);
        }

        private string PathFor(string sessionDate, int hour)
        {
            return Path.Combine(FolderFor(sessionDate), hour.ToString("00", CultureInfo.InvariantCulture) + ".json");
        }

        public void Save(ReadingFrame frame)
        {
            lock (_gate)
            {
                string folder = FolderFor(frame.SessionDate);
                Directory.CreateDirectory(folder);
                Json.WriteFile(PathFor(frame.SessionDate, frame.Hour), frame.ToJson());
            }
        }

        public ReadingFrame Load(string sessionDate, int hour)
        {
            lock (_gate)
            {
                string path = PathFor(sessionDate, hour);
                if (!File.Exists(path)) return null;
                try { return ReadingFrame.FromJson(Json.ReadFile(path)); }
                catch { return null; }
            }
        }

        public bool Has(string sessionDate, int hour)
        {
            return File.Exists(PathFor(sessionDate, hour));
        }

        public List<ReadingFrame> LoadDay(string sessionDate)
        {
            var frames = new List<ReadingFrame>();
            string folder = FolderFor(sessionDate);
            if (!Directory.Exists(folder)) return frames;

            foreach (string file in Directory.GetFiles(folder, "*.json"))
            {
                try { frames.Add(ReadingFrame.FromJson(Json.ReadFile(file))); }
                catch { /* a half-written file from a power cut is skipped, not fatal */ }
            }

            frames.Sort((a, b) => LogsheetHours.Order(a.Hour).CompareTo(LogsheetHours.Order(b.Hour)));
            return frames;
        }

        /// <summary>Drops reading folders older than the retention window.</summary>
        public int Prune(int retentionDays)
        {
            if (retentionDays <= 0) return 0;

            int removed = 0;
            DateTime cutoff = DateTime.Now.Date.AddDays(-retentionDays);

            foreach (string folder in Directory.GetDirectories(_root, "Readings-*"))
            {
                string stamp = Path.GetFileName(folder).Substring("Readings-".Length);
                DateTime date;
                if (!DateTime.TryParseExact(stamp, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                            DateTimeStyles.None, out date)) continue;
                if (date >= cutoff) continue;

                try { Directory.Delete(folder, true); removed++; }
                catch { /* in use by an operator's Explorer window; try again tomorrow */ }
            }
            return removed;
        }
    }

    /// <summary>
    /// The logsheet day runs 08:00 through 07:00 the next morning, so the hour
    /// number alone does not order the sheet and the calendar date alone does
    /// not identify the session. Both applications share these rules.
    /// </summary>
    public static class LogsheetHours
    {
        /// <summary>Hours in logsheet order: 8..24 then 1..7.</summary>
        public static readonly int[] Sequence =
        {
            8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
            1, 2, 3, 4, 5, 6, 7
        };

        /// <summary>Position in the sheet, or -1 for an hour outside the day.</summary>
        public static int Order(int hour)
        {
            for (int i = 0; i < Sequence.Length; i++)
                if (Sequence[i] == hour) return i;
            return -1;
        }

        /// <summary>
        /// Logsheet hour for a wall-clock time. Midnight is logged as hour 24,
        /// matching the sheet's last row of the evening rather than a "0".
        /// </summary>
        public static int FromClock(DateTime now)
        {
            return now.Hour == 0 ? 24 : now.Hour;
        }

        /// <summary>
        /// Session date for a wall-clock time: anything before the workday start
        /// belongs to the sheet that opened the previous morning.
        /// </summary>
        public static string SessionDate(DateTime now, int workdayStartHour)
        {
            DateTime date = now.Hour < workdayStartHour ? now.Date.AddDays(-1) : now.Date;
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
