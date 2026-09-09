using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Substation.Shared;
using Tesseract;

namespace SubstationOcrAgent
{
    /// <summary>
    /// Reads the values out of the marked boxes.
    ///
    /// Two things make this markedly more reliable than the V1 whole-strip pass:
    /// each ROI is split into its declared rows and OCR'd one line at a time
    /// (PSM 7), so a row that fails cannot shift its neighbours; and the SCADA
    /// panels are binarised on the green channel, where the phosphor text
    /// separates from the black panel far better than in luminance.
    /// </summary>
    public sealed class RoiOcrEngine : IDisposable
    {
        private readonly string _tessDataPath;
        private readonly object _gate = new object();
        private TesseractEngine _engine;

        public RoiOcrEngine(string tessDataPath)
        {
            _tessDataPath = tessDataPath;
        }

        private TesseractEngine Engine()
        {
            // Built lazily and reused: constructing an engine costs far more
            // than a page, and the hourly pass runs a dozen of them back to back.
            if (_engine == null)
            {
                // EngineMode.Default matches what V1 ran against the tessdata
                // installed on these servers. TesseractOnly would honour the
                // character whitelist more strictly but needs the legacy
                // traineddata, which the fast tessdata packages leave out.
                _engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
                _engine.SetVariable("tessedit_char_whitelist", "0123456789.-");
                _engine.SetVariable("classify_bln_numeric_mode", "1");
                _engine.DefaultPageSegMode = PageSegMode.SingleLine;
            }
            return _engine;
        }

        /// <summary>Reads every enabled ROI out of one screenshot.</summary>
        public ReadingFrame Read(Bitmap screenshot, RoiConfig rois, string serverId,
                                string sessionDate, int hour, string agentVersion)
        {
            var frame = new ReadingFrame
            {
                ServerId = serverId,
                SessionDate = sessionDate,
                Hour = hour,
                CapturedUtc = DateTime.UtcNow,
                AgentVersion = agentVersion
            };

            var screenSize = new Size(screenshot.Width, screenshot.Height);

            foreach (RoiDefinition roi in rois.EnabledRois())
            {
                Rectangle rect = rois.RectFor(roi, screenSize);
                using (Bitmap crop = ScreenGrabber.Crop(screenshot, rect))
                {
                    if (crop == null)
                    {
                        foreach (string row in roi.Rows)
                            frame.Channels.Add(Unreadable(roi, row, "ROI lies outside the captured screen"));
                        continue;
                    }
                    frame.Channels.AddRange(ReadRoi(crop, roi));
                }
            }

            return frame;
        }

        /// <summary>Reads one ROI: row by row first, whole block as a fallback.</summary>
        public List<ChannelReading> ReadRoi(Bitmap crop, RoiDefinition roi)
        {
            var readings = new List<ChannelReading>();
            int rowCount = roi.Rows.Count;
            if (rowCount == 0) return readings;

            for (int i = 0; i < rowCount; i++)
            {
                string row = roi.Rows[i];
                using (Bitmap band = ExtractRowBand(crop, i, rowCount, roi.RowPadding))
                {
                    if (band == null) { readings.Add(Unreadable(roi, row, "row band is empty")); continue; }
                    using (Bitmap prepared = Prepare(band, roi))
                    {
                        string raw;
                        double confidence;
                        Recognize(prepared, PageSegMode.SingleLine, out raw, out confidence);

                        readings.Add(new ChannelReading
                        {
                            Key = roi.ChannelKey(row),
                            Row = row,
                            RoiId = roi.Id,
                            RawText = (raw ?? "").Trim(),
                            Value = NumberFormat.Normalize(raw),
                            Confidence = confidence
                        });
                    }
                }
            }

            // If any row came back empty, the band split may be off (a resized
            // panel, a changed font). Read the whole box as one block and fill
            // the gaps positionally from what that returns.
            foreach (var r in readings)
            {
                if (r.Ok) continue;
                ApplyBlockFallback(crop, roi, readings);
                break;
            }

            return readings;
        }

        private void ApplyBlockFallback(Bitmap crop, RoiDefinition roi, List<ChannelReading> readings)
        {
            List<string> numbers;
            double confidence;

            using (Bitmap prepared = Prepare(crop, roi))
            {
                string raw;
                Recognize(prepared, PageSegMode.SingleBlock, out raw, out confidence);
                numbers = SplitNumbers(raw);
            }

            // Only trust the fallback when it found exactly as many numbers as
            // the box declares rows. Anything else and we would be guessing which
            // number belongs to which row — precisely the V1 failure mode.
            if (numbers.Count != readings.Count) return;

            for (int i = 0; i < readings.Count; i++)
            {
                if (readings[i].Ok) continue;
                readings[i].Value = numbers[i];
                readings[i].RawText = (readings[i].RawText + " |block:" + numbers[i]).Trim();
                readings[i].Confidence = confidence;
            }
        }

        private static List<string> SplitNumbers(string raw)
        {
            var found = new List<string>();
            if (string.IsNullOrEmpty(raw)) return found;

            foreach (string line in raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string value = NumberFormat.Normalize(line);
                if (!string.IsNullOrEmpty(value)) found.Add(value);
            }
            return found;
        }

        private void Recognize(Bitmap prepared, PageSegMode mode, out string text, out double confidence)
        {
            lock (_gate)
            {
                using (Page page = Engine().Process(prepared, mode))
                {
                    text = page.GetText();
                    confidence = page.GetMeanConfidence() * 100.0;
                }
            }
        }

