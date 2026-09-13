using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using Substation.Shared;

namespace Substation.Capture
{
    /// <summary>One marked box as it stood when the calibration was taken.</summary>
    public sealed class CalibrationBox
    {
        public string RoiId = "";
        public string Label = "";
        public bool Enabled = true;

        /// <summary>Where the box landed in actual screen pixels, after the
        /// reference-size scaling. This is the number an engineer edits back
        /// into the ROI file when a box has drifted.</summary>
        public Rectangle Rect;

        /// <summary>What each row read, in row order. Empty when the shot was
        /// taken without OCR.</summary>
        public List<ChannelReading> Readings = new List<ChannelReading>();

        public int OkCount
        {
            get
            {
                int n = 0;
                foreach (ChannelReading r in Readings) if (r.Ok) n++;
                return n;
            }
        }

        public Dictionary<string, object> ToJson()
        {
            var readings = new List<object>();
            foreach (ChannelReading r in Readings) readings.Add(r.ToJson());

            return new Dictionary<string, object>
            {
                { "id", RoiId },
                { "label", Label },
                { "enabled", Enabled },
                { "rect", new Dictionary<string, object>
                          { { "x", Rect.X }, { "y", Rect.Y }, { "w", Rect.Width }, { "h", Rect.Height } } },
                { "readings", readings }
            };
        }

        public static CalibrationBox FromJson(Dictionary<string, object> node)
        {
            Dictionary<string, object> rect = Json.Dict(node, "rect");
            var box = new CalibrationBox
            {
                RoiId = Json.Str(node, "id"),
                Label = Json.Str(node, "label"),
                Enabled = Json.Bool(node, "enabled", true),
                Rect = new Rectangle(Json.Int(rect, "x", 0), Json.Int(rect, "y", 0),
                                     Json.Int(rect, "w", 0), Json.Int(rect, "h", 0))
            };

            foreach (object r in Json.List(node, "readings"))
                box.Readings.Add(ChannelReading.FromJson(Json.Dict(r)));

            return box;
        }
    }

    /// <summary>
    /// What one node's screen looked like through its own ROI file: the capture
    /// with every box drawn on it, plus what each box read at that moment.
    ///
    /// This is the only place a picture crosses the wire. It is not a reading —
    /// nothing here is stored as an hour or ever reaches the QR payload — it is
    /// the answer to "are that node's boxes still on the right panels?", asked
    /// from the 132 kV seat instead of by walking to the other server.
    /// </summary>
    public sealed class CalibrationShot
    {
        /// <summary>A PNG larger than this is re-encoded as JPEG before it goes
        /// on the wire. The protocol caps a line at 8 MiB and base64 costs a
        /// third on top, so a big wall view has to give up something; the boxes
        /// and the digits survive JPEG, the file size does not survive PNG.</summary>
        public const int PngWireBudget = 3 * 1024 * 1024;

        public string NodeId = "";
        public string DisplayName = "";
        public string Machine = "";
        public string AgentVersion = "";

        /// <summary>The node's own words for the display it read.</summary>
        public string ScreenDescription = "";

        public int ScreenWidth, ScreenHeight;

        /// <summary>Size the ROI rectangles were measured against. When it
        /// differs from the screen size every box was scaled to fit, which is
        /// the first thing to suspect when boxes sit slightly off.</summary>
        public int ReferenceWidth, ReferenceHeight;

        public DateTime TakenUtc = DateTime.UtcNow;

        /// <summary>Non-empty when the capture or the OCR failed outright.</summary>
        public string Error = "";

        /// <summary>"png" or "jpeg".</summary>
        public string ImageFormat = "png";

        /// <summary>The capture with the boxes and the values drawn on it.</summary>
        public byte[] Image;

        public List<CalibrationBox> Boxes = new List<CalibrationBox>();

        /// <summary>Where the node that took it put its own copy.</summary>
        public string SavedPath = "";

        public DateTime TakenLocal { get { return TakenUtc.ToLocalTime(); } }

        public int ValuesRead
        {
            get
            {
                int n = 0;
                foreach (CalibrationBox b in Boxes) if (b.Enabled) n += b.OkCount;
                return n;
            }
        }

        public int ValuesTotal
        {
            get
            {
                int n = 0;
                foreach (CalibrationBox b in Boxes) if (b.Enabled) n += b.Readings.Count;
                return n;
            }
        }

        public double MeanConfidence
        {
            get
            {
                double sum = 0;
                int n = 0;
                foreach (CalibrationBox box in Boxes)
                    foreach (ChannelReading r in box.Readings)
                    {
                        if (!r.Ok) continue;
                        sum += r.Confidence;
                        n++;
                    }
                return n == 0 ? 0 : sum / n;
            }
        }

        /// <summary>One line for a log, a tray balloon or a window caption.</summary>
        public string Summary()
        {
            if (!string.IsNullOrEmpty(Error)) return NodeId + ": " + Error;

            return string.Format(CultureInfo.InvariantCulture,
                "{0} - {1} of {2} value(s) read in {3} box(es), mean confidence {4:0.0}%, screen {5}x{6}",
                NodeId, ValuesRead, ValuesTotal, Boxes.Count, MeanConfidence,
                ScreenWidth, ScreenHeight);
        }

        // -- The picture ----------------------------------------------------

