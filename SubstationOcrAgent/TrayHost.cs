using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Substation.Shared;

namespace SubstationOcrAgent
{
    /// <summary>
    /// The agent's only user interface: a tray icon on the SCADA server. There is
    /// nothing to operate day to day — it exists so the engineer on site can see
    /// the agent is alive, force a reading, and open the calibration overlay.
    /// </summary>
    public sealed class TrayHost : IDisposable
    {
        private readonly AgentConfig _config;
        private readonly CaptureService _capture;
        private readonly AgentServer _server;
        private readonly HourStore _store;
        private readonly AgentLog _log;

        private NotifyIcon _icon;
        private LogWindow _logWindow;

        public TrayHost(AgentConfig config, CaptureService capture, AgentServer server,
                        HourStore store, AgentLog log)
        {
            _config = config;
            _capture = capture;
            _server = server;
            _store = store;
            _log = log;
        }

        public void Show()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Read this hour now", null, (s, e) => CaptureNow());
            menu.Items.Add("Show last reading...", null, (s, e) => ShowLastReading());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Write calibration overlay", null, (s, e) => Calibrate());
            menu.Items.Add("Reload ROI configuration", null, (s, e) => ReloadRois());
            menu.Items.Add("Open agent folder", null, (s, e) => OpenFolder());
            menu.Items.Add("Show log window", null, (s, e) => ShowLog());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => Application.Exit());

