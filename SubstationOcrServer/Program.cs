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
                Run(config, args);
            }
        }

        private static void Run(ServerConfig config, string[] args)
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

            var flash = new QrFlashWindow();

            // The hook fires on a pool thread; the window must be touched on the
            // UI thread, so bounce through its handle.
            var hotkey = new HotkeyListener();
            hotkey.Triggered += () =>
            {
                try
                {
                    if (!flash.IsHandleCreated) return;
                    flash.BeginInvoke((Action)(() => ShowQr(config, merged, scheduler, flash, log)));
                }
                catch (Exception ex)
                {
                    log.Error("QR display failed: " + ex.Message);
                }
            };

            try
            {
                hotkey.Start();
                log.Info("Hotkey armed: hold Ctrl+Shift and press 7, 8, 9 to show the QR code.");
            }
            catch (Exception ex)
            {
                log.Error("Hotkey not available: " + ex.Message + " — the QR code cannot be shown.");
            }

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
                    (s, e) => ShowQr(config, merged, scheduler, flash, log));
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
                hotkey.Dispose();
                scheduler.Dispose();
                server.Dispose();
                capture.Dispose();
                flash.Dispose();
            };

            // Force the handle now — CreateControl is a no-op while a form is
            // hidden, and the hotkey needs a handle to marshal onto from the very
            // first press, without the window ever being shown.
            IntPtr handle = flash.Handle;
            log.Info("QR window ready (handle " + handle.ToInt64().ToString("X") + ").");

            host.Start();
            Application.Run(host);
        }

        /// <summary>Builds the payload for the pinned session and flashes it.</summary>
        private static void ShowQr(ServerConfig config, MergedStore merged, HourlyScheduler scheduler,
                                   QrFlashWindow flash, NodeLog log)
        {
            try
            {
                string sessionDate = scheduler.SessionDateString;

                int hoursHeld;
                Dictionary<int, string[]> rows = merged.BuildRows(sessionDate, out hoursHeld);

                var options = new QrOptions
                {
                    SubstationCode = config.SubstationCode,
                    Format = config.QrFormat,
                    Ecc = config.QrEcc,
                    UrlTemplate = config.QrUrlTemplate
                };

                var builder = new QrPayloadBuilder(options);
                QrEcc ecc = builder.Ecc;
                List<string> payloads = builder.Build(sessionDate, SessionClock.Sequence, rows, ecc);

                if (payloads.Count == 0 || hoursHeld == 0)
                {
                    log.Warn("QR requested but nothing has been gathered yet for " + sessionDate + ".");
                    return;
                }

                int expected = SessionClock.ExpectedReadings(scheduler.SessionDate, DateTime.Now,
                                                             config.CaptureMinute);

                var pages = new List<Image>();
                var captions = new List<string>();

                for (int i = 0; i < payloads.Count; i++)
                {
                    QrCode code = QrCode.Encode(payloads[i], ecc);

                    Rectangle screen = Screen.PrimaryScreen.WorkingArea;
                    int box = (int)(Math.Min(screen.Width, screen.Height) * 0.78);
                    pages.Add(code.ToBitmap(code.ModuleSizeFor(box)));

                    captions.Add(payloads.Count > 1
                        ? string.Format(CultureInfo.InvariantCulture, "{0}   {1}   —   part {2} of {3}",
                                        config.SubstationCode, sessionDate, i + 1, payloads.Count)
                        : config.SubstationCode + "   " + sessionDate);
                }

                string footer = QrFlashWindow.DescribeCoverage(hoursHeld, Math.Max(hoursHeld, expected));
                flash.Flash(pages.ToArray(), captions.ToArray(), footer, config.QrSeconds);

                log.Info("QR shown: " + hoursHeld + " hour(s), " + payloads.Count + " code(s), " +
                         config.QrSeconds + " s each.");
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
