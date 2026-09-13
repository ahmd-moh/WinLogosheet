using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Substation.Capture
{
    /// <summary>
    /// Grabs the SCADA wall view from the console session.
    ///
    /// Screen capture only works from an interactive desktop, which is why the
    /// agent is a tray application on the logged-in session rather than a
    /// Windows service.
    /// </summary>
    public static class ScreenGrabber
    {
        /// <summary>
        /// Resolves the configured display.
        ///
        /// "secondary" — the first non-primary screen, which is where the wall
        /// view lives on both servers. "primary" — the main screen. A number
        /// picks that index out of Screen.AllScreens directly.
        ///
        /// If "secondary" is asked for on a machine with one screen there is
        /// nothing to fall back to but the primary, so that is what is returned
        /// and the caller logs it — a silent capture of the wrong screen would
        /// look like an OCR fault for days.
        /// </summary>
        public static Screen Resolve(string captureScreen, out string note)
        {
            note = "";
            Screen[] screens = Screen.AllScreens;
            if (screens.Length == 0) { note = "no screens reported"; return Screen.PrimaryScreen; }

            string want = (captureScreen ?? "secondary").Trim();

            int index;
            if (int.TryParse(want, out index))
            {
                if (index >= 0 && index < screens.Length) return screens[index];
                note = "screen index " + index + " does not exist; using the primary screen";
                return Screen.PrimaryScreen;
            }

            if (want.Equals("primary", StringComparison.OrdinalIgnoreCase)) return Screen.PrimaryScreen;

            foreach (Screen screen in screens)
                if (!screen.Primary) return screen;

            note = "no secondary screen is attached; using the primary screen";
            return Screen.PrimaryScreen;
        }

        public static Rectangle Bounds(string captureScreen)
        {
            string note;
            return Resolve(captureScreen, out note).Bounds;
        }

        public static string Describe(string captureScreen)
        {
            string note;
            Screen screen = Resolve(captureScreen, out note);
            Rectangle b = screen.Bounds;
            return string.Format("{0} {1}x{2} at ({3},{4}){5}",
                screen.Primary ? "primary" : "secondary", b.Width, b.Height, b.X, b.Y,
                string.IsNullOrEmpty(note) ? "" : " — " + note);
        }

        /// <summary>Caller owns the returned bitmap.</summary>
        public static Bitmap Capture(string captureScreen)
        {
            Rectangle bounds = Bounds(captureScreen);
            var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
            try
            {
                using (var g = Graphics.FromImage(bmp))
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);
                return bmp;
            }
            catch
            {
                bmp.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Crops one ROI, clamping to the screen so a stale calibration produces
        /// an empty reading rather than an unhandled exception at 02 past.
        /// </summary>
        public static Bitmap Crop(Bitmap source, Rectangle rect)
        {
            Rectangle clamped = Rectangle.Intersect(rect, new Rectangle(0, 0, source.Width, source.Height));
            if (clamped.Width <= 0 || clamped.Height <= 0) return null;
            return source.Clone(clamped, source.PixelFormat);
        }

        /// <summary>
        /// Upscales with a nearest-neighbour-free interpolation: the SCADA face
        /// is thin and anti-aliased, and bicubic keeps the stroke continuous
        /// where nearest-neighbour would break it into blocks.
        /// </summary>
        public static Bitmap Upscale(Bitmap source, int factor)
        {
            if (factor <= 1) return (Bitmap)source.Clone();

            var scaled = new Bitmap(source.Width * factor, source.Height * factor, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(source, new Rectangle(0, 0, scaled.Width, scaled.Height));
            }
            return scaled;
        }
    }
}
