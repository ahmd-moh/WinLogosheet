using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinLogosheet
{
    // ═══════════════════════════════════════════════════════════════════════
    //  QR POPUP
    //  Modal window that renders the current logsheet as an OFFLINE QR code so a
    //  companion mobile app can scan it and rebuild the grid exactly as shown on
    //  the PC. Built entirely in code (no designer) — it is generated fresh from
    //  the payload the caller passes in each time it is opened.
    // ═══════════════════════════════════════════════════════════════════════
    public class QrCodeForm : Form
    {
        private readonly string _payload;
        private readonly PictureBox _pic;
        private readonly Label _info;
        private Bitmap _qr;

        public QrCodeForm(string payload, string title)
        {
            _payload = payload ?? string.Empty;

            Text = title ?? "Send to Device";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            ShowIcon = false;
            ClientSize = new Size(540, 620);
            MinimumSize = new Size(380, 460);

            _info = new Label
            {
                Dock = DockStyle.Top,
                Height = 48,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.75f)
            };

            _pic = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                Padding = new Padding(14)
            };

            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8)
            };
            var btnClose = new Button { Text = "Close", Width = 92, Height = 30 };
            var btnSave = new Button { Text = "Save PNG…", Width = 104, Height = 30 };
            var btnCopy = new Button { Text = "Copy Text", Width = 96, Height = 30 };
            btnClose.Click += (s, e) => Close();
            btnSave.Click += (s, e) => SavePng();
            btnCopy.Click += (s, e) => CopyText();
            bar.Controls.Add(btnClose);
            bar.Controls.Add(btnSave);
            bar.Controls.Add(btnCopy);

            // Add fill first so it docks between the top label and bottom bar.
            Controls.Add(_pic);
            Controls.Add(_info);
            Controls.Add(bar);

            AcceptButton = btnClose;

            GenerateAndShow();
        }

        private void GenerateAndShow()
        {
            try
            {
                _qr = LogsheetQr.Generate(_payload, 8, out string ecc);
                _pic.Image = _qr;
                _info.Text =
                    $"Scan with the companion app   •   {_payload.Length} characters" +
                    $"   •   error-correction {ecc}";
                _info.ForeColor = Color.FromArgb(30, 110, 30);
            }
            catch (Exception ex)
            {
                _pic.Image = null;
                _info.Text = "Could not build QR: " + ex.Message;
                _info.ForeColor = Color.Firebrick;
            }
        }

        private void CopyText()
        {
            try
            {
                if (!string.IsNullOrEmpty(_payload))
                    Clipboard.SetText(_payload);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not copy text:\n" + ex.Message, Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SavePng()
        {
            if (_qr == null) return;
            using (var dlg = new SaveFileDialog
            {
                Filter = "PNG image (*.png)|*.png",
                FileName = "logsheet-qr.png"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    _qr.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not save image:\n" + ex.Message, Text,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _pic.Image = null;
            _qr?.Dispose();
            _qr = null;
            base.OnFormClosed(e);
        }
    }
}
