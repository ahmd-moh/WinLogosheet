using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using Substation.Shared;

namespace Substation.Capture
{
    /// <summary>
    /// One hourly reading: grab the configured display, read the marked boxes,
    /// store the numbers. The screen capture is released as soon as OCR is done
    /// and, unless the operator asked to keep it, never touches the disk.
    /// </summary>
    public sealed class CaptureService : IDisposable
    {
        private readonly NodeConfig _config;
        private readonly HourStore _store;
        private readonly NodeLog _log;
        private readonly RoiOcrEngine _ocr;
        private readonly string _version;
        private readonly object _gate = new object();

        private RoiConfig _rois;
        private readonly string _roiPath;

        public CaptureService(NodeConfig config, HourStore store, NodeLog log, string version)
        {
            _config = config;
            _store = store;
            _log = log;
            _version = version;

            string tessData = TessData.Resolve(config.TessDataPath, log);
            _ocr = new RoiOcrEngine(tessData);
            _roiPath = config.ResolvePath(config.RoiConfigPath);
            _rois = RoiConfig.Load(_roiPath);

            WarmOcr(tessData);
        }

        /// <summary>
        /// Starts Tesseract during start-up instead of at the first reading, so a
        /// machine that cannot run it says so among the start-up lines — where
        /// whoever is commissioning the node is looking — rather than once an
        /// hour from :02 onwards. On a fresh SCADA server the usual cause is the
        /// Visual C++ runtime the native DLLs need, and the Tesseract wrapper
        /// reports that as a DLL it "failed to find" although the file is right
        /// there, so the runtime is named here.
        /// </summary>
        private void WarmOcr(string tessData)
        {
            Exception failure = _ocr.Warm();
            if (failure == null)
            {
                _log.Info("OCR engine ready.");
                return;
            }

            // The wrapper loads its native half by reflection, so the reason
            // arrives wrapped in a TargetInvocationException that says nothing.
            string reason = failure.GetBaseException().Message;

            string runtime = NodeEnvironment.VcRuntimeProblem();
            if (runtime != null)
                _log.Error("OCR cannot start: " + reason + " " + runtime +
                           " Every hourly reading will fail until then.");
            else if (TessData.Holds(tessData))
                _log.Error("OCR cannot start — every hourly reading will fail: " + reason);

            // Otherwise TessData.Resolve has already said the language data is
            // missing, which is the fault to fix first.
        }

        public RoiConfig Rois { get { lock (_gate) return _rois; } }

        public DateTime LastCaptureLocal { get; private set; }
        public string LastError { get; private set; }

        /// <summary>Which display this node reads, for the start-up log line.</summary>
        public string ScreenDescription
        {
            get { return ScreenGrabber.Describe(_config.CaptureScreen); }
        }

        public void ReloadRois()
        {
            lock (_gate)
            {
                _rois = RoiConfig.Load(_roiPath);
                _log.Info("ROI configuration reloaded: " + _rois.Rois.Count + " box(es) from " + _roiPath);
            }
        }

        /// <summary>
        /// Reads the display now and stores the result under the given session
        /// slot. Never throws: a failed hour is recorded as a frame carrying an
        /// error, so the gap is visible rather than silent.
        /// </summary>
        public ReadingFrame CaptureNow(string sessionDate, int hour)
        {
            lock (_gate)
            {
                try
                {
                    using (Bitmap screenshot = ScreenGrabber.Capture(_config.CaptureScreen))
                    {
                        if (_config.KeepFullScreenshots) SaveScreenshot(screenshot, sessionDate, hour);

                        ReadingFrame frame = _ocr.Read(screenshot, _rois, _config.NodeId,
                                                       sessionDate, hour, _version);
                        _store.Save(frame);
                        LastCaptureLocal = DateTime.Now;
                        LastError = "";

                        _log.Info(string.Format(CultureInfo.InvariantCulture,
                            "Read {0} hour {1:00}: {2}/{3} value(s), mean confidence {4:0.0}%",
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
                        ServerId = _config.NodeId,
                        SessionDate = sessionDate,
                        Hour = hour,
                        AgentVersion = _version,
                        Error = ex.Message
                    };
                    _store.Save(frame);
                    return frame;
                }
            }
        }

        public static int CountOk(ReadingFrame frame)
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
                _log.Error("Could not keep the full screen capture: " + ex.Message);
            }
        }

