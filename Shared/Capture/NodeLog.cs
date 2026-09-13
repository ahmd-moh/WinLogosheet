using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Substation.Capture
{
    /// <summary>
    /// Day-stamped text log. The nodes run hidden and unattended on machines
    /// nobody logs into, so every capture, connection and failure is written
    /// down and kept for the retention window.
    /// </summary>
    public sealed class NodeLog
    {
        private readonly string _folder;
        private readonly object _gate = new object();

        public event Action<string> LineWritten;

        public NodeLog(string folder)
        {
            _folder = folder;
            Directory.CreateDirectory(_folder);
        }

        public void Info(string message) { Write("INFO ", message); }
        public void Warn(string message) { Write("WARN ", message); }
        public void Error(string message) { Write("ERROR", message); }

        private void Write(string level, string message)
        {
            string line = string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH:mm:ss} {1} {2}",
                                        DateTime.Now, level, message);
            lock (_gate)
            {
                try
                {
                    string path = Path.Combine(_folder,
                        "node-" + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".log");
                    File.AppendAllText(path, line + Environment.NewLine, new UTF8Encoding(false));
                }
                catch
                {
                    // A full or read-only disk must not take the node down; the
                    // reading itself still reaches the store.
                }
            }

            Action<string> handler = LineWritten;
            if (handler != null) handler(line);
        }

        public void Prune(int retentionDays)
        {
            if (retentionDays <= 0) return;
            DateTime cutoff = DateTime.Now.Date.AddDays(-retentionDays);

            foreach (string file in Directory.GetFiles(_folder, "node-*.log"))
            {
                try
                {
                    if (File.GetLastWriteTime(file).Date < cutoff) File.Delete(file);
                }
                catch { }
            }
        }
    }
}
