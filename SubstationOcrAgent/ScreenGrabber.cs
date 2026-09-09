using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace SubstationOcrAgent
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
        public static Rectangle MonitorBounds(int monitorIndex)
        {
            Screen[] screens = Screen.AllScreens;
            if (screens.Length == 0) return Screen.PrimaryScreen.Bounds;
            if (monitorIndex < 0 || monitorIndex >= screens.Length) return screens[0].Bounds;
            return screens[monitorIndex].Bounds;
        }

        /// <summary>Caller owns the returned bitmap.</summary>
        public static Bitmap CaptureMonitor(int monitorIndex)
        {
            Rectangle bounds = MonitorBounds(monitorIndex);
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
