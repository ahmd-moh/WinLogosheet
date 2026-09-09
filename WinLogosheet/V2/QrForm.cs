using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Windows.Forms;

namespace WinLogosheet.V2
{
    /// <summary>
    /// Shows the day (or one hour) as a QR code for the Android app to scan.
    ///
    /// A full 24-hour day is a dense code, so it is drawn as large as the window
    /// allows and, when it will not fit one symbol, split into numbered parts the
    /// phone reassembles. "This hour" produces a small, easy code for a quick
    /// check at the panel.
    /// </summary>
    public sealed class QrForm : Form
    {
        private readonly QrPayloadBuilder _builder;
        private readonly V2Settings _settings;
        private readonly DateTime _sessionDate;
        private readonly IEnumerable<int> _hoursInOrder;
        private readonly Dictionary<int, string[]> _hourData;
        private readonly HashSet<int> _skippedHours;
        private readonly int _currentHour;

        private readonly PictureBox _picture;
        private readonly Label _caption;
        private readonly Label _detail;
        private readonly ComboBox _scope;
        private readonly ComboBox _ecc;
        private readonly Button _previous;
        private readonly Button _next;

        private List<string> _payloads = new List<string>();
        private int _part;

        public QrForm(V2Settings settings, DateTime sessionDate, IEnumerable<int> hoursInOrder,
                      Dictionary<int, string[]> hourData, HashSet<int> skippedHours, int currentHour)
        {
            _settings = settings;
            _builder = new QrPayloadBuilder(settings);
            _sessionDate = sessionDate;
            _hoursInOrder = hoursInOrder;
            _hourData = hourData;
            _skippedHours = skippedHours;
            _currentHour = currentHour;

            Text = "Send to mobile — QR handoff";
            ClientSize = new Size(760, 820);
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(560, 620);

            var top = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8, 8, 8, 0) };