        /// <summary>
        /// Takes a calibration shot: one capture of the configured display with
        /// every ROI outlined and named, and — when <paramref name="readValues"/>
        /// is set — what each box read at that moment drawn beside it.
        ///
        /// Reading the boxes costs one Tesseract pass per row, a few seconds in
        /// all. It is what turns "the rectangles are drawn" into "the rectangles
        /// sit on the right panels and the digits come out of them", which is
        /// the question the engineer is actually asking. Nothing taken here is
        /// stored as an hour or sent as a reading.
        ///
        /// Never throws: a failure comes back as a shot carrying the reason, so
        /// whoever asked sees an answer either way — a window that says why,
        /// rather than nothing on screen.
        /// </summary>
        public CalibrationShot Calibrate(bool readValues, int maxImageWidth)
        {
            lock (_gate)
            {
                var shot = new CalibrationShot
                {
                    NodeId = _config.NodeId,
                    DisplayName = _config.DisplayName,
                    Machine = Environment.MachineName,
                    AgentVersion = _version,
                    ScreenDescription = ScreenGrabber.Describe(_config.CaptureScreen),
                    ReferenceWidth = _rois.ReferenceWidth,
                    ReferenceHeight = _rois.ReferenceHeight,
                    TakenUtc = DateTime.UtcNow
                };

                try
                {
                    using (Bitmap screenshot = ScreenGrabber.Capture(_config.CaptureScreen))
                    {
                        shot.ScreenWidth = screenshot.Width;
                        shot.ScreenHeight = screenshot.Height;

                        ReadingFrame frame = null;
                        if (readValues)
                        {
                            DateTime now = DateTime.Now;
                            frame = _ocr.Read(screenshot, _rois, _config.NodeId,
                                              SessionClock.SessionDateStringOf(now), now.Hour, _version);
                        }

                        FillBoxes(shot, frame, new Size(screenshot.Width, screenshot.Height));
                        Annotate(screenshot, shot, readValues);
                        shot.SetImage(screenshot, maxImageWidth);
                    }

                    _log.Info("Calibration shot taken: " + shot.Summary());
                }
                catch (Exception ex)
                {
                    shot.Error = ex.Message;
                    _log.Error("Calibration failed: " + ex);
                }

                return shot;
            }
        }

        /// <summary>
        /// Takes a shot at full resolution and leaves it under <c>Calibration\</c>.
        /// Returns the file path — this copy never leaves the node.
        /// </summary>
        public string WriteCalibrationOverlay()
        {
            CalibrationShot shot = Calibrate(true, 0);
            if (!string.IsNullOrEmpty(shot.Error) && shot.Image == null)
                throw new InvalidOperationException(shot.Error);

            string path = shot.SaveTo(_config.ResolvePath("Calibration"));
            _log.Info("Calibration overlay written to " + path);
            return path;
        }

        /// <summary>Pairs every box in the ROI file with what it just read.</summary>
        private void FillBoxes(CalibrationShot shot, ReadingFrame frame, Size screenSize)
        {
            foreach (RoiDefinition roi in _rois.Rois)
            {
                var box = new CalibrationBox
                {
                    RoiId = roi.Id,
                    Label = roi.Label,
                    Enabled = roi.Enabled,
                    Rect = _rois.RectFor(roi, screenSize)
                };

                if (frame != null)
                    foreach (ChannelReading reading in frame.Channels)
                        if (string.Equals(reading.RoiId, roi.Id, StringComparison.OrdinalIgnoreCase))
                            box.Readings.Add(reading);

                shot.Boxes.Add(box);
            }
        }

        /// <summary>
        /// Draws the boxes onto the capture, coloured by what came out of them:
        /// green every row read, amber some, red none, grey switched off. The
        /// colour is the whole point — it is readable across a control room,
        /// where a list of confidences is not.
        /// </summary>
        private static void Annotate(Bitmap screenshot, CalibrationShot shot, bool readValues)
        {
            float em = Math.Max(11f, screenshot.Height / 70f);

            var placed = new List<RectangleF>();

            using (var g = Graphics.FromImage(screenshot))
            using (var font = new Font("Segoe UI", em, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var shade = new SolidBrush(Color.FromArgb(190, 0, 0, 0)))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                foreach (CalibrationBox box in shot.Boxes)
                {
                    Color colour = BoxColour(box, readValues);

                    using (var pen = new Pen(colour, box.Enabled ? 2 : 1))
                    using (var brush = new SolidBrush(colour))
                    {
                        g.DrawRectangle(pen, box.Rect);

                        string caption = Caption(box, readValues);
                        SizeF size = g.MeasureString(caption, font);
                        RectangleF at = PlaceLabel(box.Rect, size, screenshot, placed);
                        placed.Add(at);

                        g.FillRectangle(shade, at);
                        g.DrawString(caption, font, brush, at.X + 3, at.Y);
                    }
                }

                DrawHeader(g, screenshot, shot, readValues, em, shade);
            }
        }

