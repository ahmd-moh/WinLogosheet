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

namespace SubstationOcr
{
    /// <summary>
    /// One substation PC's node. Reads this PC's wall view every hour and
    /// flashes this PC's part of the session as a QR code on the main screen
    /// when the operator presses Ctrl+Shift+7+8+9.
    ///
    /// The same program runs on the 132 kV and the 33 kV PC; node.config.json
    /// says which one it is. The two PCs never talk to each other — neither
    /// accepts a connection from the other, and nobody on site has the rights to
    /// change that — so each shows its own code and the phone puts the two halves
    /// of the logsheet together.
    ///
    /// It has no window of its own: the QR flash is the only thing it ever puts
    /// on screen.
    /// </summary>
    internal static class Program
    {
        public const string Version = "3.0.0";
        public const string Product = "Substation OCR";

        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string configPath = ArgValue(args, "--config",
                Path.Combine(Application.StartupPath, NodeConfig.FileName));

            NodeConfig config;
            try
            {
                config = NodeConfig.Load(configPath);
            }
            catch (Exception ex)
            {
                // Nothing is running yet, so there is no log to write to.
                MessageBox.Show("Could not read " + configPath + ":\n\n" + ex.Message,
                                Product, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool isFirst;
            using (var single = new Mutex(true, @"Global\SubstationOcr_" + config.NodeId, out isFirst))
            {
                if (!isFirst) return; // a second instance would double every reading
                Run(config, configPath, args);
            }
        }

        private static void Run(NodeConfig config, string configPath, string[] args)
        {
            var log = new NodeLog(config.ResolvePath(config.LogFolder));
            log.Info("=== " + Product + " " + Version + " starting on " + Environment.MachineName +
                     " as '" + config.NodeId + "' ===");
            log.Info("Running on " + NodeEnvironment.Describe() + ".");
            log.Info("Reading screen: " + ScreenGrabber.Describe(config.CaptureScreen) + ".");

            var store = new HourStore(config.ResolvePath(config.DataFolder));
            var sheet = new SessionSheet(config, store);

            int ownColumns = sheet.OwnColumnCount;
            if (ownColumns == 0)
                log.Warn("No column in " + Path.GetFileName(configPath) + " has \"source\": \"" + config.NodeId +
                         "\". This PC will read its screen, but its QR code will be empty.");
            else
                log.Info("This PC fills " + ownColumns + " of the " + LogsheetQr.ColumnCount + " QR columns.");

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
                "SubstationOcr",
                config.RunAtLogon,
                LogonAutostart.BuildCommand(Application.ExecutablePath, configPath,
                                            Path.Combine(Application.StartupPath, NodeConfig.FileName)),
                log);

            var scheduler = new HourlyScheduler(config, capture, store, log);

            // WM_HOTKEY needs a window to be delivered to; this one is created
            // but never shown. Registering happens on handle creation, so the
            // hotkeys survive Windows recreating the handle.
            var hotkey = new HotkeySink();
            hotkey.Trace += log.Info;
            hotkey.Completed += () => ToggleQr(config, sheet, scheduler, log);

            scheduler.SessionEnded += () =>
            {
                if (!config.ExitWhenSessionEnds) return;
                log.Info("exitWhenSessionEnds is set — quitting.");
                Application.Exit();
            };
            scheduler.Start();

            string product = Product + " (" + config.NodeId + ")";
            var host = new HiddenHost(config, log, product);

            host.BuildingMenu += menu =>
            {
                menu.Items.Add("Show QR code now", null,
                    (s, e) => ToggleQr(config, sheet, scheduler, log));
                menu.Items.Add("Read this hour now", null, (s, e) =>
                {
                    ReadingFrame frame = scheduler.CaptureNow();
                    host.Notify(product, string.IsNullOrEmpty(frame.Error)
                        ? CaptureService.CountOk(frame) + " of " + frame.Channels.Count + " value(s) read"
                        : frame.Error, !string.IsNullOrEmpty(frame.Error));
                });
                menu.Items.Add("Check this PC's boxes (" + config.NodeId + ")", null,
                    (s, e) => ShowCalibration(config, capture, host, log));
                menu.Items.Add("Open calibration folder", null, (s, e) => OpenCalibrationFolder(config, log));
                menu.Items.Add("Reload ROI configuration", null, (s, e) => capture.ReloadRois());
            };

            Application.ApplicationExit += (s, e) =>
            {
                log.Info("Shutting down.");
                HideQr("shutdown", log);
                hotkey.Dispose();
                scheduler.Dispose();
                capture.Dispose();
            };

            // Force the handle so the hotkeys register now, without the sink
            // ever being shown.
            IntPtr handle = hotkey.Handle;
            if (hotkey.Armed)
                log.Info("Hotkey armed on handle " + handle.ToInt64().ToString("X") +
                         ": hold Ctrl+Shift, then press 7, 8, 9 within three seconds each.");
            else
                log.Warn("QR hotkey is NOT armed — another program holds Ctrl+Shift+7/8/9 " +
                         "(the previous WinLogosheet uses the same keys). Close it and restart " +
                         "this node; until then the QR can only be shown from the tray menu " +
                         "(showTrayIcon).");
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
        private static void ToggleQr(NodeConfig config, SessionSheet sheet,
                                     HourlyScheduler scheduler, NodeLog log)
        {
            lock (_flashGate)
            {
                if (_flash != null && !_flash.IsDisposed)
                {
                    HideQr("hotkey", log);
                    return;
                }

                ShowQr(config, sheet, scheduler, log);
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

        /// <summary>
        /// Builds the LS1 payload for the pinned session and shows it.
        ///
        /// Every path out of here puts something on screen. The operator pressed
        /// a key and is watching: an empty screen tells them nothing about which
        /// of the several possible faults they are looking at.
        /// </summary>
        private static void ShowQr(NodeConfig config, SessionSheet sheet,
                                   HourlyScheduler scheduler, NodeLog log)
        {
            try
            {
                DateTime sessionDate = scheduler.SessionDate;

                int hoursHeld;
                Dictionary<int, string[]> rows = sheet.BuildRows(scheduler.SessionDateString, out hoursHeld);

                if (hoursHeld == 0)
                {
                    string why = sheet.DiagnoseEmpty(scheduler.SessionDateString);
                    log.Warn("QR requested but nothing has been gathered yet for " +
                             scheduler.SessionDateString + ". " + why);

                    Present(QrFlashWindow.ForMessage("NO READINGS TO ENCODE",
                                config.NodeId + "   session " + scheduler.SessionDateString + "\n" + why,
                                config.QrSeconds, config.QrScreen), log);
                    return;
                }

                string payload = LogsheetQr.BuildPayload(sessionDate, SessionClock.Sequence, rows);

                QrEcc ecc;
                QrCode code = LogsheetQr.Encode(payload, out ecc);

                // Render one module per pixel and let the window scale it up by a
                // whole factor, which keeps every module square and crisp.
                Bitmap bitmap = code.ToBitmap(1, 4);

                // The node id is in the caption because there are two codes to
                // scan now, and the operator should see which one is up.
                string caption = string.Format(CultureInfo.InvariantCulture, "{0}   {1}   {2}   —   {3} hour(s)",
                    config.SubstationCode, config.NodeId, scheduler.SessionDateString, hoursHeld);

                Present(QrFlashWindow.ForQr(bitmap, caption, config.QrSeconds, config.QrScreen), log);

                log.Info(string.Format(CultureInfo.InvariantCulture,
                    "QR shown: {0} hour(s), {1} chars, {2} mode, version {3}, ECC {4}.",
                    hoursHeld, payload.Length, code.Mode, code.Version, ecc));
            }
            catch (Exception ex)
            {
                log.Error("Could not build the QR code: " + ex);
                Present(QrFlashWindow.ForMessage("QR CODE COULD NOT BE BUILT", ex.Message,
                            config.QrSeconds, config.QrScreen), log);
            }
        }

        /// <summary>
        /// Puts one surface up and makes it the one the hotkey toggles.
        ///
        /// Never throws: it is the last thing standing between a failure and an
        /// operator seeing nothing at all, so a failure here is logged and the
        /// toggle left in a clean state rather than propagated.
        /// </summary>
        private static void Present(QrFlashWindow window, NodeLog log)
        {
            try
            {
                window.FormClosed += (s, e) =>
                {
                    var closed = (QrFlashWindow)s;
                    lock (_flashGate) if (ReferenceEquals(_flash, closed)) _flash = null;
                    log.Info("QR hidden (" + closed.HideReason + ").");
                };

                _flash = window;
                window.Show();
                window.Activate();
            }
            catch (Exception ex)
            {
                log.Error("Could not put the QR surface on screen: " + ex.Message);
                _flash = null;
            }
        }

        // ── Calibration ────────────────────────────────────────────────────

        /// <summary>
        /// Reads this PC's screen and puts the annotated capture on the
        /// operator's display. Off the UI thread — a dozen Tesseract passes take
        /// a few seconds, and the tray menu should not hang while they run.
        ///
        /// A shot that carries no picture still opens a window saying why: the
        /// operator pressed something and is watching, and a request that
        /// silently does nothing is indistinguishable from a broken one.
        /// </summary>
        private static void ShowCalibration(NodeConfig config, CaptureService capture,
                                            HiddenHost host, NodeLog log)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                CalibrationShot shot = capture.Calibrate(true, 0);

                if (shot.Image != null)
                {
                    try { shot.SaveTo(config.ResolvePath("Calibration")); }
                    catch (Exception ex) { log.Warn("Could not keep the calibration: " + ex.Message); }
                }

                host.Post(() =>
                {
                    try
                    {
                        var window = new CalibrationWindow(shot, "read here", config.QrScreen);
                        window.Show();
                        window.Activate();
                    }
                    catch (Exception ex)
                    {
                        log.Error("Could not show the calibration: " + ex);
                    }
                });
            });
        }

        private static void OpenCalibrationFolder(NodeConfig config, NodeLog log)
        {
            try
            {
                string folder = config.ResolvePath("Calibration");
                Directory.CreateDirectory(folder);
                System.Diagnostics.Process.Start(folder);
            }
            catch (Exception ex)
            {
                log.Error("Could not open the calibration folder: " + ex.Message);
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
