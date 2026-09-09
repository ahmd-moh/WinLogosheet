using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Substation.Capture;
using Substation.Qr;
using Substation.Shared;

namespace SubstationOcrServer
{
    /// <summary>
    /// The 132 kV node. Reads its own secondary screen every hour, receives the
    /// 33 kV client's values over TCP, and flashes the gathered session as a QR
    /// code on the main screen when the operator presses Ctrl+Shift+7+8+9.
    ///
    /// It has no window of its own: the QR flash is the only thing it ever puts
    /// on screen.
    /// </summary>
    internal static class Program
    {
        public const string Version = "2.1.0";
        public const string Product = "Substation OCR Server (132 kV)";

        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string configPath = ArgValue(args, "--config",
                Path.Combine(Application.StartupPath, "server.config.json"));

            ServerConfig config;
            try
            {
                config = ServerConfig.Load(configPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not read " + configPath + ":\n\n" + ex.Message,
                                Product, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool isFirst;
            using (var single = new Mutex(true, @"Global\SubstationOcrServer_" + config.NodeId, out isFirst))
            {
                if (!isFirst) return;
                Run(config, configPath, args);
            }
        }

        private static void Run(ServerConfig config, string configPath, string[] args)
        {
            var log = new NodeLog(config.ResolvePath(config.LogFolder));
            log.Info("=== " + Product + " " + Version + " starting on " + Environment.MachineName +
                     " as '" + config.NodeId + "' ===");
            log.Info("Reading the " + ScreenGrabber.Describe(config.CaptureScreen) + " screen.");

            var store = new HourStore(config.ResolvePath(config.DataFolder));
            var merged = new MergedStore(config, store);

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
                "SubstationOcrServer",
                config.RunAtLogon,
                LogonAutostart.BuildCommand(Application.ExecutablePath, configPath,
                                            Path.Combine(Application.StartupPath, "server.config.json")),
                log);

            var scheduler = new HourlyScheduler(config, capture, store, log);
            var server = new ReadingServer(config, merged, store, capture, log);

            try
            {
                server.Start();
            }
            catch (Exception ex)
            {
                log.Error("Could not listen on port " + config.Port + ": " + ex.Message +
                          " — another program may hold the port, or the firewall rule is missing.");
                capture.Dispose();
                return;
            }

            // WM_HOTKEY needs a window to be delivered to; this one is created
            // but never shown. Registering happens on handle creation, so the
            // hotkeys survive Windows recreating the handle.
            var hotkey = new HotkeySink();
            hotkey.Trace += log.Info;
            hotkey.Completed += () => ToggleQr(config, merged, scheduler, log);

            scheduler.ReadingTaken += frame => merged.Accept(frame);
            scheduler.SessionEnded += () =>
            {
                if (!config.ExitWhenSessionEnds) return;
                log.Info("exitWhenSessionEnds is set — quitting.");
                Application.Exit();
            };
            scheduler.Start();

            var host = new HiddenHost(config, log, Product);
            host.BuildingMenu += menu =>
            {
                menu.Items.Add("Show QR code now", null,
                    (s, e) => ToggleQr(config, merged, scheduler, log));
                menu.Items.Add("Read this hour now", null, (s, e) => scheduler.CaptureNow());
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
                HideQr("shutdown", log);
                hotkey.Dispose();
                scheduler.Dispose();
                server.Dispose();
                capture.Dispose();
            };

            // Force the handle so the hotkeys register now, without the sink
            // ever being shown.
            IntPtr handle = hotkey.Handle;
            log.Info("Hotkey armed on handle " + handle.ToInt64().ToString("X") +
                     ": hold Ctrl+Shift, then press 7, 8, 9 within three seconds each.");
            log.Info("QR will be shown on the " + config.QrScreen + " screen for " +
                     config.QrSeconds + " s.");

            host.Start();
            Application.Run(host);
        }

        private static QrFlashWindow _flash;
        private static readonly object _flashGate = new object();

        /// <summary>
        /// Hotkey behaviour: showing again while the code is up takes it down,
        /// the way the previous version worked.
        /// </summary>
        private static void ToggleQr(ServerConfig config, MergedStore merged,
                                     HourlyScheduler scheduler, NodeLog log)
        {
            lock (_flashGate)
            {
                if (_flash != null && !_flash.IsDisposed)
                {
                    HideQr("hotkey", log);
                    return;
                }

                ShowQr(config, merged, scheduler, log);
            }
        }

        private static void HideQr(string reason, NodeLog log)
        {
            QrFlashWindow window = _flash;
            if (window == null || window.IsDisposed) { _flash = null; return; }

            _flash = null;
            try
            {
                window.HideReason = reason;
                window.Close();
            }
            catch (Exception ex)
            {
                log.Warn("Could not close the QR window: " + ex.Message);
            }
        }

        /// <summary>Builds the LS1 payload for the pinned session and shows it.</summary>
        private static void ShowQr(ServerConfig config, MergedStore merged,
                                   HourlyScheduler scheduler, NodeLog log)
        {
            try
            {
                DateTime sessionDate = scheduler.SessionDate;

                int hoursHeld;
                Dictionary<int, string[]> rows = merged.BuildRows(scheduler.SessionDateString, out hoursHeld);

                if (hoursHeld == 0)
                {
                    log.Warn("QR requested but nothing has been gathered yet for " +
                             scheduler.SessionDateString + ".");
                    return;
                }

                string payload = LogsheetQr.BuildPayload(sessionDate, SessionClock.Sequence, rows);

                QrEcc ecc;
                QrCode code = LogsheetQr.Encode(payload, out ecc);

                // Render one module per pixel and let the window scale it up by a
                // whole factor, which keeps every module square and crisp.
                Bitmap bitmap = code.ToBitmap(1, 4);

                string caption = string.Format(CultureInfo.InvariantCulture, "{0}   {1}   —   {2} hour(s)",
                    config.SubstationCode, scheduler.SessionDateString, hoursHeld);

                var window = new QrFlashWindow(bitmap, caption, config.QrSeconds, config.QrScreen);
                window.FormClosed += (s, e) =>
                {
                    var closed = (QrFlashWindow)s;
                    lock (_flashGate) if (ReferenceEquals(_flash, closed)) _flash = null;
                    log.Info("QR hidden (" + closed.HideReason + ").");
                };

                _flash = window;
                window.Show();
                window.Activate();

                log.Info(string.Format(CultureInfo.InvariantCulture,
                    "QR shown: {0} hour(s), {1} chars, {2} mode, version {3}, ECC {4}.",
                    hoursHeld, payload.Length, code.Mode, code.Version, ecc));
            }
            catch (Exception ex)
            {
                log.Error("Could not build the QR code: " + ex.Message);
            }
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