            _icon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                ContextMenuStrip = menu,
                Text = Truncate(AgentInfo.Product + " - " + _config.ServerId + " :" + _config.Port)
            };
            _icon.DoubleClick += (s, e) => ShowLog();

            _log.LineWritten += line =>
            {
                if (_logWindow != null && _logWindow.IsHandleCreated) _logWindow.Append(line);
            };
        }

        // The tray tooltip is capped at 63 characters by the shell.
        private static string Truncate(string text)
        {
            return text.Length <= 63 ? text : text.Substring(0, 60) + "...";
        }

        private void CaptureNow()
        {
            DateTime now = DateTime.Now;
            string sessionDate = LogsheetHours.SessionDate(now, _config.WorkdayStartHour);
            int hour = LogsheetHours.FromClock(now);

            ReadingFrame frame = _capture.CaptureNow(sessionDate, hour);
            Balloon("Hour " + hour.ToString("00", CultureInfo.InvariantCulture),
                    Describe(frame),
                    string.IsNullOrEmpty(frame.Error) ? ToolTipIcon.Info : ToolTipIcon.Error);
        }

        private void ShowLastReading()
        {
            DateTime now = DateTime.Now;
            string sessionDate = LogsheetHours.SessionDate(now, _config.WorkdayStartHour);
            int hour = LogsheetHours.FromClock(now);

            ReadingFrame frame = _store.Load(sessionDate, hour);
            if (frame == null)
            {
                MessageBox.Show("Nothing stored yet for " + sessionDate + " hour " +
                                hour.ToString("00", CultureInfo.InvariantCulture) + ".",
                                AgentInfo.Product, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine(sessionDate + "  hour " + hour.ToString("00", CultureInfo.InvariantCulture));
            sb.AppendLine("captured " + frame.CapturedUtc.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture));
            sb.AppendLine();
            foreach (ChannelReading c in frame.Channels)
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-18} {1,10}   {2:0}%",
                                            c.Key, string.IsNullOrEmpty(c.Value) ? "-" : c.Value, c.Confidence));

            MessageBox.Show(sb.ToString(), AgentInfo.Product + " - last reading",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string Describe(ReadingFrame frame)
        {
            if (!string.IsNullOrEmpty(frame.Error)) return frame.Error;

            int ok = 0;
            foreach (ChannelReading c in frame.Channels) if (c.Ok) ok++;
            return string.Format(CultureInfo.InvariantCulture, "{0} of {1} value(s) read, mean confidence {2:0}%",
                                 ok, frame.Channels.Count, frame.MeanConfidence);
        }

        private void Calibrate()
        {
            try
            {
                string path = _capture.WriteCalibrationOverlay();
                if (MessageBox.Show("Overlay written to:\n" + path + "\n\nOpen it now?",
                                    AgentInfo.Product, MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Information) == DialogResult.Yes)
                    Process.Start(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AgentInfo.Product, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReloadRois()
        {
            try
            {
                _capture.ReloadRois();
                Balloon(AgentInfo.Product, "ROI configuration reloaded.", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AgentInfo.Product, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenFolder()
        {
            try { Process.Start(_config.BaseFolder); }
            catch (Exception ex) { MessageBox.Show(ex.Message, AgentInfo.Product); }
        }

        private void ShowLog()
        {
            if (_logWindow == null || _logWindow.IsDisposed) _logWindow = new LogWindow(_config, _server, _capture);
            _logWindow.Show();
            _logWindow.BringToFront();
        }

        private void Balloon(string title, string text, ToolTipIcon icon)
        {
            if (_icon == null) return;
            _icon.BalloonTipTitle = title;
            _icon.BalloonTipText = text;
            _icon.BalloonTipIcon = icon;
            _icon.ShowBalloonTip(5000);
        }

        public void Dispose()
        {
            if (_icon != null)
            {
                _icon.Visible = false;
                _icon.Dispose();
                _icon = null;
            }
        }
    }

    /// <summary>Live log tail plus a one-glance health summary.</summary>
    internal sealed class LogWindow : Form
    {
        private readonly TextBox _text;
        private readonly Label _summary;
        private readonly AgentConfig _config;
        private readonly AgentServer _server;
        private readonly CaptureService _capture;

        public LogWindow(AgentConfig config, AgentServer server, CaptureService capture)
        {
            _config = config;
            _server = server;
            _capture = capture;

            Text = AgentInfo.Product + " - " + config.ServerId;
            ClientSize = new Size(820, 460);
            StartPosition = FormStartPosition.CenterScreen;
            MinimizeBox = true;

            _summary = new Label { Dock = DockStyle.Top, Height = 60, Padding = new Padding(8, 8, 8, 0) };
            _text = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = false,
                Font = new Font("Consolas", 9)
            };

            Controls.Add(_text);
            Controls.Add(_summary);

            var refresh = new Timer { Interval = 2000 };
            refresh.Tick += (s, e) => RefreshSummary();
            refresh.Start();

            RefreshSummary();
            LoadToday();
        }

        private void RefreshSummary()
        {
            _summary.Text = string.Format(CultureInfo.InvariantCulture,
                "{0}  |  listening {1}:{2}  |  ROIs {3}  |  clients served {4}\r\n" +
                "last capture {5}  |  last error: {6}",
                _config.ServerId, _config.ListenAddress, _config.Port,
                CountEnabled(), _server.ConnectionsServed,
                _capture.LastCaptureLocal == default(DateTime)
                    ? "none yet"
                    : _capture.LastCaptureLocal.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                string.IsNullOrEmpty(_capture.LastError) ? "none" : _capture.LastError);
        }

        private int CountEnabled()
        {
            int n = 0;
            foreach (var roi in _capture.Rois.EnabledRois()) n++;
            return n;
        }

        private void LoadToday()
        {
            try
            {
                string path = Path.Combine(_config.ResolvePath(_config.LogFolder),
                    "agent-" + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".log");
                if (!File.Exists(path)) return;

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                    _text.Text = reader.ReadToEnd();

                _text.SelectionStart = _text.TextLength;
                _text.ScrollToCaret();
            }
            catch { }
        }

        public void Append(string line)
        {
            if (InvokeRequired) { BeginInvoke((Action<string>)Append, line); return; }

            _text.AppendText(line + Environment.NewLine);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Closing the window must not close the agent.
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            base.OnFormClosing(e);
        }
    }
}
