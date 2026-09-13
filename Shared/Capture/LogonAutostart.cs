using System;
using Microsoft.Win32;

namespace Substation.Capture
{
    /// <summary>
    /// Optional "start when this account logs on", via the per-user Run key.
    ///
    /// Per-user rather than machine-wide on purpose: no administrator rights are
    /// needed, and the node lands in the interactive session, which is required
    /// both to capture the screen and to own a global hotkey. A Windows service
    /// can do neither.
    ///
    /// This covers a reboot or a logoff. It does NOT restart the daily session:
    /// a node that reaches 07:00 goes quiet and stays quiet, and on a machine
    /// that is never logged out the entry never fires again. Starting tomorrow's
    /// session is still a manual act.
    ///
    /// The setting is authoritative in both directions — turning it off removes
    /// the entry rather than leaving a stale one behind — and the registered
    /// command is re-checked every start, so a rebuilt or moved executable heals
    /// itself.
    /// </summary>
    public static class LogonAutostart
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static void Apply(string valueName, bool enabled, string command, NodeLog log)
        {
            try
            {
                using (RegistryKey run = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (run == null)
                    {
                        log.Warn("Autostart: could not open " + RunKeyPath + ".");
                        return;
                    }

                    string existing = run.GetValue(valueName) as string;

                    if (!enabled)
                    {
                        if (existing == null) return;

                        run.DeleteValue(valueName, false);
                        log.Info("Autostart removed (runAtLogon is off).");
                        return;
                    }

                    if (string.Equals(existing, command, StringComparison.OrdinalIgnoreCase)) return;

                    run.SetValue(valueName, command);
                    log.Info((existing == null ? "Autostart registered: " : "Autostart updated: ") + command);
                }
            }
            catch (Exception ex)
            {
                // A locked-down profile must not stop the node from running.
                log.Warn("Autostart could not be applied: " + ex.Message);
            }
        }

        /// <summary>
        /// The command to register: the executable, plus the --config argument
        /// when one was given, so a node started against a non-default config
        /// comes back the same way.
        /// </summary>
        public static string BuildCommand(string executablePath, string configPath, string defaultConfigPath)
        {
            string command = Quote(executablePath);

            if (!string.Equals(configPath, defaultConfigPath, StringComparison.OrdinalIgnoreCase))
                command += " --config " + Quote(configPath);

            return command;
        }

        private static string Quote(string value)
        {
            return "\"" + value + "\"";
        }
    }
}
