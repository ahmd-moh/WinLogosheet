using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using WinLogosheet.V2;

namespace WinLogosheet
{
    /// <summary>
    /// V2 additions to the main form: pull each hour from the two OCR agents
    /// over the socket link instead of screenshotting locally, and hand the day
    /// to the Android app as a QR code.
    ///
    /// Kept in its own partial file so the V1 designer layout is untouched — the
    /// V2 strip builds itself at run time in the gap between the section
    /// checkboxes and the Start button.
    /// </summary>
    public partial class Form1
    {
        private V2Settings _v2Settings;
        private HourCollector _v2Collector;
        private System.Windows.Forms.Timer _v2Timer;
        private V2StatusForm _v2Status;
        private CollectResult _v2LastResult;
        private string _v2LastCollectedSlot = "";

        private Panel _v2Panel;
        private Label _v2Indicator;
        private Button _v2CollectButton;
        private ToolTip _v2Tips;

        /// <summary>Called from the Form1 constructor, after the V1 set-up.</summary>
        private void InitializeV2()
        {
            try
            {
                _v2Settings = V2Settings.Load();
            }
            catch (Exception ex)
            {
                // A broken settings file must not stop the operator opening the
                // logsheet: V2 simply stays off and says why.
                _v2Settings = null;
                SetStatus("V2 disabled — " + V2Settings.FileName + ": " + ex.Message, 6000);
                return;
            }

            if (!_v2Settings.Enabled) return;

            _v2Collector = new HourCollector(_v2Settings);
            BuildV2Strip();

            _v2Timer = new System.Windows.Forms.Timer { Interval = 30_000 };
            _v2Timer.Tick += V2Timer_Tick;
            _v2Timer.Start();
        }

