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
            log.Info("Running on " + NodeEnvironment.Describe() + ".");
            log.Info("Reading screen: " + ScreenGrabber.Describe(config.CaptureScreen) + ".");

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

            if (HasFlag(args, "--calibrate"))
            {
                // Writes the overlay and quits, for commissioning from a command
                // line instead of the tray.
                try { log.Info("Calibration written to " + capture.WriteCalibrationOverlay()); }
                catch (Exception ex) { log.Error("Calibration failed: " + ex.Message); }
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

            var host = new HiddenHost(config, log, Product);

            // The 132 kV seat cannot reach in here — this node is the one that
            // opens sockets — so a calibration it asks for arrives as a command
            // collected on the next poll, and is answered by sending the shot
            // back up the same link.
            uplink.CommandReceived += command =>
            {
                if (!string.Equals(command.Command, AgentProtocol.CmdCalibrate,
                                   StringComparison.OrdinalIgnoreCase))
                {
                    log.Warn("Ignoring '" + command.Command + "': this node has no such command.");
                    return;
                }

                CalibrationShot shot = capture.Calibrate(true, command.MaxWidth);

                // The node keeps its own copy whatever happens to the link, so
                // an engineer standing here later sees the same picture.
                if (shot.Image != null)
                {
                    try { log.Info("Calibration overlay written to " + shot.SaveTo(config.ResolvePath("Calibration"))); }
                    catch (Exception ex) { log.Warn("Could not keep a local copy: " + ex.Message); }
                }

                uplink.SendCalibration(shot, command.Id);
                host.Notify(Product, "Calibration sent to the 132 kV node: " + shot.Summary(),
                            !string.IsNullOrEmpty(shot.Error));
            };

            uplink.Start(scheduler.SessionDateString);
            scheduler.Start();

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
                // Off the UI thread: this reads the boxes and then talks to the
                // server, and a link that is down would otherwise hold the tray
                // menu open for the whole socket timeout.
                menu.Items.Add("Send calibration to the 132 kV node", null, (s, e) =>
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        CalibrationShot shot = capture.Calibrate(true, 0);
                        uplink.SendCalibration(shot, "manual");
                        host.Notify(Product, shot.Summary(), !string.IsNullOrEmpty(shot.Error));
                    }));
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
