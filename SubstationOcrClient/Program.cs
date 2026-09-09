using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Substation.Capture;
using Substation.Shared;

namespace SubstationOcrClient
{
    /// <summary>
    /// The 33 kV node. Reads its own secondary screen every hour and pushes the
    /// values to the 132 kV server node. It has no window: everything it does
    /// goes to the day's log file.
    /// </summary>
    internal static class Program
    {
        public const string Version = "2.1.0";
        public const string Product = "Substation OCR Client (33 kV)";

        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string configPath = ArgValue(args, "--config",
                Path.Combine(Application.StartupPath, "client.config.json"));

            ClientConfig config;
            try
            {
                config = ClientConfig.Load(configPath);
            }
            catch (Exception ex)
            {
                // Nothing is running yet, so there is no log to write to.
                MessageBox.Show("Could not read " + configPath + ":\n\n" + ex.Message,
                                Product, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool isFirst;
            using (var single = new Mutex(true, @"Global\SubstationOcrClient_" + config.NodeId, out isFirst))
            {
                if (!isFirst) return; // a second instance would double every reading
                Run(config, configPath, args);
            }
        }

        private static void Run(ClientConfig config, string configPath, string[] args)
        {
            var log = new NodeLog(config.ResolvePath(config.LogFolder));
            log.Info("=== " + Product + " " + Version + " starting on " + Environment.MachineName +
                     " as '" + config.NodeId + "' ===");
            log.Info("Reading the " + ScreenGrabber.Describe(config.CaptureScreen) + " screen.");

            var store = new HourStore(config.ResolvePath(config.DataFolder));

            CaptureService capture;
            try
            {
                capture = new CaptureService(config, store, log, Version);
            }
            catch (Exception ex)
            {
                log.Error("Start-up failed: " + ex);
                return;
            }

            if (HasFlag(args, "--capture-once"))
            {
                DateTime now = DateTime.Now;
                capture.CaptureNow(SessionClock.SessionDateStringOf(now), now.Hour);
                capture.Dispose();
                return;
            }

            LogonAutostart.Apply(
                "SubstationOcrClient",
                config.RunAtLogon,
                LogonAutostart.BuildCommand(Application.ExecutablePath, configPath,
                                            Path.Combine(Application.StartupPath, "client.config.json")),
                log);

            var scheduler = new HourlyScheduler(config, capture, store, log);
            var uplink = new ReadingUplink(config, store, log);

            scheduler.ReadingTaken += frame => uplink.Send(frame);
            scheduler.SessionEnded += () =>
            {
                // Deliver whatever is still queued before going quiet.
                uplink.Drain();
                if (config.ExitWhenSessionEnds)
                {
                    log.Info("exitWhenSessionEnds is set — quitting.");
                    Application.Exit();
                }
            };

            uplink.Start(scheduler.SessionDateString);
            scheduler.Start();

            var host = new HiddenHost(config, log, Product);
            host.BuildingMenu += menu =>
            {
                menu.Items.Add("Read this hour now", null, (s, e) =>
                {
                    ReadingFrame frame = scheduler.CaptureNow();
                    host.Notify(Product, string.IsNullOrEmpty(frame.Error)
                        ? CaptureService.CountOk(frame) + " of " + frame.Channels.Count + " value(s) read"
                        : frame.Error, !string.IsNullOrEmpty(frame.Error));
                });
                menu.Items.Add("Send queued hours now", null, (s, e) => uplink.Drain());
                menu.Items.Add("Write calibration overlay", null, (s, e) =>
                {
                    try { capture.WriteCalibrationOverlay(); }
                    catch (Exception ex) { log.Error(ex.Message); }
                });
                menu.Items.Add("Reload ROI configuration", null, (s, e) => capture.ReloadRois());
            };

            Application.ApplicationExit += (s, e) =>
            {
                log.Info("Shutting down.");
                scheduler.Dispose();
                uplink.Dispose();
                capture.Dispose();
            };

            host.Start();
            Application.Run(host);
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
