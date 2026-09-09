using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace SubstationOcrServer
{
    /// <summary>
    /// Flashes the QR code on the MAIN screen for a few seconds, then hides.
    ///
    /// This is the only thing either node ever puts on screen. It is borderless,
    /// topmost, click-through-free and takes no focus — the SCADA application
    /// underneath keeps the keyboard, which matters on a live operator desktop.
    /// It closes on its own after the configured time, or on any key or click.
    /// </summary>
    public sealed class QrFlashWindow : Form
    {
        private readonly PictureBox _picture;
        private readonly Label _caption;
        private readonly Label _footer;
        private readonly Timer _hideTimer;

        private Image[] _pages = new Image[0];
        private string[] _captions = new string[0];
        private int _page;
        private int _secondsPerPage;

        public QrFlashWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.White;
            DoubleBuffered = true;

            _caption = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.Black
            };

            _picture = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.CenterImage,
                BackColor = Color.White
            };

            _footer = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.DimGray
            };

            Controls.Add(_picture);
            Controls.Add(_footer);
            Controls.Add(_caption);

            _hideTimer = new Timer();
            _hideTimer.Tick += (s, e) => NextPageOrHide();

            _picture.Click += (s, e) => HideNow();
            _caption.Click += (s, e) => HideNow();
            _footer.Click += (s, e) => HideNow();
            Click += (s, e) => HideNow();
        }

        /// <summary>Never steal focus from the SCADA application underneath.</summary>
        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= 0x08000000;  // WS_EX_NOACTIVATE
                parameters.ExStyle |= 0x00000008;  // WS_EX_TOPMOST
                return parameters;
            }
        }

        /// <summary>
        /// Shows one or more codes on the primary screen. With several pages
        /// each is held for the same number of seconds before the next appears,
        /// so the operator can scan a multi-part payload in one pass.
        /// </summary>
        public void Flash(Image[] pages, string[] captions, string footer, int secondsPerPage)
        {
            if (pages == null || pages.Length == 0) return;

            DisposePages();
            _pages = pages;
            _captions = captions ?? new string[pages.Length];
            _page = 0;
            _secondsPerPage = Math.Max(1, secondsPerPage);
            _footer.Text = footer ?? "";

            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            int side = (int)(Math.Min(screen.Width, screen.Height) * 0.82);
            side = Math.Max(360, side);

            Size = new Size(side, side + _caption.Height + _footer.Height);
            Location = new Point(screen.X + (screen.Width - Width) / 2,
                                 screen.Y + (screen.Height - Height) / 2);

            ShowPage();

            _hideTimer.Interval = _secondsPerPage * 1000;
            _hideTimer.Start();

            Show();
            TopMost = true;
            BringToFront();
        }

        private void ShowPage()
        {
            _picture.Image = _pages[_page];
            _caption.Text = _page < _captions.Length ? _captions[_page] : "";
        }

        private void NextPageOrHide()
        {
            if (_page + 1 < _pages.Length)
            {
                _page++;
                ShowPage();
                return;
            }
            HideNow();
        }

        private void HideNow()
        {
            _hideTimer.Stop();
            _picture.Image = null;
            Hide();
            DisposePages();
        }

        private void DisposePages()
        {
            foreach (Image page in _pages)
                if (page != null) page.Dispose();
            _pages = new Image[0];
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Any key dismisses it early.
            HideNow();
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // The window lives for the life of the process; hide, never close.
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideNow();
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hideTimer.Dispose();
                DisposePages();
            }
            base.Dispose(disposing);
        }

        /// <summary>Convenience for the status caption under the code.</summary>
        public static string DescribeCoverage(int hoursHeld, int hoursExpected)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "{0} of {1} hour(s) gathered", hoursHeld, hoursExpected);
        }
    }
}
