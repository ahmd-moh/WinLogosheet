using System;
using System.Collections.Generic;
using System.IO;

namespace Substation.Capture
{
    /// <summary>
    /// Finds the Tesseract language data.
    ///
    /// tessDataPath used to be a single hard-coded per-user install path. That
    /// path is right on the substation servers and wrong everywhere else, and
    /// when it is wrong Tesseract throws "Failed to initialise tesseract engine"
    /// on every capture — an hourly line in the log that nobody is watching,
    /// while the operator in front of the screen sees nothing at all.
    ///
    /// So the configured folder is a preference now, not a requirement: it wins
    /// when it holds the language file, and otherwise the usual install
    /// locations are tried before the node gives up. Whatever happens is said
    /// plainly in the log at start-up, once, where it can be acted on.
    /// </summary>
    public static class TessData
    {
        /// <summary>What Tesseract must find in the folder for language "eng".</summary>
        public const string LanguageFile = "eng.traineddata";

        public static bool Holds(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return false;

            try { return File.Exists(Path.Combine(folder, LanguageFile)); }
            catch (ArgumentException) { return false; }   // an unusable path is simply not a candidate
        }

        /// <summary>
        /// Where to look, best guess first: what the config asked for, the same
        /// path with \tessdata appended (pointing at the install root instead of
        /// the data folder is the usual slip), TESSDATA_PREFIX in both of the
        /// meanings Tesseract has given it, a tessdata folder beside the
        /// executable — which makes a node portable onto a machine with no
        /// Tesseract installed — and finally the installers' own locations.
        /// </summary>
        public static List<string> Candidates(string configured)
        {
            var paths = new List<string>();

            Add(paths, configured);
            Add(paths, Combine(configured, "tessdata"));

            string prefix = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
            Add(paths, prefix);
            Add(paths, Combine(prefix, "tessdata"));

            Add(paths, Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"));

            foreach (string root in InstallRoots())
                Add(paths, Combine(root, Path.Combine("Tesseract-OCR", "tessdata")));

            return paths;
        }

        /// <summary>
        /// The folder to hand Tesseract, or null when none of the candidates
        /// holds the language file. Reports what it did either way.
        ///
        /// Returning null rather than throwing is deliberate: the node still
        /// starts, so the socket link, the stored hours and the QR hotkey all
        /// keep working on whatever was gathered before OCR broke.
        /// </summary>
        public static string Resolve(string configured, NodeLog log)
        {
            List<string> candidates = Candidates(configured);

            foreach (string path in candidates)
            {
                if (!Holds(path)) continue;

                if (Same(path, configured))
                    log.Info("Tesseract language data: " + path);
                else
                    log.Warn("tessDataPath '" + configured + "' does not hold " + LanguageFile +
                             " — using " + path + " instead. Set tessDataPath to that to silence this.");

                return path;
            }

            log.Error("No Tesseract language data found — every hourly reading will fail and the QR " +
                      "code will stay empty. Install Tesseract, or copy a tessdata folder holding " +
                      LanguageFile + " next to the node's .exe. Looked in: " +
                      string.Join("; ", candidates.ToArray()));

            return configured;   // hand it back unchanged so the log and the config still agree
        }

        private static IEnumerable<string> InstallRoots()
        {
            // ProgramW6432 is the real 64-bit Program Files even from a 32-bit
            // process, where SpecialFolder.ProgramFiles quietly means (x86).
            yield return Environment.GetEnvironmentVariable("ProgramW6432");
            yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            yield return Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                 "Programs");
        }

        private static string Combine(string folder, string child)
        {
            if (string.IsNullOrEmpty(folder)) return null;

            try { return Path.Combine(folder, child); }
            catch (ArgumentException) { return null; }
        }

        private static void Add(List<string> paths, string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            foreach (string existing in paths)
                if (Same(existing, path)) return;

            paths.Add(path);
        }

        private static bool Same(string a, string b)
        {
            return !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) &&
                   string.Equals(a.TrimEnd('\\', '/'), b.TrimEnd('\\', '/'),
                                 StringComparison.OrdinalIgnoreCase);
        }
    }
}
