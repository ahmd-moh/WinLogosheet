using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;
using Substation.Capture;

namespace SubstationOcrServer
{
    /// <summary>
    /// Shows one calibration shot — this node's own, or the one the 33 kV node
    /// sent back — on the 132 kV operator's screen.
    ///
    /// Unlike the QR flash this is a window that stays: checking a box sits on
    /// the right panel means looking closely, so it zooms to the pixel and pans,
    /// and two shots can stand side by side while an engineer compares the two
    /// wall views without leaving the seat.
    /// </summary>
    public sealed class CalibrationWindow : Form
    {
        private readonly CalibrationShot _shot;
        private readonly Bitmap _image;
        private readonly string _origin;

        private bool _fit = true;
        private float _zoom = 1f;
        private PointF _offset;

        private bool _dragging;
        private Point _dragFrom;
        private PointF _dragOffset;

        private static int _cascade;

        public CalibrationWindow(CalibrationShot shot, string origin, string screen)
        {
            _shot = shot;
            _origin = origin ?? "";
            _image = shot.ToBitmap();

            Text = "Calibration - " + shot.NodeId + " - " +
                   shot.TakenLocal.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            BackColor = Color.FromArgb(18, 18, 18);
            ShowInTaskbar = true;
            KeyPreview = true;
            StartPosition = FormStartPosition.Manual;
            MinimumSize = new Size(480, 360);
            Bounds = PlaceOn(QrFlashWindow.PickScreen(screen));

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw, true);

            KeyDown += OnKey;
            MouseWheel += OnWheel;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += (s, e) => _dragging = false;
            MouseDoubleClick += (s, e) => ToggleZoom(e.Location);
        }

        /// <summary>
        /// Most of the operator's screen, stepped along for each window already
        /// open so a second shot does not land exactly on top of the first.
        /// </summary>
        private static Rectangle PlaceOn(Screen screen)
        {
            Rectangle work = screen.WorkingArea;
            int width = (int)(work.Width * 0.92);
            int height = (int)(work.Height * 0.92);
            int step = (_cascade++ % 4) * 28;

            return new Rectangle(work.X + (work.Width - width) / 2 + step,
                                 work.Y + (work.Height - height) / 2 + step,
                                 width, height);
        }

        // -- Input ----------------------------------------------------------

