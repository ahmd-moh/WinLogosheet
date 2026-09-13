using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Win32;

namespace Substation.Capture
{
    /// <summary>
    /// What a node found on the machine it started on, for the start-up lines of
    /// its log.
    ///
    /// The substation servers are not the build machine. They run .NET
    /// Framework 4.7 rather than 4.8, and whether they carry the Visual C++
    /// runtime that Tesseract's native half is linked against is anybody's
    /// guess. When a node misbehaves there, its log is the only witness, so it
    /// says up front which framework, which Windows and which runtime it got.
    /// </summary>
    public static class NodeEnvironment
    {
        /// <summary>
        /// The oldest Visual C++ runtime the bundled native DLLs accept:
        /// leptonica-1.82.0.dll was linked with the 14.33 toolset, and a runtime
        /// older than the toolset is not guaranteed to load what it built.
        /// </summary>
        public static readonly Version MinimumVcRuntime = new Version(14, 33);

        /// <summary>One line: framework, Windows, bitness, Visual C++ runtime.</summary>
        public static string Describe()
        {
            Version vc = VcRuntimeVersion();

            return Framework() + ", " + Windows() + ", " + Bitness() + " process, " +
                   (vc == null ? "no Visual C++ runtime found" : "Visual C++ runtime " + vc);
        }

        /// <summary>
        /// Why Tesseract's native DLLs cannot load here, as far as the Visual C++
        /// runtime explains it, or null when the runtime looks fine. Only a hint:
        /// it is consulted after loading has already failed, never instead of
        /// trying, because a DLL on the PATH can satisfy what this does not see.
        /// </summary>
        public static string VcRuntimeProblem()
        {
            string package = "the Microsoft Visual C++ 2015-2022 Redistributable (" + Bitness() + ")";

            var missing = new List<string>();
            foreach (string dll in VcRuntimeFiles())
                if (Locate(dll) == null) missing.Add(dll);

            if (missing.Count > 0)
                return "This machine has no " + string.Join(", ", missing.ToArray()) +
                       " — install " + package + ".";

            Version found = VcRuntimeVersion();
            if (found != null && found < MinimumVcRuntime)
                return "The Visual C++ runtime here is " + found + " and Tesseract needs " +
                       MinimumVcRuntime + " or later — install the current " + package + ".";

            return null;
        }

        /// <summary>
        /// The installed .NET Framework, from the release number setup writes.
        /// Environment.Version cannot tell 4.7 from 4.8: both say CLR 4.0.30319.
        /// </summary>
        private static string Framework()
        {
            int release = 0;
            try
            {
                using (RegistryKey key = OpenMachineKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                    if (key != null)
                        release = Convert.ToInt32(key.GetValue("Release", 0), CultureInfo.InvariantCulture);
            }
            catch
            {
                // An unreadable key is reported as unknown; it is no reason to stop.
            }

            // Lowest release number of each version, as Microsoft documents them.
            string name = release >= 533320 ? "4.8.1"
                        : release >= 528040 ? "4.8"
                        : release >= 461808 ? "4.7.2"
                        : release >= 461308 ? "4.7.1"
                        : release >= 460798 ? "4.7"
                        : "unknown";

            return ".NET Framework " + name + " (release " + release + ")";
        }

        private static string Windows()
        {
            try
            {
                using (RegistryKey key = OpenMachineKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        string product = Convert.ToString(key.GetValue("ProductName", "Windows"), CultureInfo.InvariantCulture);
                        string release = Convert.ToString(key.GetValue("DisplayVersion", key.GetValue("ReleaseId", "")),
                                                          CultureInfo.InvariantCulture);
                        string build = Convert.ToString(key.GetValue("CurrentBuild", "?"), CultureInfo.InvariantCulture);

                        // Windows 11 still names itself "Windows 10" here; only
                        // the build number tells the two apart.
                        int number;
                        if (product.StartsWith("Windows 10", StringComparison.OrdinalIgnoreCase) &&
                            int.TryParse(build, NumberStyles.Integer, CultureInfo.InvariantCulture, out number) &&
                            number >= 22000)
                            product = "Windows 11" + product.Substring("Windows 10".Length);

                        return product + (release.Length == 0 ? "" : " " + release) + " (build " + build + ")";
                    }
                }
            }
            catch
            {
                // Fall through to what the framework reports, which without an
                // OS-compatibility manifest understates Windows 10 as 6.2.
            }

            return Environment.OSVersion.VersionString;
        }

        /// <summary>
        /// Opens a machine key in the 64-bit view. A 32-bit process is otherwise
        /// shown WOW6432Node's copy, which Windows does not keep current — after
        /// an edition change it still names the old edition. On 32-bit Windows
        /// the 64-bit view simply is the 32-bit one.
        /// </summary>
        private static RegistryKey OpenMachineKey(string path)
        {
            using (RegistryKey root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                return root.OpenSubKey(path);
        }

        /// <summary>The native DLL folder the Tesseract wrapper loads from.</summary>
        private static string Bitness()
        {
            return Environment.Is64BitProcess ? "x64" : "x86";
        }

        /// <summary>What tesseract50.dll and leptonica-1.82.0.dll import from the
        /// runtime. vcruntime140_1.dll exists only in the 64-bit runtime.</summary>
        private static string[] VcRuntimeFiles()
        {
            return Environment.Is64BitProcess
                ? new[] { "vcruntime140.dll", "vcruntime140_1.dll", "msvcp140.dll" }
                : new[] { "vcruntime140.dll", "msvcp140.dll" };
        }

        private static Version VcRuntimeVersion()
        {
            string path = Locate("vcruntime140.dll");
            if (path == null) return null;

            try
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);
                return new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Where Windows would find a runtime DLL for this process: beside the
        /// executable, or in the system folder — which for a 32-bit process on
        /// 64-bit Windows is redirected to SysWOW64, exactly as the loader sees it.
        /// </summary>
        private static string Locate(string dll)
        {
            foreach (string folder in new[] { AppDomain.CurrentDomain.BaseDirectory, Environment.SystemDirectory })
            {
                try
                {
                    string path = Path.Combine(folder, dll);
                    if (File.Exists(path)) return path;
                }
                catch (ArgumentException)
                {
                    // An unusable folder is simply not a place the DLL is.
                }
            }
            return null;
        }
    }
}
