using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using Substation.Shared;

namespace SubstationOcrAgent
{
    /// <summary>
    /// One hourly reading: grab the wall view, read the marked boxes, store the
    /// numbers. The screenshot is released as soon as OCR is done and, unless
    /// the operator asked to keep it, never touches the disk.
    /// </summary>
    public sealed class CaptureService : IDisposable
    {
        private readonly AgentConfig _config;
        private readonly HourStore _store;
        private readonly AgentLog _log;
        private readonly RoiOcrEngine _ocr;
        private readonly object _gate = new object();

        private RoiConfig _rois;
        private string _roiPath;

        public CaptureService(AgentConfig config, HourStore store, AgentLog log)
        {
            _config = config;
            _store = store;
            _log = log;
            _ocr = new RoiOcrEngine(config.TessDataPath);
            _roiPath = config.ResolvePath(config.RoiConfigPath);
            _rois = RoiConfig.Load(_roiPath);
        }

        public RoiConfig Rois { get { lock (_gate) return _rois; } }

        public DateTime LastCaptureLocal { get; private set; }
        public string LastError { get; private set; }

        public void ReloadRois()
        {
            lock (_gate)
            {
                _rois = RoiConfig.Load(_roiPath);
                _log.Info("ROI configuration reloaded: " + _rois.Rois.Count + " box(es) from " + _roiPath);
            }
        }

        /// <summary>
        /// Reads the display now and stores the result under the given logsheet
        /// slot. Never throws: a failed hour is recorded as a frame carrying an
        /// error so the collector can show the operator what went wrong.
        /// </summary>
        public ReadingFrame CaptureNow(string sessionDate, int hour)
        {
            lock (_gate)
            {
                try
                {
                    using (Bitmap screenshot = ScreenGrabber.CaptureMonitor(_config.MonitorIndex))
                    {
                        if (_config.KeepFullScreenshots) SaveScreenshot(screenshot, sessionDate, hour);

                        ReadingFrame frame = _ocr.Read(screenshot, _rois, _config.ServerId,
                                                       sessionDate, hour, AgentInfo.Version);
                        _store.Save(frame);
                        LastCaptureLocal = DateTime.Now;
                        LastError = "";

                        _log.Info(string.Format(CultureInfo.InvariantCulture,
                            "Captured {0} hour {1:00}: {2}/{3} channel(s) read, mean confidence {4:0.0}%",
                            sessionDate, hour, CountOk(frame), frame.Channels.Count, frame.MeanConfidence));

                        return frame;
                    }
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    _log.Error("Capture failed for hour " + hour + ": " + ex);

                    var frame = new ReadingFrame
                    {
                        ServerId = _config.ServerId,
                        SessionDate = sessionDate,
                        Hour = hour,
                        AgentVersion = AgentInfo.Version,
                        Error = ex.Message
                    };
                    _store.Save(frame);
                    return frame;
                }
            }
        }

        private static int CountOk(ReadingFrame frame)
        {
            int n = 0;
            foreach (var c in frame.Channels) if (c.Ok) n++;
            return n;
        }

        private void SaveScreenshot(Bitmap screenshot, string sessionDate, int hour)
        {
            try
            {
                string folder = Path.Combine(_config.ResolvePath(_config.DataFolder), "Screens-" + sessionDate);
                Directory.CreateDirectory(folder);
                screenshot.Save(Path.Combine(folder, hour.ToString("00", CultureInfo.InvariantCulture) + ".png"),
                                ImageFormat.Png);
            }
            catch (Exception ex)
            {
                _log.Error("Could not keep the full screenshot: " + ex.Message);
            }
        }

        /// <summary>
        /// Writes a full screenshot with every ROI outlined and labelled, so the
        /// engineer commissioning the agent can check the boxes land on the right
        /// panels. Returns the file path — the image stays on the agent host.
        /// </summary>
        public string WriteCalibrationOverlay()
        {
            lock (_gate)
            {
                string folder = _config.ResolvePath("Calibration");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder,
                    "overlay-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".png");

                using (Bitmap screenshot = ScreenGrabber.CaptureMonitor(_config.MonitorIndex))
                {
                    var screenSize = new Size(screenshot.Width, screenshot.Height);

                    using (var g = Graphics.FromImage(screenshot))
                    using (var pen = new Pen(Color.Red, 2))
                    using (var disabledPen = new Pen(Color.DimGray, 1))
                    using (var font = new Font("Segoe UI", 9, FontStyle.Bold))
                    using (var brush = new SolidBrush(Color.Yellow))
                    using (var shade = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                    {
                        foreach (RoiDefinition roi in _rois.Rois)
                        {
                            Rectangle rect = _rois.RectFor(roi, screenSize);
                            g.DrawRectangle(roi.Enabled ? pen : disabledPen, rect);

                            string caption = roi.Id + (roi.Enabled ? "" : " (off)");
                            SizeF size = g.MeasureString(caption, font);
                            var labelAt = new RectangleF(rect.X, Math.Max(0, rect.Y - size.Height - 2),
                                                         size.Width + 4, size.Height);
                            g.FillRectangle(shade, labelAt);
                            g.DrawString(caption, font, brush, labelAt.X + 2, labelAt.Y);
                        }
                    }

                    screenshot.Save(path, ImageFormat.Png);
                }

                _log.Info("Calibration overlay written to " + path);
                return path;
            }
        }

        /// <summary>
        /// PNG bytes of one ROI as Tesseract sees it, for on-demand
        /// troubleshooting. Gated on allowRoiImages, which ships off.
        /// </summary>
        public byte[] RoiEvidencePng(string roiId, bool prepared)
        {
            lock (_gate)
            {
                RoiDefinition roi = _rois.Find(roiId);
                if (roi == null) throw new ArgumentException("No ROI named '" + roiId + "'.");

                using (Bitmap screenshot = ScreenGrabber.CaptureMonitor(_config.MonitorIndex))
                {
                    Rectangle rect = _rois.RectFor(roi, new Size(screenshot.Width, screenshot.Height));
                    using (Bitmap crop = ScreenGrabber.Crop(screenshot, rect))
                    {
                        if (crop == null) throw new InvalidOperationException("ROI lies outside the captured screen.");
                        using (Bitmap output = prepared ? RoiOcrEngine.Prepare(crop, roi) : (Bitmap)crop.Clone())
                        using (var buffer = new MemoryStream())
                        {
                            output.Save(buffer, ImageFormat.Png);
                            return buffer.ToArray();
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            _ocr.Dispose();
        }
    }

    public static class AgentInfo
    {
        public const string Version = "2.0.0";
        public const string Product = "Substation OCR Agent";
    }
}