        /// <summary>
        /// Encodes the annotated capture for transport, narrowing it to
        /// <paramref name="maxWidth"/> first (0 keeps the full resolution).
        /// </summary>
        public void SetImage(Bitmap annotated, int maxWidth)
        {
            Bitmap sized = annotated;
            bool ownsSized = false;

            try
            {
                if (maxWidth > 0 && annotated.Width > maxWidth)
                {
                    int height = Math.Max(1, (int)Math.Round(
                        annotated.Height * (double)maxWidth / annotated.Width));

                    sized = new Bitmap(maxWidth, height, PixelFormat.Format24bppRgb);
                    ownsSized = true;

                    using (var g = Graphics.FromImage(sized))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(annotated, new Rectangle(0, 0, maxWidth, height));
                    }
                }

                using (var png = new MemoryStream())
                {
                    sized.Save(png, System.Drawing.Imaging.ImageFormat.Png);
                    if (png.Length <= PngWireBudget)
                    {
                        Image = png.ToArray();
                        ImageFormat = "png";
                        return;
                    }
                }

                Image = Jpeg(sized, 85L);
                ImageFormat = "jpeg";
            }
            finally
            {
                if (ownsSized) sized.Dispose();
            }
        }

        private static byte[] Jpeg(Bitmap bitmap, long quality)
        {
            ImageCodecInfo codec = null;
            foreach (ImageCodecInfo candidate in ImageCodecInfo.GetImageEncoders())
                if (candidate.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid) codec = candidate;

            using (var buffer = new MemoryStream())
            {
                if (codec == null)
                {
                    bitmap.Save(buffer, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
                else
                {
                    using (var parameters = new EncoderParameters(1))
                    {
                        parameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                        bitmap.Save(buffer, codec, parameters);
                    }
                }
                return buffer.ToArray();
            }
        }

        /// <summary>Decodes the carried picture. Returns null when there is none.</summary>
        public Bitmap ToBitmap()
        {
            if (Image == null || Image.Length == 0) return null;

            // A Bitmap built from a stream keeps using that stream for its
            // lifetime, so the pixels are copied out and the stream let go here.
            using (var source = new MemoryStream(Image))
            using (var loaded = new Bitmap(source))
                return new Bitmap(loaded);
        }

        /// <summary>
        /// Writes the picture under the given folder as
        /// <c>nodeId-yyyyMMdd-HHmmss.png</c> and remembers where it went. Both
        /// nodes save their own; the server also saves what it is sent, so a
        /// commissioning session leaves one folder holding both wall views.
        /// </summary>
        public string SaveTo(string folder)
        {
            if (Image == null || Image.Length == 0)
                throw new InvalidOperationException("This calibration carries no picture: " + Error);

            Directory.CreateDirectory(folder);

            string name = (string.IsNullOrEmpty(NodeId) ? "node" : NodeId) + "-" +
                          TakenLocal.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) +
                          (ImageFormat == "jpeg" ? ".jpg" : ".png");

            string path = Path.Combine(folder, name);
            File.WriteAllBytes(path, Image);
            SavedPath = path;
            return path;
        }

        // -- Wire form ------------------------------------------------------

        public Dictionary<string, object> ToJson()
        {
            var boxes = new List<object>();
            foreach (CalibrationBox box in Boxes) boxes.Add(box.ToJson());

            return new Dictionary<string, object>
            {
                { "nodeId", NodeId },
                { "displayName", DisplayName },
                { "machine", Machine },
                { "agentVersion", AgentVersion },
                { "screen", ScreenDescription },
                { "screenWidth", ScreenWidth },
                { "screenHeight", ScreenHeight },
                { "referenceWidth", ReferenceWidth },
                { "referenceHeight", ReferenceHeight },
                { "takenUtc", TakenUtc.ToString("o", CultureInfo.InvariantCulture) },
                { "error", Error },
                { "imageFormat", ImageFormat },
                { "image", Image == null ? "" : Convert.ToBase64String(Image) },
                { "boxes", boxes }
            };
        }

        public static CalibrationShot FromJson(Dictionary<string, object> node)
        {
            var shot = new CalibrationShot
            {
                NodeId = Json.Str(node, "nodeId"),
                DisplayName = Json.Str(node, "displayName"),
                Machine = Json.Str(node, "machine"),
                AgentVersion = Json.Str(node, "agentVersion"),
                ScreenDescription = Json.Str(node, "screen"),
                ScreenWidth = Json.Int(node, "screenWidth", 0),
                ScreenHeight = Json.Int(node, "screenHeight", 0),
                ReferenceWidth = Json.Int(node, "referenceWidth", 0),
                ReferenceHeight = Json.Int(node, "referenceHeight", 0),
                Error = Json.Str(node, "error"),
                ImageFormat = Json.Str(node, "imageFormat", "png")
            };

            DateTime taken;
            if (DateTime.TryParse(Json.Str(node, "takenUtc"), CultureInfo.InvariantCulture,
                                  DateTimeStyles.RoundtripKind, out taken))
                shot.TakenUtc = taken.ToUniversalTime();

            string image = Json.Str(node, "image");
            if (!string.IsNullOrEmpty(image))
            {
                // A picture that will not decode is a damaged shot, not a dead
                // node: keep the numbers and say so in the error line.
                try { shot.Image = Convert.FromBase64String(image); }
                catch (FormatException) { shot.Error = "the picture did not survive the wire (bad base64)"; }
            }

            foreach (object box in Json.List(node, "boxes"))
                shot.Boxes.Add(CalibrationBox.FromJson(Json.Dict(box)));

            return shot;
        }
    }
}