        private void BuildV2Strip()
        {
            _v2Panel = new Panel
            {
                Location = new Point(1190, 730),
                Size = new Size(400, 34),
                BackColor = Color.Transparent
            };

            _v2Indicator = new Label
            {
                Text = "V2",
                Location = new Point(0, 7),
                Size = new Size(30, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = Color.Gainsboro,
                ForeColor = Color.DimGray,
                BorderStyle = BorderStyle.FixedSingle
            };

            _v2CollectButton = new Button
            {
                Text = "Collect hour",
                Location = new Point(34, 3),
                Size = new Size(96, 28)
            };
            _v2CollectButton.Click += (s, e) => CollectCurrentHour(true, true);

            var backfill = new Button { Text = "Backfill day", Location = new Point(134, 3), Size = new Size(90, 28) };
            backfill.Click += (s, e) => BackfillDay();

            var agents = new Button { Text = "Agents…", Location = new Point(228, 3), Size = new Size(76, 28) };
            agents.Click += (s, e) => ShowV2Status();

            var qr = new Button { Text = "QR → Mobile", Location = new Point(308, 3), Size = new Size(92, 28) };
            qr.Click += (s, e) => ShowQr();

            _v2Panel.Controls.AddRange(new Control[] { _v2Indicator, _v2CollectButton, backfill, agents, qr });

            Controls.Add(_v2Panel);
            _v2Panel.BringToFront();

            _v2Tips = new ToolTip();
            _v2Tips.SetToolTip(_v2CollectButton,
                "Ask both servers to read this hour again now. Replaces the values on this row.");
            _v2Tips.SetToolTip(backfill, "Pull every hour both servers hold for today, filling only the gaps.");
            _v2Tips.SetToolTip(agents, "Connection state, and where each column's value came from.");
            _v2Tips.SetToolTip(qr, "Show the day as a QR code for the Android app.");
        }

        // ── Automatic collection ───────────────────────────────────────────

        /// <summary>
        /// Once an hour, a minute after the agents take their reading. The slot
        /// guard means a restart mid-hour re-collects at most once, and a
        /// long-running session never collects the same hour twice.
        /// </summary>
        private void V2Timer_Tick(object sender, EventArgs e)
        {
            if (_v2Settings == null || !_v2Settings.AutoCollect) return;

            DateTime now = DateTime.Now;
            if (now.Minute < _v2Settings.CollectMinute) return;

            int hour = now.Hour == 0 ? 24 : now.Hour;
            string slot = SessionDateString() + "/" + hour.ToString("00", CultureInfo.InvariantCulture);
            if (slot == _v2LastCollectedSlot) return;

            _v2LastCollectedSlot = slot;
            CollectHour(hour, false, false);
        }

        /// <summary>Workday start. Hours 8-24 belong to the session date, hours
        /// 1-7 to the morning after — the same rule the agents apply.</summary>
        private const int V2WorkdayStartHour = 8;

        /// <summary>
        /// The session date to ask the agents for.
        ///
        /// V1's GetSessionDate reads the scheduled capture task's start boundary,
        /// which is right while that task exists. A V2-only site never registers
        /// it, so GetSessionDate falls back to today's date — which is a day late
        /// between midnight and 08:00, when the sheet still belongs to the
        /// morning it opened. Apply the workday rule to that fallback so both
        /// sides agree on which day an 02:00 reading belongs to.
        /// </summary>
        private DateTime SessionDateV2()
        {
            DateTime session = GetSessionDate();
            if (DateTime.Now.Hour < V2WorkdayStartHour && session.Date == DateTime.Now.Date)
                session = session.AddDays(-1);
            return session.Date;
        }

        private string SessionDateString()
        {
            return SessionDateV2().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        // ── Collection ─────────────────────────────────────────────────────

        private void CollectCurrentHour(bool fresh, bool report)
        {
            CollectHour(_currentHour, fresh, report);
        }

        /// <summary>
        /// Pulls one hour from the agents and writes it into the sheet.
        ///
        /// fresh=true means the operator pressed a button: the agents re-read
        /// their displays rather than returning what they stored at :02, and the
        /// answer replaces whatever is on the row. The automatic hourly pass runs
        /// with fresh=false and never overwrites a value already there.
        /// </summary>
        private CollectResult CollectHour(int hour, bool fresh, bool report)
        {
            if (_v2Collector == null) return null;

            SetIndicator(Color.Khaki, Color.Black, "…", "Collecting hour " + hour);
            SetStatus("Collecting hour " + hour.ToString("00", CultureInfo.InvariantCulture) + " from the agents…", 0);

            CollectResult result;
            using (new WaitCursorScope())
            {
                string[] existing;
                _hourData.TryGetValue(hour, out existing);
                result = _v2Collector.Collect(SessionDateString(), hour, fresh, existing, fresh);
            }

            ApplyCollected(hour, result);
            _v2LastResult = result;

            if (result.AgentErrors.Count > 0 && !result.AnyAgentReached)
                SetIndicator(Color.Firebrick, Color.White, "V2", "No agent answered");
            else if (result.CountNeedingAttention > 0 || result.AgentErrors.Count > 0)
                SetIndicator(Color.Goldenrod, Color.Black, "V2", result.Summary());
            else
                SetIndicator(Color.SeaGreen, Color.White, "V2", result.Summary());

            SetStatus(result.Summary(), 6000);

            if (_v2Status != null && !_v2Status.IsDisposed) _v2Status.Update(result);
            if (report && result.CountNeedingAttention > 0) ShowV2Status();

            return result;
        }

        /// <summary>Writes a collected row into the sheet and refreshes the UI.</summary>
        private void ApplyCollected(int hour, CollectResult result)
        {
            if (result == null) return;

            var values = new string[Substation.Shared.ColumnMap.ColumnCount];
            for (int i = 0; i < values.Length; i++) values[i] = result.Values[i] ?? "";

            _hourData[hour] = values;

            // Rebuilding the list fires SelectedIndexChanged, which would reload
            // the row underneath us and reset the caret; the same guard the
            // textbox handler uses keeps that re-entry out.
            bool previous = _isUpdatingFromListView;
            _isUpdatingFromListView = true;
            try
            {
                if (hour == _currentHour) UpdateTextboxes(values);
                UpdateListView();
            }
            finally
            {
                _isUpdatingFromListView = previous;
            }
        }

        private void BackfillDay()
        {
            if (_v2Collector == null) return;

            SetStatus("Reading every stored hour from both agents…", 0);

            Dictionary<int, CollectResult> collected;
            using (new WaitCursorScope())
                collected = _v2Collector.CollectDay(SessionDateString(), _hourData);

            int filled = 0;
            foreach (var pair in collected)
            {
                bool hasValue = false;
                foreach (string v in pair.Value.Values)
                    if (!string.IsNullOrEmpty(v)) { hasValue = true; break; }
                if (!hasValue) continue;

                ApplyCollected(pair.Key, pair.Value);
                filled++;
            }

            SelectCurrentHourInListView();
            SetStatus(filled == 0
                ? "Backfill found nothing new — the agents hold no stored hours for today."
                : "Backfill filled " + filled + " hour(s) from the agents.", 6000);
        }

        // ── Windows ────────────────────────────────────────────────────────

        private void ShowV2Status()
        {
            if (_v2Settings == null) return;

            if (_v2Status == null || _v2Status.IsDisposed)
            {
                _v2Status = new V2StatusForm(_v2Settings, _v2LastResult);
                _v2Status.ReReadRequested += fresh => CollectHour(_currentHour, fresh, false);
            }

            _v2Status.Show(this);
            _v2Status.BringToFront();
        }

        private void ShowQr()
        {
            if (_v2Settings == null)
            {
                MessageBox.Show("V2 is not configured — " + V2Settings.FileName + " could not be read.",
                                "QR handoff", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var form = new QrForm(_v2Settings, SessionDateV2(), _hourSeq,
                                         _hourData, _skippedHours, _currentHour))
                form.ShowDialog(this);
        }

        private void SetIndicator(Color back, Color fore, string text, string tooltip)
        {
            if (_v2Indicator == null) return;

            _v2Indicator.BackColor = back;
            _v2Indicator.ForeColor = fore;
            _v2Indicator.Text = text;

            if (_v2Tips != null) _v2Tips.SetToolTip(_v2Indicator, tooltip);
        }

        private void DisposeV2()
        {
            if (_v2Timer != null) { _v2Timer.Stop(); _v2Timer.Dispose(); _v2Timer = null; }
            if (_v2Status != null && !_v2Status.IsDisposed) { _v2Status.Close(); _v2Status = null; }
            if (_v2Tips != null) { _v2Tips.Dispose(); _v2Tips = null; }
        }
    }
}