        private static ChannelReading Unreadable(RoiDefinition roi, string row, string reason)
        {
            return new ChannelReading
            {
                Key = roi.ChannelKey(row),
                Row = row,
                RoiId = roi.Id,
                Value = "",
                RawText = reason,
                Confidence = 0
            };
        }

        // ── Image preparation ──────────────────────────────────────────────

        /// <summary>Horizontal slice holding one stacked value cell.</summary>
        public static Bitmap ExtractRowBand(Bitmap crop, int index, int rowCount, int padding)
        {
            double bandHeight = (double)crop.Height / rowCount;
            int top = (int)Math.Round(index * bandHeight) + padding;
            int bottom = (int)Math.Round((index + 1) * bandHeight) - padding;

            if (bottom - top < 3)
            {
                // Too thin to pad: take the raw band rather than nothing.
                top = (int)Math.Round(index * bandHeight);
                bottom = (int)Math.Round((index + 1) * bandHeight);
            }

            top = Math.Max(0, top);
            bottom = Math.Min(crop.Height, bottom);
            if (bottom - top <= 0) return null;

            return crop.Clone(new Rectangle(0, top, crop.Width, bottom - top), crop.PixelFormat);
        }

        /// <summary>
        /// Upscale, isolate the text channel, Otsu-binarise, then invert to
        /// dark-on-light and add a quiet margin — the shape Tesseract reads best.
        /// </summary>
        public static Bitmap Prepare(Bitmap source, RoiDefinition roi)
        {
            using (Bitmap scaled = ScreenGrabber.Upscale(source, roi.Scale))
            {
                byte[] gray = ToChannelBytes(scaled, roi.ColorChannel);
                int threshold = OtsuThreshold(gray);
                return ToBinaryBitmap(gray, scaled.Width, scaled.Height, threshold, roi.Invert, 12);
            }
        }

        private static byte[] ToChannelBytes(Bitmap source, string colorChannel)
        {
            int w = source.Width, h = source.Height;
            var bytes = new byte[w * h];

            BitmapData data = source.LockBits(new Rectangle(0, 0, w, h),
                                              ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                unsafe
                {
                    byte* scan0 = (byte*)data.Scan0;
                    bool useGreen = !string.Equals(colorChannel, "gray", StringComparison.OrdinalIgnoreCase);

                    for (int y = 0; y < h; y++)
                    {
                        byte* row = scan0 + (y * data.Stride);
                        int outRow = y * w;
                        for (int x = 0; x < w; x++)
                        {
                            byte b = row[x * 3];
                            byte g = row[x * 3 + 1];
                            byte r = row[x * 3 + 2];

                            // The value text is green on black. Taking the green
                            // channel alone keeps the digits at full strength and
                            // pushes the panel, its border and the grey chrome down.
                            bytes[outRow + x] = useGreen
                                ? g
                                : (byte)((r * 299 + g * 587 + b * 114) / 1000);
                        }
                    }
                }
            }
            finally
            {
                source.UnlockBits(data);
            }
            return bytes;
        }

        /// <summary>Otsu's method: the threshold maximising between-class variance.</summary>
        public static int OtsuThreshold(byte[] gray)
        {
            var histogram = new int[256];
            foreach (byte value in gray) histogram[value]++;

            int total = gray.Length;
            if (total == 0) return 128;

            double sum = 0;
            for (int i = 0; i < 256; i++) sum += i * (double)histogram[i];

            double sumBackground = 0;
            int weightBackground = 0;
            double bestVariance = -1;
            int best = 128;

            for (int t = 0; t < 256; t++)
            {
                weightBackground += histogram[t];
                if (weightBackground == 0) continue;

                int weightForeground = total - weightBackground;
                if (weightForeground == 0) break;

                sumBackground += t * (double)histogram[t];

                double meanBackground = sumBackground / weightBackground;
                double meanForeground = (sum - sumBackground) / weightForeground;
                double between = (double)weightBackground * weightForeground *
                                 (meanBackground - meanForeground) * (meanBackground - meanForeground);

                if (between > bestVariance) { bestVariance = between; best = t; }
            }
            return best;
        }

        private static Bitmap ToBinaryBitmap(byte[] gray, int w, int h, int threshold, bool invert, int margin)
        {
            var output = new Bitmap(w + margin * 2, h + margin * 2, PixelFormat.Format24bppRgb);

            BitmapData data = output.LockBits(new Rectangle(0, 0, output.Width, output.Height),
                                              ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                unsafe
                {
                    byte* scan0 = (byte*)data.Scan0;

                    // Quiet margin first: white when the output is dark-on-light.
                    byte background = invert ? (byte)255 : (byte)0;
                    for (int y = 0; y < output.Height; y++)
                    {
                        byte* row = scan0 + (y * data.Stride);
                        for (int x = 0; x < output.Width * 3; x++) row[x] = background;
                    }

                    for (int y = 0; y < h; y++)
                    {
                        byte* row = scan0 + ((y + margin) * data.Stride);
                        int inRow = y * w;
                        for (int x = 0; x < w; x++)
                        {
                            bool lit = gray[inRow + x] > threshold;
                            // "lit" is a text pixel; invert paints it black on white.
                            byte v = invert ? (lit ? (byte)0 : (byte)255)
                                            : (lit ? (byte)255 : (byte)0);
                            int off = (x + margin) * 3;
                            row[off] = v;
                            row[off + 1] = v;
                            row[off + 2] = v;
                        }
                    }
                }
            }
            finally
            {
                output.UnlockBits(data);
            }
            return output;
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_engine != null) { _engine.Dispose(); _engine = null; }
            }
        }
    }
}
