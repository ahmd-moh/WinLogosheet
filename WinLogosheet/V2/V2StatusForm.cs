using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace WinLogosheet.V2
{
    /// <summary>
    /// What the two servers just sent, column by column.
    ///
    /// The operator's question after a collect is always the same — which values
    /// came from where, and which ones should I check against the screen. This
    /// window answers both, and lets them re-read an hour without leaving it.
    /// </summary>
    public sealed class V2StatusForm : Form
    {
        private readonly V2Settings _settings;
        private readonly ListView _agents;
        private readonly ListView _columns;
        private readonly Label _summary;

        private CollectResult _result;

        /// <summary>Raised when the operator asks for a fresh read; the form
        /// hands back the collected row for Form1 to apply.</summary>
        public event Func<bool, CollectResult> ReReadRequested;

        public V2StatusForm(V2Settings settings, CollectResult result)
        {
            _settings = settings;
            _result = result;

            Text = "V2 collector — agents and columns";
            ClientSize = new Size(900, 640);
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(760, 480);

            _summary = new Label
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(10, 8, 10, 0),
                Font = new Font("Segoe UI", 9.5f)
            };

            var agentBox = new GroupBox { Dock = DockStyle.Top, Height = 150, Text = "Agents", Padding = new Padding(8) };
            _agents = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            _agents.Columns.Add("Server", 90);
            _agents.Columns.Add("Endpoint", 170);
            _agents.Columns.Add("State", 110);
            _agents.Columns.Add("Detail", 480);
            agentBox.Controls.Add(_agents);

            var columnBox = new GroupBox { Dock = DockStyle.Fill, Text = "Columns", Padding = new Padding(8) };
            _columns = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            _columns.Columns.Add("#", 40);
            _columns.Columns.Add("Measurement", 130);
            _columns.Columns.Add("Value", 90);
            _columns.Columns.Add("Source", 70);
            _columns.Columns.Add("Channel", 150);
            _columns.Columns.Add("Confidence", 85);
            _columns.Columns.Add("State", 130);
            _columns.Columns.Add("Note", 240);
            columnBox.Controls.Add(_columns);

            var buttons = new Panel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(8) };

            var test = new Button { Text = "Test agents", Location = new Point(8, 8), Size = new Size(110, 30) };
            test.Click += (s, e) => TestAgents();

            var reread = new Button { Text = "Re-read this hour", Location = new Point(124, 8), Size = new Size(140, 30) };
            reread.Click += (s, e) => RaiseReRead(true);

            var calibrate = new Button { Text = "Write ROI overlay", Location = new Point(270, 8), Size = new Size(140, 30) };
            calibrate.Click += (s, e) => Calibrate();

            var evidence = new Button { Text = "Show ROI image", Location = new Point(416, 8), Size = new Size(130, 30) };
            evidence.Click += (s, e) => ShowRoiEvidence();

            var close = new Button { Text = "Close", Location = new Point(556, 8), Size = new Size(90, 30) };
            close.Click += (s, e) => Close();

            buttons.Controls.AddRange(new Control[] { test, reread, calibrate, evidence, close });

            Controls.Add(columnBox);
            Controls.Add(agentBox);
            Controls.Add(buttons);
            Controls.Add(_summary);

            Populate();
        }

        public void Update(CollectResult result)
        {
            _result = result;
            Populate();
        }

        private void Populate()
        {
            _summary.Text = _result == null
                ? "No collection has run yet."
                : string.Format(CultureInfo.InvariantCulture,
                    "{0}  hour {1:00}   ·   collected {2:HH:mm:ss}   ·   {3}",
                    _result.SessionDate, _result.Hour, _result.CollectedLocal, _result.Summary());

            _agents.BeginUpdate();
            _agents.Items.Clear();
            foreach (AgentEndpoint endpoint in _settings.Agents)
            {
                string state = "not polled";
                string detail = endpoint.Enabled ? "" : "disabled in winlogosheet.v2.json";

                if (_result != null)
                {
                    string error;
                    if (_result.AgentErrors.TryGetValue(endpoint.Id, out error)) { state = "error"; detail = error; }
                    else if (endpoint.Enabled) { state = "answered"; detail = "values received"; }
                }

                var item = new ListViewItem(endpoint.Id);
                item.SubItems.Add(endpoint.Host + ":" + endpoint.Port.ToString(CultureInfo.InvariantCulture));
                item.SubItems.Add(state);
                item.SubItems.Add(detail);
                item.ForeColor = state == "error" ? Color.Firebrick
                               : state == "answered" ? Color.DarkGreen : Color.DimGray;
                item.Tag = endpoint;
                _agents.Items.Add(item);
            }
            _agents.EndUpdate();

            _columns.BeginUpdate();
            _columns.Items.Clear();
            if (_result != null)
            {
                foreach (ColumnResult column in _result.Columns)
                {
                    var item = new ListViewItem(column.Column.ToString(CultureInfo.InvariantCulture));
                    item.SubItems.Add(column.Label);
                    item.SubItems.Add(string.IsNullOrEmpty(column.Value) ? "—" : column.Value);
                    item.SubItems.Add(column.SourceId);
                    item.SubItems.Add(column.Channel);
                    item.SubItems.Add(column.Confidence > 0
                        ? column.Confidence.ToString("0", CultureInfo.InvariantCulture) + "%" : "");
                    item.SubItems.Add(Describe(column.Status));
                    item.SubItems.Add(column.Note);
                    item.BackColor = ColourFor(column.Status);
                    _columns.Items.Add(item);
                }
            }
            _columns.EndUpdate();
        }

        private static string Describe(ColumnStatus status)
        {
            switch (status)
            {
                case ColumnStatus.Ok: return "ok";
                case ColumnStatus.LowConfidence: return "check — low confidence";
                case ColumnStatus.OutOfRange: return "check — out of range";
                case ColumnStatus.Missing: return "not read";
                case ColumnStatus.AgentUnreachable: return "agent unreachable";
                case ColumnStatus.NoBinding: return "not mapped";
                case ColumnStatus.KeptManualEdit: return "kept existing value";
                default: return "";
            }
        }

        private static Color ColourFor(ColumnStatus status)
        {
            switch (status)
            {
                case ColumnStatus.Ok: return Color.FromArgb(226, 246, 226);
                case ColumnStatus.KeptManualEdit: return Color.FromArgb(228, 238, 252);
                case ColumnStatus.LowConfidence: return Color.FromArgb(255, 245, 190);
                case ColumnStatus.OutOfRange: return Color.FromArgb(255, 214, 160);
                default: return Color.FromArgb(255, 216, 216);
            }
        }

        private void RaiseReRead(bool fresh)
        {
            Func<bool, CollectResult> handler = ReReadRequested;
            if (handler == null) return;

            using (new WaitCursorScope())
            {
                CollectResult result = handler(fresh);
                if (result != null) Update(result);
            }
        }

        private void TestAgents()
        {
            using (new WaitCursorScope())
            {
                _agents.BeginUpdate();
                foreach (ListViewItem item in _agents.Items)
                {
                    var endpoint = item.Tag as AgentEndpoint;
                    if (endpoint == null) continue;

                    try
                    {
                        var client = new RemoteAgentClient(endpoint, _settings.SharedSecret, _settings.TimeoutMs);
                        AgentIdentity identity = client.Hello();

                        item.SubItems[2].Text = "answered";
                        item.SubItems[3].Text = string.Format(CultureInfo.InvariantCulture,
                            "{0} v{1} on {2} — {3}/{4} ROI(s) enabled, {5} channel(s)",
                            identity.DisplayName, identity.AgentVersion, identity.Machine,
                            identity.EnabledRois, identity.TotalRois, identity.Channels.Count);
                        item.ForeColor = Color.DarkGreen;
                    }
                    catch (Exception ex)
                    {
                        item.SubItems[2].Text = "error";
                        item.SubItems[3].Text = ex.Message;
                        item.ForeColor = Color.Firebrick;
                    }
                }
                _agents.EndUpdate();
            }
        }

        private AgentEndpoint SelectedAgent()
        {
            if (_agents.SelectedItems.Count > 0) return _agents.SelectedItems[0].Tag as AgentEndpoint;

            MessageBox.Show("Select an agent in the list first.", "V2 collector",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        private void Calibrate()
        {
            AgentEndpoint endpoint = SelectedAgent();
            if (endpoint == null) return;

            try
            {
                using (new WaitCursorScope())
                {
                    var client = new RemoteAgentClient(endpoint, _settings.SharedSecret, _settings.TimeoutMs);
                    string path = client.Calibrate();

                    MessageBox.Show(
                        "The agent wrote an overlay of its ROI boxes to:\n\n" + path +
                        "\n\nOpen it on that server to check the boxes still sit on the right panels.",
                        "V2 collector", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "V2 collector", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowRoiEvidence()
        {
            if (_columns.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select the column you want to look into first.", "V2 collector",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ListViewItem row = _columns.SelectedItems[0];
            string serverId = row.SubItems[3].Text;
            string channel = row.SubItems[4].Text;

            AgentEndpoint endpoint = _settings.FindAgent(serverId);
            if (endpoint == null)
            {
                MessageBox.Show("Column " + row.Text + " is not bound to a configured agent.", "V2 collector");
                return;
            }

            // "T1.MW" -> ROI "T1"
            int dot = channel.LastIndexOf('.');
            string roiId = dot < 0 ? channel : channel.Substring(0, dot);

            try
            {
                using (new WaitCursorScope())
                {
                    var client = new RemoteAgentClient(endpoint, _settings.SharedSecret, _settings.TimeoutMs);
                    byte[] png = client.RoiImage(roiId, true);

                    using (var stream = new System.IO.MemoryStream(png))
                    using (Image image = Image.FromStream(stream))
                        ShowImage(roiId + " as the agent prepares it for OCR", new Bitmap(image));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "V2 collector", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowImage(string title, Bitmap image)
        {
            var viewer = new Form
            {
                Text = title,
                ClientSize = new Size(Math.Max(320, Math.Min(1200, image.Width + 20)),
                                      Math.Max(200, Math.Min(800, image.Height + 20))),
                StartPosition = FormStartPosition.CenterParent
            };

            var box = new PictureBox { Dock = DockStyle.Fill, Image = image, SizeMode = PictureBoxSizeMode.Zoom };
            viewer.Controls.Add(box);
            viewer.FormClosed += (s, e) => image.Dispose();
            viewer.Show(this);
        }
    }

    /// <summary>Hourglass for the duration of a socket round trip.</summary>
    internal sealed class WaitCursorScope : IDisposable
    {
        private readonly Cursor _previous;

        public WaitCursorScope()
        {
            _previous = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
        }

        public void Dispose()
        {
            Cursor.Current = _previous;
        }
    }
}
