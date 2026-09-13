using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
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
    ///
    /// The same surface also carries the reason when there is no code to show.
    /// Failing silently is worse than useless here: from in front of the screen
    /// a hotkey nobody registered, a hotkey another program stole, and a night
    /// of OCR reading nothing all look identical, and the log that tells them
    /// apart is on a machine the operator is not sitting at.
    /// </summary>
    public sealed class QrFlashWindow : Form
    {
        private readonly Bitmap _qr;
        private readonly Timer _autoHide;
        private readonly string _caption;
        private readonly string _headline;
        private readonly string _detail;

        /// <summary>Why the window went away — read by the caller when logging.</summary>
        public string HideReason { get; set; }

        /// <summary>The code itself, captioned with the session it covers.</summary>
        public static QrFlashWindow ForQr(Bitmap qr, string caption, int seconds, string qrScreen)
        {
            return new QrFlashWindow(qr, caption, null, null, seconds, qrScreen);
        }

        /// <summary>
        /// Why there is no code, in the same place the code would have been.
        ///
        /// Held a little longer than a code: qrSeconds is set for how long a
        /// phone needs to scan, and two lines take longer than that to read. The
        /// five-second ceiling is the one the config already allows, so this
        /// stays inside the 3-5 s the SCADA desktop was specified for.
        /// </summary>
        public static QrFlashWindow ForMessage(string headline, string detail, int seconds, string qrScreen)
        {
            return new QrFlashWindow(null, null, headline, detail, Math.Max(seconds, 5), qrScreen);
        }

        private QrFlashWindow(Bitmap qr, string caption, string headline, string detail,
                              int seconds, string qrScreen)
        {
            _qr = qr;
            _caption = caption ?? "";
            _headline = headline ?? "";
            _detail = detail ?? "";
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

            if (_qr == null) { PaintMessage(e.Graphics); return; }

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

        /// <summary>
        /// Amber on black, sized off the screen height so it carries from where
        /// the operator actually stands. Amber, not white, so that at a glance
        /// this reads as "something to fix" rather than as a code that failed
        /// to draw.
        /// </summary>
        private void PaintMessage(Graphics g)
        {
            if (_headline.Length == 0 && _detail.Length == 0) return;

            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float headEm = Math.Max(20f, ClientSize.Height / 16f);
            float bodyEm = Math.Max(13f, ClientSize.Height / 34f);

            var block = new RectangleF(ClientSize.Width * 0.08f, 0, ClientSize.Width * 0.84f, 0);
            if (block.Width < 1) return;

            using (var headFont = new Font("Segoe UI", headEm, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var bodyFont = new Font("Segoe UI", bodyEm, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var headBrush = new SolidBrush(Color.FromArgb(255, 190, 60)))
            using (var bodyBrush = new SolidBrush(Color.FromArgb(215, 215, 215)))
            using (var centred = new StringFormat
                   {
                       Alignment = StringAlignment.Center,
                       LineAlignment = StringAlignment.Near,
                       Trimming = StringTrimming.EllipsisWord
                   })
            {
                SizeF head = g.MeasureString(_headline, headFont, (int)block.Width, centred);
                SizeF body = g.MeasureString(_detail, bodyFont, (int)block.Width, centred);

                float gap = bodyEm;
                float top = Math.Max(0, (ClientSize.Height - (head.Height + gap + body.Height)) / 2f);

                g.DrawString(_headline, headFont, headBrush,
                             new RectangleF(block.X, top, block.Width, head.Height), centred);
                g.DrawString(_detail, bodyFont, bodyBrush,
                             new RectangleF(block.X, top + head.Height + gap, block.Width, body.Height), centred);
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