        private void OnKey(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    Close();
                    break;
                case Keys.F:
                case Keys.D0:
                    _fit = true;
                    Invalidate();
                    break;
                case Keys.Add:
                case Keys.Oemplus:
                    Zoom(1.25f, Centre());
                    break;
                case Keys.Subtract:
                case Keys.OemMinus:
                    Zoom(0.8f, Centre());
                    break;
                case Keys.D1:
                    ToggleZoom(Centre());
                    break;
            }
        }

        private Point Centre()
        {
            return new Point(ClientSize.Width / 2, ImageArea().Height / 2);
        }

        private void OnWheel(object sender, MouseEventArgs e)
        {
            Zoom(e.Delta > 0 ? 1.2f : 1f / 1.2f, e.Location);
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _image == null) return;

            // Read the current view before leaving fit mode: afterwards both
            // answers come from the fields this is about to set.
            float scale = CurrentScale();
            PointF offset = CurrentOffset();

            _dragging = true;
            _dragFrom = e.Location;
            _dragOffset = offset;
            _offset = offset;
            _zoom = scale;
            _fit = false;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            _offset = new PointF(_dragOffset.X + (e.X - _dragFrom.X),
                                 _dragOffset.Y + (e.Y - _dragFrom.Y));
            Invalidate();
        }

        /// <summary>Fit and 100% are the two views worth having; this swaps them.</summary>
        private void ToggleZoom(Point at)
        {
            if (_fit) Zoom(1f / CurrentScale(), at);
            else { _fit = true; Invalidate(); }
        }

        private void Zoom(float factor, Point at)
        {
            if (_image == null) return;

            float was = CurrentScale();
            PointF wasOffset = CurrentOffset();
            float now = Math.Max(0.05f, Math.Min(8f, was * factor));

            // Keep whatever is under the pointer under the pointer.
            _offset = new PointF(at.X - (at.X - wasOffset.X) * now / was,
                                 at.Y - (at.Y - wasOffset.Y) * now / was);
            _zoom = now;
            _fit = false;
            Invalidate();
        }

        // -- Layout ---------------------------------------------------------

        private int FooterHeight { get { return Math.Max(56, ClientSize.Height / 11); } }

        private Rectangle ImageArea()
        {
            return new Rectangle(0, 0, ClientSize.Width, Math.Max(1, ClientSize.Height - FooterHeight));
        }

        private float FitScale()
        {
            Rectangle area = ImageArea();
            if (_image == null || _image.Width == 0 || _image.Height == 0) return 1f;

            return Math.Min((float)area.Width / _image.Width, (float)area.Height / _image.Height);
        }

        private float CurrentScale()
        {
            return _fit ? FitScale() : _zoom;
        }

        private PointF CurrentOffset()
        {
            if (!_fit) return _offset;

            Rectangle area = ImageArea();
            float scale = FitScale();
            return new PointF((area.Width - _image.Width * scale) / 2f,
                              (area.Height - _image.Height * scale) / 2f);
        }

        // -- Painting -------------------------------------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.Clear(BackColor);

            if (_image == null) { PaintMissing(g); PaintFooter(g); return; }

            float scale = CurrentScale();
            PointF offset = CurrentOffset();

            Rectangle area = ImageArea();
            g.SetClip(area);
            g.InterpolationMode = scale >= 1f
                ? InterpolationMode.NearestNeighbor   // pixel-level checking
                : InterpolationMode.HighQualityBicubic; // legible when shrunk
            g.PixelOffsetMode = PixelOffsetMode.Half;

            g.DrawImage(_image, new RectangleF(offset.X, offset.Y,
                                               _image.Width * scale, _image.Height * scale));
            g.ResetClip();

            PaintFooter(g);
        }

        /// <summary>
        /// A shot can arrive with no picture — the node could not reach its
        /// screen, or the capture failed. Saying so beats an empty black window,
        /// which looks exactly like a window that failed to draw.
        /// </summary>
        private void PaintMissing(Graphics g)
        {
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            string text = string.IsNullOrEmpty(_shot.Error)
                ? "This calibration carries no picture."
                : _shot.NodeId + " could not take a calibration:\n\n" + _shot.Error;

            using (var font = new Font("Segoe UI", Math.Max(14f, ClientSize.Height / 26f),
                                       FontStyle.Regular, GraphicsUnit.Pixel))
            using (var brush = new SolidBrush(Color.FromArgb(255, 190, 60)))
            using (var centred = new StringFormat { Alignment = StringAlignment.Center,
                                                    LineAlignment = StringAlignment.Center })
                g.DrawString(text, font, brush, ImageArea(), centred);
        }

        private void PaintFooter(Graphics g)
        {
            int top = ClientSize.Height - FooterHeight;

            using (var band = new SolidBrush(Color.FromArgb(32, 32, 32)))
            using (var rule = new Pen(Color.FromArgb(64, 64, 64)))
            {
                g.FillRectangle(band, 0, top, ClientSize.Width, FooterHeight);
                g.DrawLine(rule, 0, top, ClientSize.Width, top);
            }

            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float em = Math.Max(12f, ClientSize.Height / 52f);

            string headline = _shot.NodeId + "  " + _shot.DisplayName +
                              (string.IsNullOrEmpty(_origin) ? "" : "   (" + _origin + ")");

            string detail = string.Format(CultureInfo.InvariantCulture,
                "{0} of {1} value(s) read   {2}   {3}   {4}",
                _shot.ValuesRead, _shot.ValuesTotal, Tally(),
                _shot.ScreenDescription,
                string.IsNullOrEmpty(_shot.SavedPath) ? "" : "saved to " + _shot.SavedPath);

            string hints = "wheel or +/- zoom   drag to pan   double-click or 1 for 100%   F fits   Esc closes";

            using (var head = new Font("Segoe UI", em * 1.15f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var body = new Font("Segoe UI", em, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var headBrush = new SolidBrush(Color.White))
            using (var bodyBrush = new SolidBrush(Color.FromArgb(200, 200, 200)))
            using (var hintBrush = new SolidBrush(Color.FromArgb(130, 130, 130)))
            {
                float y = top + 6;
                g.DrawString(headline, head, headBrush, 12, y);
                y += em * 1.5f;
                g.DrawString(detail, body, bodyBrush, 12, y);
                y += em * 1.35f;
                g.DrawString(hints, body, hintBrush, 12, y);
            }
        }

        /// <summary>How many boxes came out clean, partial and empty.</summary>
        private string Tally()
        {
            int clean = 0, partial = 0, empty = 0, off = 0;

            foreach (CalibrationBox box in _shot.Boxes)
            {
                if (!box.Enabled) { off++; continue; }
                if (box.Readings.Count > 0 && box.OkCount == box.Readings.Count) clean++;
                else if (box.OkCount > 0) partial++;
                else empty++;
            }

            return clean + " box(es) clean, " + partial + " partial, " + empty + " empty" +
                   (off == 0 ? "" : ", " + off + " off");
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_image != null) _image.Dispose();
            base.OnFormClosed(e);
        }
    }
}