            _scope = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(8, 8),
                Width = 210
            };
            _scope.Items.Add("Whole day (08:00 → 07:00)");
            _scope.Items.Add("This hour only");
            _scope.SelectedIndex = 0;
            _scope.SelectedIndexChanged += (s, e) => Rebuild();

            var eccLabel = new Label
            {
                Text = "Error correction:",
                Location = new Point(232, 12),
                AutoSize = true
            };

            _ecc = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(340, 8),
                Width = 190
            };
            _ecc.Items.Add("L — most data per code");
            _ecc.Items.Add("M — balanced");
            _ecc.Items.Add("Q — tolerant of damage");
            _ecc.Items.Add("H — most tolerant");
            _ecc.SelectedIndex = IndexForEcc(QrCode.ParseEcc(settings.QrEcc));
            _ecc.SelectedIndexChanged += (s, e) => Rebuild();

            top.Controls.Add(_scope);
            top.Controls.Add(eccLabel);
            top.Controls.Add(_ecc);

            _caption = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };

            _picture = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.CenterImage,
                BackColor = Color.White
            };
            _picture.Resize += (s, e) => Render();

            _detail = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray
            };

            var buttons = new Panel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(8) };

            _previous = new Button { Text = "◀ Part", Location = new Point(8, 8), Size = new Size(80, 30) };
            _previous.Click += (s, e) => { if (_part > 0) { _part--; Render(); } };

            _next = new Button { Text = "Part ▶", Location = new Point(94, 8), Size = new Size(80, 30) };
            _next.Click += (s, e) => { if (_part < _payloads.Count - 1) { _part++; Render(); } };

            var save = new Button { Text = "Save PNG...", Location = new Point(196, 8), Size = new Size(110, 30) };
            save.Click += (s, e) => SavePng();

            var print = new Button { Text = "Print", Location = new Point(312, 8), Size = new Size(90, 30) };
            print.Click += (s, e) => PrintCode();

            var copy = new Button { Text = "Copy payload", Location = new Point(408, 8), Size = new Size(120, 30) };
            copy.Click += (s, e) => CopyPayload();

            var close = new Button { Text = "Close", Location = new Point(534, 8), Size = new Size(90, 30) };
            close.Click += (s, e) => Close();

            buttons.Controls.AddRange(new Control[] { _previous, _next, save, print, copy, close });

            Controls.Add(_picture);
            Controls.Add(_detail);
            Controls.Add(buttons);
            Controls.Add(_caption);
            Controls.Add(top);

            Rebuild();
        }

        private static int IndexForEcc(QrEcc ecc)
        {
            return (int)ecc;
        }

        private QrEcc SelectedEcc()
        {
            return (QrEcc)Math.Max(0, Math.Min(3, _ecc.SelectedIndex));
        }

        private void Rebuild()
        {
            try
            {
                if (_scope.SelectedIndex == 1)
                {
                    string[] values;
                    _hourData.TryGetValue(_currentHour, out values);
                    _payloads = _builder.BuildHour(_sessionDate, _currentHour, values ?? new string[0], SelectedEcc());
                }
                else
                {
                    _payloads = _builder.BuildDay(_sessionDate, _hoursInOrder, _hourData, _skippedHours, SelectedEcc());
                }
            }
            catch (Exception ex)
            {
                _payloads = new List<string>();
                MessageBox.Show(ex.Message, "QR handoff", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _part = 0;
            Render();
        }

        private void Render()
        {
            Image previous = _picture.Image;
            _picture.Image = null;
            if (previous != null) previous.Dispose();

            if (_payloads.Count == 0 || string.IsNullOrEmpty(PayloadBody()))
            {
                _caption.Text = "Nothing to send yet";
                _detail.Text = "No hour on this sheet has any values.";
                _previous.Enabled = _next.Enabled = false;
                return;
            }

            string payload = _payloads[_part];

            try
            {
                QrCode code = QrCode.Encode(payload, SelectedEcc());

                int box = Math.Max(160, Math.Min(_picture.ClientSize.Width, _picture.ClientSize.Height) - 16);
                _picture.Image = code.ToBitmap(code.ModuleSizeFor(box));

                string subject = _settings.SubstationCode + "   " +
                                 _sessionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) +
                                 (_scope.SelectedIndex == 1
                                     ? "   hour " + _currentHour.ToString("00", CultureInfo.InvariantCulture)
                                     : "");

                _caption.Text = _payloads.Count > 1
                    ? string.Format(CultureInfo.InvariantCulture, "{0}   —   part {1} of {2}, scan every part",
                                    subject, _part + 1, _payloads.Count)
                    : subject;

                _detail.Text = string.Format(CultureInfo.InvariantCulture,
                    "{0} bytes · QR version {1} ({2}×{2} modules) · error correction {3}\r\n{4}",
                    payload.Length, code.Version, code.Size, SelectedEcc(),
                    _payloads.Count > 1
                        ? "The app accepts the day once all " + _payloads.Count + " parts are scanned."
                        : "Scan with the substation logsheet app.");
            }
            catch (Exception ex)
            {
                _caption.Text = "Cannot build this code";
                _detail.Text = ex.Message;
            }

            _previous.Enabled = _part > 0;
            _next.Enabled = _part < _payloads.Count - 1;
        }

        private string PayloadBody()
        {
            if (_payloads.Count == 0) return "";

            string first = _payloads[0];
            int lastBar = first.LastIndexOf('|');
            return lastBar < 0 ? first : first.Substring(lastBar + 1);
        }

        private void SavePng()
        {
            if (_picture.Image == null) return;

            using (var dialog = new SaveFileDialog
            {
                Filter = "PNG image|*.png",
                FileName = string.Format(CultureInfo.InvariantCulture, "logsheet-{0}{1}.png",
                    _sessionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    _payloads.Count > 1 ? "-part" + (_part + 1).ToString(CultureInfo.InvariantCulture) : "")
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    _picture.Image.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "QR handoff", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void PrintCode()
        {
            if (_picture.Image == null) return;

            using (var document = new PrintDocument())
            {
                Image image = _picture.Image;
                document.PrintPage += (s, e) =>
                {
                    int side = Math.Min(e.MarginBounds.Width, e.MarginBounds.Height);
                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    e.Graphics.DrawImage(image, e.MarginBounds.Left, e.MarginBounds.Top, side, side);
                };

                using (var preview = new PrintPreviewDialog { Document = document, WindowState = FormWindowState.Maximized })
                    preview.ShowDialog(this);
            }
        }

        private void CopyPayload()
        {
            if (_payloads.Count == 0) return;

            try { Clipboard.SetText(_payloads[_part]); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "QR handoff"); }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_picture.Image != null) { _picture.Image.Dispose(); _picture.Image = null; }
            base.OnFormClosed(e);
        }
    }
}
