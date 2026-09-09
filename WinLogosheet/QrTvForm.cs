using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinLogosheet
{
    // ═══════════════════════════════════════════════════════════════════════
    //  TV QR DISPLAY  (headless mode)
    //  Borderless fullscreen surface that shows ONLY the logsheet QR code —
    //  no buttons, no labels, no window chrome. It targets the SECOND screen
    //  (the TV); when no second monitor is attached it falls back to the
    //  primary so the code is never displayed nowhere. Esc, a mouse click or
    //  the global Ctrl+Shift+7,8,9 sequence closes it, and it always hides
    //  itself after AutoHideMs so the TV never shows a stale code for long.
    //  The form takes ownership of the QR bitmap and disposes it on close.
    // ═══════════════════════════════════════════════════════════════════════
    public class QrTvForm : Form
    {
        private const int AutoHideMs = 7000;

        private readonly Bitmap _qr;
        private readonly Timer _autoHide;

        // Why the form went away — read by Form1 when logging the close.
        public string HideReason { get; set; } = "external";

        public QrTvForm(Bitmap qr)
        {
            _qr = qr;

            Screen target = PickTargetScreen();
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = target.Bounds;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.Black;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw, true);

            KeyDown += (s, e) =>
            { if (e.KeyCode == Keys.Escape) { HideReason = "Esc"; Close(); } };
            MouseClick += (s, e) => { HideReason = "click"; Close(); };

            _autoHide = new Timer { Interval = AutoHideMs };
            _autoHide.Tick += (s, e) =>
            { HideReason = $"auto-timeout {AutoHideMs / 1000} s"; Close(); };
            _autoHide.Start();
        }

        // Prefer any non-primary monitor (the TV). Primary is the fallback.
        private static Screen PickTargetScreen()
        {
            foreach (Screen s in Screen.AllScreens)
                if (!s.Primary) return s;
            return Screen.PrimaryScreen;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_qr == null) return;

            // Largest size that fits with a margin, snapped to a whole
            // multiple of the source pixels so modules stay razor sharp on
            // the TV. Falls back to plain fit if the code is somehow bigger
            // than the screen.
            int side = (int)(Math.Min(ClientSize.Width, ClientSize.Height) * 0.92f);
            if (side < 1) return;
            int scale = side / _qr.Width;
            int size = scale >= 1 ? _qr.Width * scale : side;

            int x = (ClientSize.Width - size) / 2;
            int y = (ClientSize.Height - size) / 2;

            // White mat behind the code guarantees the quiet zone against the
            // black background regardless of what the PNG embeds.
            int pad = Math.Max(12, size / 25);
            e.Graphics.FillRectangle(Brushes.White,
                x - pad, y - pad, size + pad * 2, size + pad * 2);

            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.DrawImage(_qr, x, y, size, size);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _autoHide.Stop();
            _autoHide.Dispose();
            _qr?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
