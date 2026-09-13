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
            log.Info("Running on " + NodeEnvironment.Describe() + ".");
            log.Info("Reading screen: " + ScreenGrabber.Describe(config.CaptureScreen) + ".");

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

            if (HasFlag(args, "--calibrate"))
            {
                // Writes this node's own overlay and quits, for commissioning
                // from a command line instead of the tray.
                try { log.Info("Calibration written to " + capture.WriteCalibrationOverlay()); }
                catch (Exception ex) { log.Error("Calibration failed: " + ex.Message); }
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

            // A calibration coming back from the 33 kV node arrives on a socket
            // thread; the engineer who asked for it is sitting in front of this
            // screen, so it crosses onto the UI thread and opens a window.
            server.CalibrationReceived += shot =>
                host.Post(() => ShowCalibration(config, shot, "sent by " + shot.NodeId, log));

            host.BuildingMenu += menu =>
            {
                menu.Items.Add("Show QR code now", null,
                    (s, e) => ToggleQr(config, merged, scheduler, log));
                menu.Items.Add("Read this hour now", null, (s, e) => scheduler.CaptureNow());
                menu.Items.Add("Check this node's boxes (" + config.NodeId + ")", null,
                    (s, e) => ShowOwnCalibration(config, capture, host, log));
                menu.Items.Add("Ask " + config.ClientNodeId + " for its boxes", null,
                    (s, e) => AskClientForCalibration(config, server, host, log));
                menu.Items.Add("Open calibration folder", null, (s, e) => OpenCalibrationFolder(config, log));
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

        /// <summary>
        /// Builds the LS1 payload for the pinned session and shows it.
        ///
        /// Every path out of here puts something on screen. The operator pressed
        /// a key and is watching: an empty screen tells them nothing about which
        /// of the several possible faults they are looking at, and the log line
        /// that would tell them is on a machine they are not sitting at.
        /// </summary>
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
                    string why = merged.DiagnoseEmpty(scheduler.SessionDateString);
                    log.Warn("QR requested but nothing has been gathered yet for " +
                             scheduler.SessionDateString + ". " + why);

                    Present(QrFlashWindow.ForMessage("NO READINGS TO ENCODE",
                                "Session " + scheduler.SessionDateString + "\n" + why,
                                config.QrSeconds, config.QrScreen), log);
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
        // Both wall views can be checked from this seat: this node reads its own
        // screen, and the 33 kV node is asked to read its own and send the
        // picture back. Neither one needs anybody to walk to the other server.

        /// <summary>Watches the last request made of the client node.</summary>
        private static System.Threading.Timer _calibrationWatchdog;

        /// <summary>
        /// Reads this node's own screen and puts the annotated capture on the
        /// operator's display. Off the UI thread — a dozen Tesseract passes take
        /// a few seconds, and the tray menu should not hang while they run.
        /// </summary>
        private static void ShowOwnCalibration(ServerConfig config, CaptureService capture,
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

                host.Post(() => ShowCalibration(config, shot, "read here", log));
            });
        }

        /// <summary>
        /// Parks a calibration request for the client node and says what to
        /// expect. The link only opens the other way, so the request waits until
        /// that node's next poll — seconds, normally, but not instant, and an
        /// operator told nothing assumes the click did nothing.
        /// </summary>
        private static void AskClientForCalibration(ServerConfig config, ReadingServer server,
                                                    HiddenHost host, NodeLog log)
        {
            RemoteCommand queued = server.RequestCalibration(config.ClientNodeId, config.CalibrationMaxWidth);

            host.Notify(Product, "Asked " + config.ClientNodeId +
                        " for its calibration. It appears here when that node answers.", false);

            // Nothing arriving is itself an answer, and it is one the operator
            // can only get from this seat: the log that would explain it is on
            // the other machine.
            //
            // The timer is held in a field because a Timer nothing references is
            // collectable, and one collected before it fires says nothing at all.
            if (_calibrationWatchdog != null) _calibrationWatchdog.Dispose();
            _calibrationWatchdog = new System.Threading.Timer(_ =>
            {
                string complaint = Complaint(server.StateOf(queued.Id), config.ClientNodeId);
                if (complaint == null) return;

                log.Warn(complaint);
                host.Notify(Product, complaint, true);
            }, null, TimeSpan.FromSeconds(90), TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>What to tell the operator when nothing came back. Null when
        /// the request was answered and there is nothing to say.</summary>
        private static string Complaint(CommandState state, string clientNodeId)
        {
            switch (state)
            {
                case CommandState.Waiting:
                    return clientNodeId + " has not asked for work since. Is that node running, " +
                           "is its nodeId really '" + clientNodeId + "', and is pollSeconds above 0?";
                case CommandState.Collected:
                    return clientNodeId + " took the request but sent nothing back. " +
                           "Check that node's log — its screen capture or OCR may have failed.";
                case CommandState.Expired:
                    return "The calibration request expired: " + clientNodeId + " never collected it.";
                default:
                    return null; // answered
            }
        }

        /// <summary>
        /// Puts one shot on screen. UI thread only.
        ///
        /// A shot that carries no picture still opens a window saying why: the
        /// operator pressed something and is watching, and a request that
        /// silently does nothing is indistinguishable from a broken one.
        /// </summary>
        private static void ShowCalibration(ServerConfig config, CalibrationShot shot,
                                            string origin, NodeLog log)
        {
            try
            {
                var window = new CalibrationWindow(shot, origin, config.QrScreen);
                window.Show();
                window.Activate();
            }
            catch (Exception ex)
            {
                log.Error("Could not show the calibration: " + ex);
            }
        }

        private static void OpenCalibrationFolder(ServerConfig config, NodeLog log)
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
