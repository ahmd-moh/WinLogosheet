using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Substation.Shared;

namespace Substation.Capture
{
    /// <summary>
    /// Every reading a node holds, on that node's own disk, as
    /// Data\Readings-yyyy-MM-dd\HH-NODEID.json.
    ///
    /// The node id is part of the file name so two nodes can never overwrite
    /// each other's hours, even when both are run from one folder for testing.
    ///
    /// The store is what makes a restart harmless: a node started again
    /// mid-session picks every hour it already read back up from disk, and the
    /// QR code still carries the whole session.
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

        private string PathFor(string sessionDate, int hour, string nodeId)
        {
            string name = hour.ToString("00", CultureInfo.InvariantCulture) + "-" + Sanitise(nodeId) + ".json";
            return Path.Combine(FolderFor(sessionDate), name);
        }

        /// <summary>Node ids reach the file system, so keep them to safe characters.</summary>
        private static string Sanitise(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return "NODE";

            var sb = new System.Text.StringBuilder(nodeId.Length);
            foreach (char c in nodeId)
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
            return sb.Length == 0 ? "NODE" : sb.ToString();
        }

        public void Save(ReadingFrame frame)
        {
            lock (_gate)
            {
                Directory.CreateDirectory(FolderFor(frame.SessionDate));
                Json.WriteFile(PathFor(frame.SessionDate, frame.Hour, frame.ServerId), frame.ToJson());
            }
        }

        public ReadingFrame Load(string sessionDate, int hour, string nodeId)
        {
            lock (_gate)
            {
                string path = PathFor(sessionDate, hour, nodeId);
                if (!File.Exists(path)) return null;

                try { return ReadingFrame.FromJson(Json.ReadFile(path)); }
                catch { return null; }
            }
        }

        public bool Has(string sessionDate, int hour, string nodeId)
        {
            lock (_gate) return File.Exists(PathFor(sessionDate, hour, nodeId));
        }

        /// <summary>Every frame stored for a session date, from every node.</summary>
        public List<ReadingFrame> LoadDay(string sessionDate)
        {
            var frames = new List<ReadingFrame>();
            string folder = FolderFor(sessionDate);
            if (!Directory.Exists(folder)) return frames;

            lock (_gate)
            {
                foreach (string file in Directory.GetFiles(folder, "*.json"))
                {
                    try { frames.Add(ReadingFrame.FromJson(Json.ReadFile(file))); }
                    catch { /* a half-written file from a power cut is skipped, not fatal */ }
                }
            }

            frames.Sort((a, b) =>
            {
                int order = SessionClock.Order(a.Hour).CompareTo(SessionClock.Order(b.Hour));
                return order != 0 ? order : string.CompareOrdinal(a.ServerId, b.ServerId);
            });
            return frames;
        }

        /// <summary>Frames a single node stored for a session date.</summary>
        public List<ReadingFrame> LoadDay(string sessionDate, string nodeId)
        {
            var mine = new List<ReadingFrame>();
            foreach (ReadingFrame frame in LoadDay(sessionDate))
                if (string.Equals(frame.ServerId, nodeId, StringComparison.OrdinalIgnoreCase)) mine.Add(frame);
            return mine;
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
                if (!SessionClock.TryParse(stamp, out date)) continue;
                if (date >= cutoff) continue;

                try { Directory.Delete(folder, true); removed++; }
                catch { /* open in someone's Explorer window; try again tomorrow */ }
            }
            return removed;
        }
    }
}