        /// <summary>
        /// Finds somewhere the caption can be read: above its box, below it, or
        /// stepped down until it clears the captions already drawn. Boxes on
        /// these wall views sit shoulder to shoulder, and two labels on top of
        /// each other are worse than either one alone.
        /// </summary>
        private static RectangleF PlaceLabel(Rectangle box, SizeF size, Bitmap screenshot,
                                             List<RectangleF> placed)
        {
            float width = size.Width + 6;
            float x = Math.Min(box.X, Math.Max(0, screenshot.Width - width));

            var candidates = new List<float>
            {
                box.Y - size.Height - 2,          // above
                box.Bottom + 2,                   // below
                box.Y + 1                         // inside, when the box is hemmed in
            };

            foreach (float candidate in candidates)
            {
                if (candidate < 0 || candidate + size.Height > screenshot.Height) continue;

                var at = new RectangleF(x, candidate, width, size.Height);
                if (!Collides(at, placed)) return at;
            }

            // Every natural spot is taken: walk down from the box until it is
            // clear, or give up at the top of it rather than leave the picture.
            float y = Math.Max(0, box.Y - size.Height - 2);
            for (int step = 0; step < 12; step++)
            {
                var at = new RectangleF(x, y, width, size.Height);
                if (!Collides(at, placed) && y + size.Height <= screenshot.Height) return at;
                y += size.Height + 2;
            }

            return new RectangleF(x, Math.Max(0, box.Y - size.Height - 2), width, size.Height);
        }

        private static bool Collides(RectangleF candidate, List<RectangleF> placed)
        {
            foreach (RectangleF taken in placed)
                if (candidate.IntersectsWith(taken)) return true;
            return false;
        }

        private static Color BoxColour(CalibrationBox box, bool readValues)
        {
            if (!box.Enabled) return Color.DimGray;
            if (!readValues || box.Readings.Count == 0) return Color.Red;
            if (box.OkCount == box.Readings.Count) return Color.FromArgb(90, 230, 90);
            return box.OkCount == 0 ? Color.FromArgb(255, 80, 80) : Color.FromArgb(255, 185, 60);
        }

        private static string Caption(CalibrationBox box, bool readValues)
        {
            string caption = box.RoiId + (box.Enabled ? "" : " (off)");
            if (!readValues || !box.Enabled) return caption;

            foreach (ChannelReading reading in box.Readings)
                caption += "  " + reading.Row + "=" + (reading.Ok ? reading.Value : "?");

            return caption;
        }

        /// <summary>
        /// Stamps the shot with which node, which screen and which ROI file it
        /// came from. A picture of a wall view with red boxes on it is otherwise
        /// impossible to place once it has travelled to another machine.
        /// </summary>
        private static void DrawHeader(Graphics g, Bitmap screenshot, CalibrationShot shot,
                                       bool readValues, float em, Brush shade)
        {
            string scaled = shot.ReferenceWidth == shot.ScreenWidth && shot.ReferenceHeight == shot.ScreenHeight
                ? "boxes used as measured"
                : "boxes scaled from " + shot.ReferenceWidth + "x" + shot.ReferenceHeight;

            string header =
                shot.NodeId + "  " + shot.DisplayName + "  on " + shot.Machine + "\n" +
                shot.ScreenDescription + "   " + scaled + "   " +
                shot.TakenLocal.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "\n" +
                (readValues
                    ? string.Format(CultureInfo.InvariantCulture,
                        "{0} of {1} value(s) read, mean confidence {2:0.0}%   -   green all rows, amber some, red none, grey off",
                        shot.ValuesRead, shot.ValuesTotal, shot.MeanConfidence)
                    : "boxes only - this shot was taken without reading them");

            using (var headFont = new Font("Segoe UI", em * 1.15f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var headBrush = new SolidBrush(Color.White))
            {
                SizeF size = g.MeasureString(header, headFont);
                g.FillRectangle(shade, 0, 0, Math.Min(screenshot.Width, size.Width + 16), size.Height + 10);
                g.DrawString(header, headFont, headBrush, 8, 5);
            }
        }

        public void Dispose()
        {
            _ocr.Dispose();
        }
    }
}
