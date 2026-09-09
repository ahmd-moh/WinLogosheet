using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SubstationOcrServer
{
    /// <summary>
    /// Full-screen surface showing only the QR code — no buttons, no labels, no
    /// chrome. This is the one thing either node ever puts on screen.
    ///
    /// It targets the MAIN screen: on these servers the secondary head carries
    /// the SCADA wall view being read, so the code goes on the operator's own
    /// display. `qrScreen` in the config overrides that.
    ///
    /// It closes on Esc, on a click, on the hotkey again, or after qrSeconds.
    /// The form takes ownership of the bitmap and disposes it on close.
    /// </summary>
    public sealed class QrFlashWindow : Form
    {
        private readonly Bitmap _qr;
        private readonly Timer _autoHide;
        private readonly string _caption;

        /// <summary>Why the window went away — read by the caller when logging.</summary>
        public string HideReason { get; set; }

        public QrFlashWindow(Bitmap qr, string caption, int seconds, string qrScreen)
        {
            _qr = qr;
            _caption = caption ?? "";
            HideReason = "external";

            Screen target = PickScreen(qrScreen);

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = target.Bounds;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.Black;
            KeyPreview = true;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw, true);

            KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Escape) return;
                HideReason = "Esc";
                Close();
            };
            MouseClick += (s, e) => { HideReason = "click"; Close(); };

            _autoHide = new Timer { Interval = Math.Max(1, seconds) * 1000 };
            _autoHide.Tick += (s, e) =>
            {
                HideReason = "auto-timeout " + seconds + " s";
                Close();
            };
            _autoHide.Start();
        }

        /// <summary>
        /// "primary" (the default) is the operator's own display. "secondary"
        /// picks the first non-primary screen; a number picks that index.
        /// Anything unresolvable falls back to the primary, so the code is
        /// never displayed nowhere.
        /// </summary>
        public static Screen PickScreen(string qrScreen)
        {
            Screen[] screens = Screen.AllScreens;
            if (screens.Length == 0) return Screen.PrimaryScreen;

            string want = (qrScreen ?? "primary").Trim();

            int index;
            if (int.TryParse(want, out index))
                return index >= 0 && index < screens.Length ? screens[index] : Screen.PrimaryScreen;

            if (want.Equals("secondary", StringComparison.OrdinalIgnoreCase))
            {
                foreach (Screen screen in screens)
                    if (!screen.Primary) return screen;
            }

            return Screen.PrimaryScreen;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_qr == null) return;

            // Largest size that fits with a margin, snapped to a whole multiple
            // of the source pixels so the modules stay razor sharp. Falls back to
            // a plain fit if the code is somehow larger than the screen.
            int side = (int)(Math.Min(ClientSize.Width, ClientSize.Height) * 0.90f);
            if (side < 1) return;

            int scale = side / _qr.Width;
            int size = scale >= 1 ? _qr.Width * scale : side;

            int x = (ClientSize.Width - size) / 2;
            int y = (ClientSize.Height - size) / 2;

            // A white mat behind the code guarantees the quiet zone against the
            // black background, whatever the bitmap itself carries.
            int pad = Math.Max(12, size / 25);
            e.Graphics.FillRectangle(Brushes.White, x - pad, y - pad, size + pad * 2, size + pad * 2);

            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.DrawImage(_qr, x, y, size, size);

            if (_caption.Length == 0) return;

            using (var font = new Font("Segoe UI", 14, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            {
                SizeF measured = e.Graphics.MeasureString(_caption, font);
                e.Graphics.DrawString(_caption, font, brush,
                    (ClientSize.Width - measured.Width) / 2,
                    Math.Max(4, y - pad - measured.Height - 10));
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _autoHide.Stop();
            _autoHide.Dispose();
            if (_qr != null) _qr.Dispose();
            base.OnFormClosed(e);
        }
    }
}
