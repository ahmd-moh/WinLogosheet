using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Substation.Shared;

namespace SubstationOcrAgent
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string configPath = ArgValue(args, "--config",
                Path.Combine(Application.StartupPath, "agent.config.json"));

            AgentConfig config;
            try
            {
                config = AgentConfig.Load(configPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not read " + configPath + ":\n\n" + ex.Message,
                                AgentInfo.Product, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // One agent per server. A second instance would fight the first for
            // the port and double every reading.
            bool isFirst;
            using (var single = new Mutex(true, @"Global\SubstationOcrAgent_" + config.ServerId, out isFirst))
            {
                if (!isFirst)
                {
                    MessageBox.Show("The agent for '" + config.ServerId + "' is already running on this server.",
                                    AgentInfo.Product, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Run(config, args);
            }
        }

        private static void Run(AgentConfig config, string[] args)
        {
            var log = new AgentLog(config.ResolvePath(config.LogFolder));
            log.Info("=== " + AgentInfo.Product + " " + AgentInfo.Version + " starting on " +
                     Environment.MachineName + " as '" + config.ServerId + "' ===");

            var store = new HourStore(config.ResolvePath(config.DataFolder));

            CaptureService capture;
            try
            {
                capture = new CaptureService(config, store, log);
            }
            catch (Exception ex)
            {
                log.Error("Start-up failed: " + ex);
                MessageBox.Show("Start-up failed:\n\n" + ex.Message +
                                "\n\nCheck roiConfigPath and tessDataPath in agent.config.json.",
                                AgentInfo.Product, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // --capture-once reads the current hour and exits, for a smoke test
            // during commissioning or a Task Scheduler fallback.
            if (HasFlag(args, "--capture-once"))
            {
                DateTime now = DateTime.Now;
                ReadingFrame frame = capture.CaptureNow(
                    LogsheetHours.SessionDate(now, config.WorkdayStartHour),
                    LogsheetHours.FromClock(now));
                log.Info("--capture-once finished, mean confidence " +
                         frame.MeanConfidence.ToString("0.0", CultureInfo.InvariantCulture) + "%");
                capture.Dispose();
                return;
            }

            var server = new AgentServer(config, capture, store, log);
            try
            {
                server.Start();
            }
            catch (Exception ex)
            {
                log.Error("Could not listen on port " + config.Port + ": " + ex.Message);
                MessageBox.Show("Could not listen on port " + config.Port + ":\n\n" + ex.Message +
                                "\n\nAnother program may be using the port, or the firewall rule is missing.",
                                AgentInfo.Product, MessageBoxButtons.OK, MessageBoxIcon.Error);
                capture.Dispose();
                return;
            }

            var scheduler = new HourlyScheduler(config, capture, store, log);
            scheduler.Start();

            var tray = new TrayHost(config, capture, server, store, log);
            tray.Show();

            Application.ApplicationExit += (s, e) =>
            {
                log.Info("Shutting down.");
                scheduler.Dispose();
                server.Dispose();
                capture.Dispose();
                tray.Dispose();
            };

            Application.Run();
        }

        private static string ArgValue(string[] args, string name, string fallback)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return fallback;
        }

        private static bool HasFlag(string[] args, string name)
        {
            foreach (string a in args)
                if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
