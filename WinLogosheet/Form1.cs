using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Timers;
using System.Windows.Forms;
using Tesseract;
using ExcelInterop = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using Microsoft.Win32.TaskScheduler;

namespace WinLogosheet
{
    public partial class Form1 : Form
    {
        private FormFiller formFiller;
        private const int DefaultColumnWidth = 100;
        private string _tessDataPath;
        private int _currentHour = 8;
        private string _imageFolderPath = "";
        private System.Timers.Timer _statusTimer;
        private Label _statusLabel;
        private Dictionary<int, string[]> _hourData = new Dictionary<int, string[]>();
        private bool _isUpdatingFromListView = false;
        private System.Windows.Forms.Timer _printEnableTimer;
        private const string TemplateFileName = "Exact_Substation_Log_v4.xlsx";
        private readonly Rectangle _captureRegion = new Rectangle(50, 192, 50, 530);

        private readonly int[][] _groupColumns = new int[][]
        {
            new int[] { 1,2,3,4 }, new int[] { 5,6,7,8 },
            new int[] { 9,10,11,12 }, new int[] { 13,14,15,16 },
            new int[] { 17,18,19,20 }, new int[] { 21,22,23,24 }
        };

        // ── Header height ─────────────────────────────────────────────────
        private const int HEADER_H = 26;   // px — single row header

        // ── Group / sub-column name helpers ──────────────────────────────
        // Group name: only the FIRST col of each group returns a name (others return "")
        // so the top-row text is drawn only once per group without spanning.
        private static string GetGroupName(int col)
        {
            switch (col)
            {
                case 0: return "Hrs";
                case 1: return "Yarmja";
                case 5: return "Qayra";
                case 9: return "T1";
                case 13: return "T2";
                case 17: return "T3";
                case 21: return "Domez";
                case 22: return "Summer";
                case 23: return "Salam 1";
                case 24: return "Salam 2";
                default: return "";
            }
        }

        private static string GetSubName(int col)
        {
            if (col <= 0) return "Hrs";
            // Feeders (21-24) have individual names – sub-row shows "WM"
            if (col >= 21) return "WM";
            switch ((col - 1) % 4)
            {
                case 0: return "KV";
                case 1: return "A";
                case 2: return "MW";
                default: return "MV";
            }
        }

        private static bool IsGroupStart(int col)
            => col == 0 || col == 1 || col == 5 || col == 9 ||
               col == 13 || col == 17 || col == 21 || col == 22 ||
               col == 23 || col == 24;

        // ── P/Invoke ──────────────────────────────────────────────────────
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr h, IntPtr hAfter,
            int X, int Y, int cx, int cy, uint f);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr h, out RECT rc);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr h, string a, string b);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        private const int LVM_GETHEADER = 0x101F;
        private const int WM_SIZE = 0x0005;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;

        private void EnsureHeaderHeight()
        {
            if (!listView1.IsHandleCreated) return;

            IntPtr hdr = SendMessage(listView1.Handle, LVM_GETHEADER,
                                     IntPtr.Zero, IntPtr.Zero);
            if (hdr == IntPtr.Zero) return;

            // Remove Aero visual style so our owner-draw is fully in control
            SetWindowTheme(hdr, " ", " ");

            RECT rc;
            GetWindowRect(hdr, out rc);
            int w = rc.Right - rc.Left;

            // Resize the header window
            SetWindowPos(hdr, IntPtr.Zero, 0, 0, w, HEADER_H,
                SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);

            // Tell the ListView about the new size so items reflow correctly
            // (prevents items from hiding under the taller header on scroll)
            int wl = listView1.Width, hl = listView1.Height;
            SendMessage(listView1.Handle, WM_SIZE, IntPtr.Zero,
                (IntPtr)(hl << 16 | wl));

            listView1.Invalidate(true);
        }

        // ── Header colours ────────────────────────────────────────────────
        private Color GroupTopColor(int col)
        {
            if (col == 0) return Color.FromArgb(200, 200, 200);
            Color c = _groupColors[GetColorIndexForColumn(col)];
            return Color.FromArgb(
                Math.Max(0, c.R - 35),
                Math.Max(0, c.G - 35),
                Math.Max(0, c.B - 35));
        }

        private Color GroupBotColor(int col)
            => col == 0 ? Color.FromArgb(225, 225, 225)
                        : _groupColors[GetColorIndexForColumn(col)];

        // ── Fixed fonts for the header (small = fit in 20px half-row) ────
        private static readonly Font _hdrTop = new Font("Segoe UI", 11.0f, FontStyle.Bold);
        private static readonly Font _hdrBot = new Font("Segoe UI", 11.5f, FontStyle.Regular);
        private static readonly Font _cellFont = new Font("Segoe UI", 11.5f);

        // ═══════════════════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════════
        public Form1()
        {
            InitializeComponent();
#if DEBUG
            _tessDataPath = @"C:\Program Files\Tesseract-OCR\tessdata";
#else
            _tessDataPath = @"C:\Users\DCS_User\AppData\Local\Programs\Tesseract-OCR\tessdata";
#endif
            ExcelPackage.License.SetNonCommercialPersonal("My Name");
            colorDialog1.Color = Color.Gray;
            InitializeListView();

            _statusTimer = new System.Timers.Timer();
            _statusTimer.Elapsed += OnStatusTimerElapsed;
            _statusTimer.AutoReset = false;

            if (toolStripStatusLabel1 == null)
            {
                _statusLabel = new Label
                {
                    Text = "Ready",
                    ForeColor = Color.Green,
                    Dock = DockStyle.Bottom,
                    Height = 20,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                Controls.Add(_statusLabel);
            }

            _imageFolderPath = Path.Combine(Application.StartupPath,
                $"Screenshots-{DateTime.Now:yyyy-MM-dd}");

            LoadCurrentImage();
            SetStatus("Ready", 2000);

            _printEnableTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
            _printEnableTimer.Tick += (s, e) => UpdatePrintButtonEnabled();
            _printEnableTimer.Start();
            UpdatePrintButtonEnabled();
            UpdateColumnVisibility();


            button_new.Text = "New   يوم جديد ";
        }

        // ═══════════════════════════════════════════════════════════════════
        //  LISTVIEW SETUP
        // ═══════════════════════════════════════════════════════════════════
        private void InitializeListView()
        {
            listView1.View = View.Details;
            listView1.GridLines = true;
            listView1.FullRowSelect = true;
            listView1.MultiSelect = false;
            listView1.OwnerDraw = true;

            // NO Padding, NO ROW_TOP_MARGIN — they break scrolling

            var ar = new CultureInfo("ar-IQ");
            lable_date.Text =
                $"التاريخ : {DateTime.Now.ToString("yyyy/MM/dd", ar)}" +
                $" - {DateTime.Now.ToString("dddd", ar)}";

            listView1.Items.Clear();
            listView1.SelectedIndexChanged += ListView1_SelectedIndexChanged;
            listView1.DrawColumnHeader += ListView1_DrawColumnHeader;
            listView1.DrawSubItem += ListView1_DrawSubItem;
            listView1.DrawItem += ListView1_DrawItem;

            // Apply custom header height once the native handle is ready
            listView1.HandleCreated += (s, e) => BeginInvoke(
                new System.Action(EnsureHeaderHeight));

            // Re-apply after the ListView is resized (scroll bars appearing
            // can reset the header to its default height)
            listView1.SizeChanged += (s, e) =>
            {
                if (listView1.IsHandleCreated)
                    BeginInvoke(new System.Action(EnsureHeaderHeight));
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        //  TWO-ROW COLUMN HEADER DRAWING
        //  Group name in TOP half (bold, only first col of each group).
        //  Sub-column name in BOTTOM half.
        //  Both drawn with DrawString using exact Y from font metrics —
        //  never relies on TextRenderer which ignores font ascent.
        // ═══════════════════════════════════════════════════════════════════
        private void ListView1_DrawColumnHeader(object sender,
                                                 DrawListViewColumnHeaderEventArgs e)
        {
            int col = e.ColumnIndex;

            // ── Single-row background ─────────────────────────────────────
            // First col of each group gets the darker shade, rest get group colour
            Color back = IsGroupStart(col) && col > 0
                ? GroupTopColor(col)
                : GroupBotColor(col);
            if (col == 0) back = Color.FromArgb(215, 215, 215);

            using (var br = new SolidBrush(back))
                e.Graphics.FillRectangle(br, e.Bounds);

            // ── Label: group name on first col, sub-name on the rest ──────
            // Feeders (21-24) always show their own name (Domez, Summer…)
            string label;
            if (col >= 21)
                label = GetGroupName(col);          // Domez / Summer / Salam 1 / Salam 2
            else
                label = IsGroupStart(col)
                    ? GetGroupName(col)             // Yarmja / Qayra / T1 / T2 / T3
                    : GetSubName(col);              // KV / A / MW / MV

            if (!string.IsNullOrEmpty(label))
            {
                float fh = _hdrTop.GetHeight(e.Graphics);
                // Use HEADER_H (our real height) — e.Bounds.Height may report old default
                float y = e.Bounds.Top + Math.Max(0f, (HEADER_H - fh) / 2f);
                e.Graphics.DrawString(label, _hdrTop, Brushes.Black,
                    e.Bounds.Left + 4f, y);
            }

            // ── Cell border ───────────────────────────────────────────────
            e.Graphics.DrawRectangle(Pens.Gray, e.Bounds);

            // ── Thick left border on first col of each group ──────────────
            if (IsGroupStart(col) && col > 0)
                using (var p = new Pen(Color.DimGray, 2))
                    e.Graphics.DrawLine(p,
                        e.Bounds.Left, e.Bounds.Top,
                        e.Bounds.Left, e.Bounds.Bottom);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  VALIDATION COLOURS
        //  Rules (applied only when the row has at least one non-empty value):
        //  1. Empty cell                        → RED   (missing reading)
        //  2. Yarmja/Qayra KV outside 120–145   → YELLOW (expected ~132 kV)
        //  3. T1/T2/T3 KV outside 28–40         → YELLOW (expected ~33 kV)
        //  4. |MVAR| > 100                       → ORANGE (abnormally high reactive power)
        //  5. A < 0  or  |MW| > 800              → YELLOW (suspect reading)
        //  6. Feeder WM (col 21-24) < 0          → YELLOW (negative watt-meter reading)
        //  Selected rows always use system highlight.
        // ═══════════════════════════════════════════════════════════════════

        private static readonly Color _clrMissing = Color.FromArgb(255, 110, 110);  // red
        private static readonly Color _clrHighMvar = Color.FromArgb(255, 170, 50);  // orange
        private static readonly Color _clrSuspect = Color.FromArgb(255, 245, 100);  // yellow

        private static bool RowHasAnyData(ListViewItem item)
        {
            for (int i = 1; i < item.SubItems.Count; i++)
                if (!string.IsNullOrWhiteSpace(item.SubItems[i].Text)) return true;
            return false;
        }

        private Color GetValidationColor(int col, string value, ListViewItem item)
        {
            if (col == 0) return Color.Empty;

            bool empty = string.IsNullOrWhiteSpace(value);

            // ── 1. Missing value in a row that has other data ─────────────
            if (empty)
                return RowHasAnyData(item) ? _clrMissing : Color.Empty;

            if (!double.TryParse(value, out double val)) return Color.Empty;

            // ── Feeder columns 21-24: WM (MW) readings ────────────────────
            // Each feeder carries active power; > 500 MW or negative is suspect
            if (col >= 21)
            {
                if (val < 0 || val > 500) return _clrSuspect;

                // Feeder MW must not exceed its upstream transformer MW.
                // Topology:  Domez→T1, Summer→T2, Salam1→T2, Salam2→T3.
                Color xfmrCross = CheckFeederAgainstTransformer(col, val, item);
                if (xfmrCross != Color.Empty) return xfmrCross;

                return Color.Empty;
            }

            // Sub-column type within each 4-col group: 0=KV  1=A  2=MW  3=MVAR
            int subType = (col - 1) % 4;

            // Voltage class per section:
            //   Yarmja (1-4)  + Qayra (5-8)  → 132 kV bus → expected 118–145 kV
            //   T1 (9-12) + T2 (13-16) + T3 (17-20) → 33 kV bus → expected 28–38 kV
            bool is132kV = (col >= 1 && col <= 8);
            bool is33kV = (col >= 9 && col <= 20);

            switch (subType)
            {
                case 0: // KV
                    if (is132kV && (val < 118 || val > 145))
                        return _clrSuspect;   // out of 132 kV normal range
                    if (is33kV && (val < 28 || val > 38))
                        return _clrSuspect;   // out of 33 kV normal range
                    break;

                case 1: // A (Ampere) — negative is invalid for both voltage levels
                    if (val < 0) return _clrSuspect;
                    break;

                case 2: // MW — rated capacity limits
                    // 132 kV lines: up to ~400 MW reasonable
                    // 33 kV transformers: up to ~150 MW reasonable
                    if (is132kV && Math.Abs(val) > 400) return _clrSuspect;
                    if (is33kV && Math.Abs(val) > 150) return _clrSuspect;

                    // Topology-based conservation:
                    //   Qayra MW  ≥  T1 + T2 + T3 MW
                    //   T1 MW     ≥  Domez MW
                    //   T2 MW     ≥  Summer + Salam1 MW
                    //   T3 MW     ≥  Salam2 MW
                    Color topo = CheckTransformerLoadBalance(col, val, item);
                    if (topo != Color.Empty) return topo;
                    break;

                case 3: // MVAR — |MVAR| > 100 is abnormal on either voltage level
                    if (Math.Abs(val) > 100)
                    {
                        // Cross-check: if KV of same group is in normal range, the
                        // line is energised → likely OCR misread → orange warning.
                        // If KV is also bad → yellow (general suspect reading).
                        int kvIdx = col - 3;
                        double kv = 0;
                        if (kvIdx > 0 && kvIdx < item.SubItems.Count)
                            double.TryParse(item.SubItems[kvIdx].Text, out kv);
                        bool kvOk = is132kV ? (kv >= 118 && kv <= 145)
                                            : (kv >= 28 && kv <= 38);
                        return kvOk ? _clrHighMvar : _clrSuspect;
                    }
                    break;
            }

            return Color.Empty;
        }

        // Tolerance (MW) for comparing transformer input vs feeder output,
        // covers measurement noise and transformer losses.
        private const double _mwTolerance = 1.0;

        // Transformer MW columns (sub-col 2 of each 4-col group).
        private const int _colQayraMW = 7;
        private const int _colT1MW = 11;
        private const int _colT2MW = 15;
        private const int _colT3MW = 19;

        // Feeder MW columns.
        private const int _colDomez = 21;
        private const int _colSummer = 22;
        private const int _colSalam1 = 23;
        private const int _colSalam2 = 24;

        private static bool TryReadDouble(ListViewItem item, int col, out double v)
        {
            v = 0;
            if (item == null || col < 0 || col >= item.SubItems.Count) return false;
            return double.TryParse(item.SubItems[col].Text, out v);
        }

        private Color CheckFeederAgainstTransformer(int feederCol, double feederMW, ListViewItem item)
        {
            int xfmrCol;
            switch (feederCol)
            {
                case _colDomez:  xfmrCol = _colT1MW; break;
                case _colSummer: xfmrCol = _colT2MW; break;
                case _colSalam1: xfmrCol = _colT2MW; break;
                case _colSalam2: xfmrCol = _colT3MW; break;
                default: return Color.Empty;
            }
            if (!TryReadDouble(item, xfmrCol, out double xfmrMW)) return Color.Empty;
            return Math.Abs(feederMW) > Math.Abs(xfmrMW) + _mwTolerance
                 ? _clrHighMvar : Color.Empty;
        }

        private Color CheckTransformerLoadBalance(int col, double xfmrMW, ListViewItem item)
        {
            double sumOut = 0; bool any = false;

            switch (col)
            {
                case _colT1MW:
                    if (TryReadDouble(item, _colDomez, out double d)) { sumOut += Math.Abs(d); any = true; }
                    break;
                case _colT2MW:
                    if (TryReadDouble(item, _colSummer, out double su)) { sumOut += Math.Abs(su); any = true; }
                    if (TryReadDouble(item, _colSalam1, out double s1)) { sumOut += Math.Abs(s1); any = true; }
                    break;
                case _colT3MW:
                    if (TryReadDouble(item, _colSalam2, out double s2)) { sumOut += Math.Abs(s2); any = true; }
                    break;
                case _colQayraMW:
                    if (TryReadDouble(item, _colT1MW, out double t1)) { sumOut += Math.Abs(t1); any = true; }
                    if (TryReadDouble(item, _colT2MW, out double t2)) { sumOut += Math.Abs(t2); any = true; }
                    if (TryReadDouble(item, _colT3MW, out double t3)) { sumOut += Math.Abs(t3); any = true; }
                    break;
                default:
                    return Color.Empty;
            }

            if (!any) return Color.Empty;
            return sumOut > Math.Abs(xfmrMW) + _mwTolerance ? _clrHighMvar : Color.Empty;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  ROW / SUBITEM DRAWING  — NO artificial margin offsets
        // ═══════════════════════════════════════════════════════════════════
        private void ListView1_DrawItem(object sender, DrawListViewItemEventArgs e)
            => e.DrawDefault = false;

        private void ListView1_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ColumnIndex == 0) { e.DrawDefault = true; return; }

            Color back;
            Color fore = Color.Black;

            if (e.Item.Selected)
            {
                back = SystemColors.Highlight;
                fore = SystemColors.HighlightText;
            }
            else
            {
                // Start with group colour, then apply any validation override
                back = _groupColors[GetColorIndexForColumn(e.ColumnIndex)];
                Color vColor = GetValidationColor(
                    e.ColumnIndex, e.SubItem.Text, e.Item);
                if (vColor != Color.Empty) back = vColor;
            }

            using (var br = new SolidBrush(back))
                e.Graphics.FillRectangle(br, e.Bounds);

            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, _cellFont, e.Bounds,
                fore, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);

            e.Graphics.DrawRectangle(Pens.Gray, e.Bounds);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  COLUMN / GROUP HELPERS
        // ═══════════════════════════════════════════════════════════════════
        private int GetColorIndexForColumn(int col)
        {
            if (col >= 1 && col <= 4) return 0;
            if (col >= 5 && col <= 8) return 1;
            if (col >= 9 && col <= 12) return 2;
            if (col >= 13 && col <= 16) return 3;
            if (col >= 17 && col <= 20) return 4;
            if (col >= 21 && col <= 24) return 5;
            return 0;
        }

        private readonly Color[] _groupColors =
        {
            Color.FromArgb(255,230,230,250), Color.FromArgb(255,230,255,230),
            Color.FromArgb(255,255,240,245), Color.FromArgb(255,240,255,240),
            Color.FromArgb(255,240,248,255), Color.FromArgb(255,255,245,238)
        };

        private void UpdateColumnVisibility()
        {
            bool[] vis = {
                checkBoxYarema.Checked, checkBoxQayara.Checked,
                checkBoxT1.Checked, checkBoxT2.Checked,
                checkBoxT3.Checked, checkBoxFdd.Checked
            };
            bool any = false;
            foreach (bool b in vis) if (b) { any = true; break; }
            if (!any) for (int i = 0; i < vis.Length; i++) vis[i] = true;

            for (int c = 1; c < listView1.Columns.Count; c++)
                listView1.Columns[c].Width =
                    vis[GetColorIndexForColumn(c)] ? DefaultColumnWidth : 0;

            if (listView1.IsHandleCreated)
                BeginInvoke(new System.Action(EnsureHeaderHeight));
        }

        // ═══════════════════════════════════════════════════════════════════
        //  LISTVIEW DATA
        // ═══════════════════════════════════════════════════════════════════
        private void ListView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isUpdatingFromListView || listView1.SelectedItems.Count == 0) return;
            var item = listView1.SelectedItems[0];
            if (int.TryParse(item.Text, out int h))
            {
                _isUpdatingFromListView = true;
                _currentHour = h;
                LoadCurrentImageFromData();
                _isUpdatingFromListView = false;
            }

            listView1_SelectedIndexChanged(sender, e);
        }

        private void LoadCurrentImageFromData()
        {
            if (!_hourData.ContainsKey(_currentHour)) return;
            UpdateTextboxes(_hourData[_currentHour]);
            labelStatus.Text = _currentHour == 0 ? "Hour: 24" : $"Hour: {_currentHour}";
            string ip = GetCurrentImagePath();
            if (File.Exists(ip))
            {
                try
                {
                    pictureBox1.Image?.Dispose(); pictureBox1.Image = null;
                    using (var fs = new FileStream(ip, FileMode.Open,
                                                    FileAccess.Read, FileShare.ReadWrite))
                    using (var img = Image.FromStream(fs))
                        pictureBox1.Image = new Bitmap(img);
                    panel_fixedError.Hide();
                }
                catch (Exception ex)
                {
                    pictureBox1.Image = null; panel_fixedError.Show();
                    SetStatus("Error loading image: " + ex.Message, 4000);
                }
            }
            else
            {
                pictureBox1.Image?.Dispose(); pictureBox1.Image = null;
                panel_fixedError.Show();
            }
            SetStatus($"Loaded hour {_currentHour:00} from memory", 2000);
        }

        private string GetMonthlyExcelPath()
            => Path.Combine(Application.StartupPath,
                            DateTime.Now.ToString("MM-yyyy") + ".xlsx");

        private void UpdatePrintButtonEnabled()
            => button_OnPrint.Enabled =  (_currentHour == 24);   // Always available; validation runs on click

        private void LoadCurrentImage()
        {
            if (_currentHour == 25) _currentHour = 8;
            if (_currentHour == 1) _currentHour = 7;
            string ip = GetCurrentImagePath();
            if (File.Exists(ip))
            {
                SetStatus($"Loading hour {_currentHour:00}...", 0);
                pictureBox1.Image = Image.FromFile(ip);
                string[] nums = RunOcr(ip);
                _hourData[_currentHour] = nums;
                UpdateTextboxes(nums); UpdateListView();
                labelStatus.Text = _currentHour == 0 ? "Hour: 24" : $"Hour: {_currentHour}";
                SetStatus($"Hour {_currentHour:00} loaded successfully", 3000);
                panel_fixedError.Hide(); SelectCurrentHourInListView();
            }
            else
            {
                SetStatus($"Error: Image not found - {Path.GetFileName(ip)}", 5000);
                ClearAllTextboxes(); pictureBox1.Image = null;
                panel_fixedError.Show();
                labelStatus.Text = _currentHour == 0 ? "Hour: 24" : $"Hour: {_currentHour}";
                if (!_hourData.ContainsKey(_currentHour))
                {
                    _hourData[_currentHour] = new string[0];
                    UpdateListView(); SelectCurrentHourInListView();
                }
            }
        }

        private void SelectCurrentHourInListView()
        {
            foreach (ListViewItem item in listView1.Items)
                if (item.Text == _currentHour.ToString())
                { item.Selected = true; item.EnsureVisible(); break; }
        }

        private string GetCurrentImagePath()
            => Path.Combine(_imageFolderPath, $"{_currentHour:00}.png");

        private TextBox[] GetTextBoxes() => new[] {
            textBox1,  textBox2,  textBox3,  textBox4,  textBox5,
            textBox6,  textBox7,  textBox8,  textBox9,  textBox10,
            textBox11, textBox12, textBox13, textBox14, textBox15,
            textBox16, textBox17, textBox18, textBox19, textBox20,
            textBox21, textBox22, textBox23, textBox24
        };

        private void UpdateTextboxes(string[] nums)
        {
            ClearAllTextboxes();
            if (nums == null || nums.Length == 0) { UpdateTextboxValidationColors(); return; }
            var tb = GetTextBoxes();
            int max = Math.Min(nums.Length, tb.Length);
            for (int i = 0; i < max; i++) tb[i].Text = GetParseString(nums[i]);
            AttachTextChangedHandlers();
            UpdateTextboxValidationColors();
        }

        // Mirror ListView validation colours onto the edit textboxes so errors
        // are visible while typing, not only in the list.
        private void UpdateTextboxValidationColors()
        {
            var tbs = GetTextBoxes();
            var probe = new ListViewItem(_currentHour.ToString());
            for (int i = 0; i < tbs.Length; i++)
                probe.SubItems.Add(tbs[i].Text ?? string.Empty);

            for (int i = 0; i < tbs.Length; i++)
            {
                int col = i + 1;
                Color vc = GetValidationColor(col, tbs[i].Text ?? string.Empty, probe);
                tbs[i].BackColor = vc == Color.Empty ? SystemColors.Window : vc;
            }
        }

        private void AttachTextChangedHandlers()
        {
            foreach (var t in GetTextBoxes())
            { t.TextChanged -= TextBox_TextChanged; t.TextChanged += TextBox_TextChanged; }
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            if (_isUpdatingFromListView) return;
            UpdateDataFromTextboxes(); UpdateListView();
            UpdateTextboxValidationColors();
        }

        private void UpdateDataFromTextboxes()
        {
            var list = new List<string>();
            foreach (var t in GetTextBoxes())
                if (!string.IsNullOrEmpty(t.Text)) list.Add(t.Text);
            _hourData[_currentHour] = list.ToArray();
        }

        private void UpdateListView()
        {
            listView1.Items.Clear();
            var hrs = new List<int>();
            for (int i = 7; i <= 24; i++) hrs.Add(i);
            foreach (int h in hrs)
                if (_hourData.ContainsKey(h)) AddHourToListView(h, _hourData[h]);
            SelectCurrentHourInListView();
        }

        private void AddHourToListView(int hour, string[] nums)
        {
            var item = new ListViewItem(hour.ToString());
            int max = Math.Min(nums.Length, 24);
            for (int i = 0; i < max; i++) item.SubItems.Add(GetParseString(nums[i]));
            for (int i = max; i < 24; i++) item.SubItems.Add("");
            listView1.Items.Add(item);
        }

        private void UpdateListView(string[] nums)
        {
            listView1.Items.Clear();
            if (nums == null || nums.Length == 0) return;
            var item = new ListViewItem(_currentHour.ToString());
            int max = Math.Min(nums.Length, 24);
            for (int i = 0; i < max; i++) item.SubItems.Add(GetParseString(nums[i]));
            for (int i = max; i < 24; i++) item.SubItems.Add("");
            listView1.Items.Add(item);
        }

        private void ClearAllTextboxes()
        {
            foreach (var t in GetTextBoxes())
            { t.TextChanged -= TextBox_TextChanged; t.Text = ""; }
        }

        private string GetParseString(string s)
        {
            if (string.IsNullOrEmpty(s) || s == "n/a") return "";
            int pt = s.IndexOf('.'); if (pt >= 0) s = s.Substring(0, pt);
            return s.Replace("-", "").Trim();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  NAVIGATION
        // ═══════════════════════════════════════════════════════════════════
        private void button_prv_Click(object sender, EventArgs e)
        {
            if (_currentHour == 0) _currentHour = 24;
            if (_currentHour > 8)
            { _currentHour--; LoadCurrentImage(); SelectCurrentHourInListView(); }
            else SetStatus("Already at the first image (08.png)", 3000);
        }

        int GetCuurentHour(bool bInc = false)
            => DateTime.Now.Hour == 0 ? 24 : DateTime.Now.Hour + (bInc ? 1 : 0);

        private void button_next_Click(object sender, EventArgs e)
        {
            if (_currentHour >= GetCuurentHour(true))
            { SetStatus("Cannot goes far to the current hour", 3000); return; }
            if (_currentHour <= 24 && _currentHour > 0)
            { _currentHour++; LoadCurrentImage(); SelectCurrentHourInListView(); }
            else SetStatus("Already at the last image (24.png)", 3000);
        }

        private int GetPreviousHour(int h) => h == 8 ? 24 : h == 0 ? 23 : h - 1;
        private int GetNextHour(int h) => h == 24 ? 8 : h == 23 ? 0 : h + 1;

        private void button_copy_fromPrev_Click(object sender, EventArgs e)
        {
            int prev = GetPreviousHour(_currentHour);
            if (_hourData.ContainsKey(prev) && _hourData[prev].Length > 0)
            {
                _hourData[_currentHour] = (string[])_hourData[prev].Clone();
                UpdateTextboxes(_hourData[_currentHour]); UpdateListView();
                panel_fixedError.Hide();
                SetStatus($"Copied data from hour {prev:00}", 3000);
                string ip = GetCurrentImagePath();
                pictureBox1.Image = File.Exists(ip) ? Image.FromFile(ip) : null;
            }
            else SetStatus($"No data from previous hour ({prev:00})", 3000);
        }

        private void button_copy_fromNext_Click(object sender, EventArgs e)
        {
            int next = GetNextHour(_currentHour);
            if (_hourData.ContainsKey(next) && _hourData[next].Length > 0)
            {
                _hourData[_currentHour] = (string[])_hourData[next].Clone();
                UpdateTextboxes(_hourData[_currentHour]); UpdateListView();
                panel_fixedError.Hide();
                SetStatus($"Copied data from hour {next:00}", 3000);
                string ip = GetCurrentImagePath();
                pictureBox1.Image = File.Exists(ip) ? Image.FromFile(ip) : null;
            }
            else SetStatus($"No data from next hour ({next:00})", 3000);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  STATUS
        // ═══════════════════════════════════════════════════════════════════
        public void SetStatus(string msg, int ms = 0)
        {
            try
            {
                if (statusStrip1 == null) return;
                if (statusStrip1.InvokeRequired)
                { statusStrip1.Invoke(new Action<string, int>(SetStatus), msg, ms); return; }
                _statusTimer.Stop();
                Color col = msg.StartsWith("Error:") || msg.StartsWith("Already") ||
                            msg.StartsWith("Cannot") ? Color.Red
                    : msg.StartsWith("Loading") || msg.Contains("loading") ? Color.Blue
                    : (msg == "Ready" || msg.Contains("successfully")) ? Color.Green
                    : Color.Black;
                if (toolStripStatusLabel1 != null)
                { toolStripStatusLabel1.Text = msg; toolStripStatusLabel1.ForeColor = col; }
                else if (_statusLabel != null)
                { _statusLabel.Text = msg; _statusLabel.ForeColor = col; }
                if (ms > 0) { _statusTimer.Interval = ms; _statusTimer.Start(); }
            }
            catch { }
        }

        private void OnStatusTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (statusStrip1 == null) return;
                if (statusStrip1.InvokeRequired)
                    statusStrip1.Invoke(new System.Action(() => SetStatusText("Ready", Color.Green)));
                else SetStatusText("Ready", Color.Green);
            }
            catch { }
        }

        private void SetStatusText(string t, Color c)
        {
            if (toolStripStatusLabel1 != null)
            { toolStripStatusLabel1.Text = t; toolStripStatusLabel1.ForeColor = c; }
            else if (_statusLabel != null) { _statusLabel.Text = t; _statusLabel.ForeColor = c; }
        }

        private void buttonSelectFolder_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog
            {
                Description = "Select folder (08.png–24.png)",
                SelectedPath = _imageFolderPath
            })
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    _imageFolderPath = fbd.SelectedPath; _currentHour = 8;
                    SetStatus($"Folder: {Path.GetFileName(_imageFolderPath)}", 3000);
                    LoadCurrentImage();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  OCR
        // ═══════════════════════════════════════════════════════════════════
        private string[] RunOcr(string ip)
        {
            try
            {
                SetStatus("Processing OCR...", 0);
                using (var eng = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default))
                {
                    eng.SetVariable("tessedit_char_whitelist", "0123456789.-");
                    eng.SetVariable("tessedit_pageseg_mode", "6");
                    eng.SetVariable("tessedit_char_blacklist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
                    eng.SetVariable("classify_bln_numeric_mode", "1");
                    using (var img = Pix.LoadFromFile(ip))
                    using (var page = eng.Process(PreprocessImage(img)))
                    {
                        float conf = page.GetMeanConfidence() * 100;
                        SetStatus($"OCR — Confidence: {conf:0.0}%", 2000);
                        return ExtractNumbersToArray(page.GetText(), conf);
                    }
                }
            }
            catch (Exception ex) { SetStatus($"OCR Error: {ex.Message}", 5000); return new string[0]; }
        }

        private string[] ExtractNumbersToArray(string raw, float conf)
        {
            if (string.IsNullOrEmpty(raw)) return new string[0];
            var list = new List<string>();
            foreach (var line in raw.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string c = CleanLine(line);
                if (!string.IsNullOrEmpty(c) && ContainsNumbers(c)) list.Add(c);
            }
            if (list.Count == 0) list.Add("n/a");
            return list.ToArray();
        }

        private string CleanLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return string.Empty;
            var sb = new System.Text.StringBuilder(); bool sp = false;
            foreach (char c in line.Trim())
            {
                if (char.IsDigit(c) || c == '.' || c == '-') { sb.Append(c); sp = false; }
                else if ((c == ' ' || c == '\t') && !sp && sb.Length > 0) { sb.Append(' '); sp = true; }
            }
            return sb.ToString().Trim();
        }

        private Pix PreprocessImage(Pix o)
        {
            var g = o.ConvertRGBToGray();
            var s = g.Scale(2f, 2f);
            return s.BinarizeOtsuAdaptiveThreshold(32, 32, 10, 10, 0.1f);
        }

        private bool ContainsNumbers(string t)
        { foreach (char c in t) if (char.IsDigit(c)) return true; return false; }

        protected override void OnFormClosed(FormClosedEventArgs e)
        { _statusTimer?.Stop(); _statusTimer?.Dispose(); base.OnFormClosed(e); }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            EnsureHeaderHeight();
        }

        private void OnNext(object sender, KeyEventArgs e) => button_next_Click(sender, e);
        private void OnPrev(object sender, KeyEventArgs e) => button_prv_Click(sender, e);

        // ═══════════════════════════════════════════════════════════════════
        //  PRINT
        // ═══════════════════════════════════════════════════════════════════
        private string CleanDate(string raw)
            => raw.Contains(":") ? raw.Substring(raw.IndexOf(':') + 1).Trim() : raw.Trim();

        public void OpenCalibrationPreview(LogsheetTextPrinter printer, IWin32Window owner)
        {
            string sub = label22.Text.Length > 0 ? label22.Text : "Test Substation";
            string date = CleanDate(lable_date.Text).Length > 0
                          ? CleanDate(lable_date.Text) : DateTime.Now.ToString("dd/MM/yyyy");
            printer.PrintPreview(listView1, sub, date, null, owner);
        }

        private LogsheetTextPrinter _sharedPrinter;

        private LogsheetTextPrinter GetPrinter(bool showImage = false)
        {
            if (_sharedPrinter == null)
            {
                _sharedPrinter = new LogsheetTextPrinter();
                string path = CoordinateCalibrationDialog.DefaultCoordsPath;
                if (File.Exists(path))
                    CoordinateCalibrationDialog.LoadCoordsInto(_sharedPrinter, path);
            }
            _sharedPrinter.ShowImage = showImage;
            return _sharedPrinter;
        }

        // ── Collect every validation issue across all rows ───────────────────
        private List<string> CollectValidationIssues()
        {
            var issues = new List<string>();
            foreach (ListViewItem item in listView1.Items)
            {
                if (!int.TryParse(item.Text, out int hour)) continue;
                if (!RowHasAnyData(item)) continue;   // skip hours with no data at all

                for (int col = 1; col < item.SubItems.Count; col++)
                {
                    string val = item.SubItems[col].Text;
                    Color vc = GetValidationColor(col, val, item);
                    if (vc == Color.Empty) continue;

                    string colDesc = ColDesc(col);
                    string severity;
                    if (vc == _clrMissing) severity = "MISSING";
                    else if (vc == _clrHighMvar)
                    {
                        bool isMwOrFeeder = col >= 21 || (col <= 20 && ((col - 1) % 4) == 2);
                        severity = isMwOrFeeder ? "OVERLOAD" : "HIGH MVAR";
                    }
                    else severity = "SUSPECT";

                    string detail = string.IsNullOrWhiteSpace(val) ? "(empty)" : val;
                    issues.Add($"  Hr {hour:D2}  {colDesc,-18} {severity}  [{detail}]");
                }
            }
            return issues;
        }

        private static string ColDesc(int col)
        {
            string grp = col <= 4 ? "Yarmja" : col <= 8 ? "Qayra" :
                         col <= 12 ? "T1" : col <= 16 ? "T2" :
                         col <= 20 ? "T3" : col == 21 ? "Domez" :
                         col == 22 ? "Summer" : col == 23 ? "Salam1" : "Salam2";
            string sub = col >= 21 ? "WM" : GetSubName(col);
            return $"{grp} {sub}";
        }

        private void buttonOnPrint(object sender, EventArgs e)
        {
            // ── الخطوة 1: جمع جميع مشاكل التحقق ─────────────────────
            var issues = CollectValidationIssues();

            if (issues.Count > 0)
            {
                // ── الخطوة 2: عرض تحذير ومنع الطباعة بالكامل ─
                const int MAX_SHOWN = 25;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("⛔ لا يمكن المتابعة إلى الطباعة.");
                sb.AppendLine("الخلايا التالية تحتوي على قيم خاطئة أو مفقودة:");
                sb.AppendLine(new string('─', 55));

                foreach (var iss in issues.GetRange(0, Math.Min(issues.Count, MAX_SHOWN)))
                    sb.AppendLine(iss);

                if (issues.Count > MAX_SHOWN)
                    sb.AppendLine($"  … وهناك {issues.Count - MAX_SHOWN} مشكلة إضافية.");

                sb.AppendLine(new string('─', 55));
                sb.AppendLine();
                sb.AppendLine("👁 يرجى مراجعة الصورة في اللوحة اليسرى");
                sb.AppendLine("   وتصحيح جميع الأخطاء قبل المتابعة.");

                MessageBox.Show(
                    sb.ToString(),
                    "خطأ في التحقق — يجب التصحيح قبل الطباعة",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                SetStatus("تم منع الطباعة — يجب تصحيح جميع القيم أولاً.", 4000);
                return; // ⛔ إيقاف كامل
            }

            // ── الخطوة 3: السماح فقط عند عدم وجود مشاكل ────
            try
            {
                GetPrinter(false).PrintPreview(listView1,
                    label22.Text, CleanDate(lable_date.Text));

                SetStatus("تم إغلاق معاينة الطباعة.", 2000);
            }
            catch (Exception ex)
            {
                SetStatus($"خطأ في الطباعة: {ex.Message}", 5000);
                MessageBox.Show($"فشلت الطباعة:\n{ex.Message}", "خطأ في الطباعة",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonCheckImage_Click(object sender, EventArgs e)
        {
            try
            {
                var p = GetPrinter(true);
                string sub = label22.Text.Length > 0 ? label22.Text : "Test Substation";
                string date = CleanDate(lable_date.Text).Length > 0
                              ? CleanDate(lable_date.Text)
                              : DateTime.Now.ToString("dd/MM/yyyy");
                p.PrintPreview(listView1, sub, date, null);
                SetStatus("Image check closed.", 2000);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Image check failed:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


       
        private void buttonCalibrate_Click(object sender, EventArgs e)
        {
            using (var dlg = new CoordinateCalibrationDialog(GetPrinter(false)))
                dlg.ShowDialog(this);
            SetStatus("Calibration closed.", 2000);
        }

        private void PrintFrontDirect()
            => GetPrinter(false).Print(listView1, label22.Text,
                CleanDate(lable_date.Text), null);

        // ═══════════════════════════════════════════════════════════════════
        //  EXCEL EXPORT
        // ═══════════════════════════════════════════════════════════════════
        private void ExportToExactExcelTemplate()
        {
            try
            {
                string tp = Path.Combine(Application.StartupPath, TemplateFileName);
                if (!File.Exists(tp))
                {
                    MessageBox.Show($"Template not found:\n{tp}", "Excel export",
                    MessageBoxButtons.OK, MessageBoxIcon.Error); return;
                }
                string mp = GetMonthlyExcelPath();
                if (!File.Exists(mp)) File.Copy(tp, mp, true);

                using (var pkg = new ExcelPackage(new FileInfo(mp)))
                {
                    var ts = pkg.Workbook.Worksheets["TemplateSheet"]
                          ?? pkg.Workbook.Worksheets["ExactSheet"]
                          ?? pkg.Workbook.Worksheets[0];
                    ts.Name = "TemplateSheet"; ts.Hidden = eWorkSheetHidden.VeryHidden;
                    string day = DateTime.Now.ToString("dd");
                    var ws = pkg.Workbook.Worksheets[day]
                          ?? pkg.Workbook.Worksheets.Add(day, ts);

                    int[] cY = { 7, 9, 10, 11 }, cQ = { 14, 16, 17, 18 }, cT1 = { 20, 21, 22, 23 };
                    int[] cT2 = { 25, 26, 27, 28 }, cT3 = { 30, 31, 32, 33 }, cF = { 36, 37, 38 };

                    for (int r = 6; r <= 29; r++)
                    {
                        foreach (int c in cY) ws.Cells[r, c].Value = null;
                        foreach (int c in cQ) ws.Cells[r, c].Value = null;
                        foreach (int c in cT1) ws.Cells[r, c].Value = null;
                        foreach (int c in cT2) ws.Cells[r, c].Value = null;
                        foreach (int c in cT3) ws.Cells[r, c].Value = null;
                        foreach (int c in cF) ws.Cells[r, c].Value = null;
                    }
                    foreach (ListViewItem item in listView1.Items)
                    {
                        if (!int.TryParse(item.Text, out int h)) continue;
                        if (h == 0) h = 24; if (h < 1 || h > 24) continue;
                        int row = 4 + h;
                        void Copy(int s, int[] cols)
                        {
                            for (int i = 0; i < cols.Length; i++)
                            {
                                int si = s + i;
                                if (si >= item.SubItems.Count) break;
                                string txt = item.SubItems[si].Text;
                                if (string.IsNullOrWhiteSpace(txt)) continue;
                                ws.Cells[row, cols[i]].Value =
                                    double.TryParse(txt, out double d) ? (object)d : txt;
                                ws.Cells[row, cols[i]].Style.HorizontalAlignment =
                                    ExcelHorizontalAlignment.Center;
                            }
                        }
                        Copy(1, cY); Copy(5, cQ); Copy(9, cT1);
                        Copy(13, cT2); Copy(17, cT3); Copy(21, cF);
                    }
                    pkg.Save();
                }
                string bak = Path.Combine(Path.GetDirectoryName(mp),
                    Path.GetFileNameWithoutExtension(mp) + "_backup" + Path.GetExtension(mp));
                File.Copy(mp, bak, true);
                MessageBox.Show($"Saved.\nMain: {mp}\nBackup: {bak}",
                    "Excel export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed:\n" + ex.Message, "Excel export",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonOnExcel(object sender, EventArgs e) => ExportToExactExcelTemplate();

        private void PrintTodaySheetFromExcel(string wbPath)
        {
            string day = DateTime.Now.ToString("dd");
            ExcelInterop.Application app = null; ExcelInterop.Workbook wb = null;
            try
            {
                app = new ExcelInterop.Application { Visible = false };
                wb = app.Workbooks.Open(wbPath);
                ExcelInterop.Worksheet ws = null;
                foreach (ExcelInterop.Worksheet s in wb.Worksheets)
                    if (s.Name == day) { ws = s; break; }
                if (ws == null) throw new Exception($"Sheet '{day}' not found.");
                ws.Select();
                ws.PageSetup.Orientation = ExcelInterop.XlPageOrientation.xlLandscape;
                ws.PageSetup.PaperSize = ExcelInterop.XlPaperSize.xlPaperA4;
                ws.PrintOut();
            }
            finally
            {
                if (wb != null) { wb.Close(false); Marshal.ReleaseComObject(wb); }
                if (app != null) { app.Quit(); Marshal.ReleaseComObject(app); }
                GC.Collect(); GC.WaitForPendingFinalizers();
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  SCREEN CAPTURE
        // ═══════════════════════════════════════════════════════════════════
        private Bitmap CaptureScreenRegion(Rectangle r)
        {
            var bmp = new Bitmap(r.Width, r.Height);
            using (var g = Graphics.FromImage(bmp))
                g.CopyFromScreen(r.Location, Point.Empty, r.Size);
            return bmp;
        }

        private void CaptureCurrentHourFromScreenAndOcr()
        {
            int cur = GetCuurentHour();
            if (_currentHour != cur)
            {
                MessageBox.Show($"You can only capture hour {cur:00}.\nSelected: {_currentHour:00}",
                    "Wrong hour", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpdateCaptureButtonState(); return;
            }
            try
            {
                if (!Directory.Exists(_imageFolderPath))
                    Directory.CreateDirectory(_imageFolderPath);
                string ip = GetCurrentImagePath();
                using (var bmp = CaptureScreenRegion(_captureRegion))
                {
                    bmp.Save(ip, System.Drawing.Imaging.ImageFormat.Png);
                    pictureBox1.Image?.Dispose(); pictureBox1.Image = null;
                    pictureBox1.Image = new Bitmap(bmp);
                }
                string[] nums = RunOcr(ip);
                _hourData[_currentHour] = nums;
                UpdateTextboxes(nums); UpdateListView();
                SelectCurrentHourInListView(); panel_fixedError.Hide();
                labelStatus.Text = _currentHour == 0 ? "Hour: 24" : $"Hour: {_currentHour}";
                SetStatus($"Captured & OCR for hour {_currentHour:00}", 3000);
            }
            catch (Exception ex)
            {
                SetStatus($"Capture/OCR error: {ex.Message}", 5000);
                MessageBox.Show("Capture/OCR error:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateCaptureButtonState()
        {
            int cur = GetCuurentHour(); bool ok = (_currentHour == cur);
            button_UpdateCurrHImage.Enabled = ok;

            if (ok)
            {
                SetStatus($"Ready to capture hour {_currentHour:00}", 3000);
            }

        }

        private void button_UpdateCurrHImage_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Change current image recognition?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                CaptureCurrentHourFromScreenAndOcr();
        }

        private void button_New_Click(object sender, EventArgs e)
        {
            const string msg =
                "⚠ تحذير — ابدأ جلسة جديدة فقط إذا كنت في يوم جديد.\r\n" +
                "سيتم إغلاق البرنامج وإعادة تشغيله، وستفقد البيانات غير المحفوظة.\r\n" +
                "\r\n" +
                "WARNING — Only start a new session if this is a new day.\r\n" +
                "The application will close and restart; any unsaved data will be lost.\r\n" +
                "\r\n" +
                "هل تريد المتابعة؟ / Continue?";

            const string title = "جلسة جديدة / New Session";

            var result = MessageBox.Show(
                msg, title,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button3);

            if (result != DialogResult.Yes) return;

            try { Application.Restart(); }
            catch (Exception ex)
            {
                MessageBox.Show("Restart failed: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  TASK SCHEDULER
        // ═══════════════════════════════════════════════════════════════════
        private bool TaskNeedsUpdate()
        {
            string name = "ScreenCapHourlyTask", vbs = Application.StartupPath + "\\screencap.vbs";
            using (var ts = new TaskService())
            {
                var ex = ts.GetTask(name); if (ex == null) return true;
                if (ex.Definition.Triggers.Count == 0) return true;
                var t = ex.Definition.Triggers[0] as TimeTrigger; if (t == null) return true;
                if (t.StartBoundary.Minute != 2) return true;
                if (t.Repetition.Interval != TimeSpan.FromHours(1)) return true;
                if (t.EndBoundary != t.StartBoundary.AddDays(1)) return true;
                if (DateTime.Now > t.EndBoundary) return true;
                if (ex.Definition.Settings.DeleteExpiredTaskAfter != TimeSpan.FromMinutes(5)) return true;
                var a = ex.Definition.Actions[0] as ExecAction;
                if (a == null || !a.Arguments.Contains(vbs)) return true;
                return false;
            }
        }

        private void CreateHourlyTask()
        {
            try
            {
                string name = "ScreenCapHourlyTask", vbs = Application.StartupPath + "\\screencap.vbs";
                using (var ts = new TaskService())
                {
                    var td = ts.NewTask();
                    td.RegistrationInfo.Description = "Auto capture screen every hour";
                    var now = DateTime.Now;
                    var start = new DateTime(now.Year, now.Month, now.Day, now.Hour, 2, 0);
                    if (DateTime.Now > start) start = start.AddHours(1);
                    var exp = new DateTime(start.Year, start.Month, start.Day, 0, 2, 0).AddDays(1);
                    td.Triggers.Add(new TimeTrigger
                    {
                        StartBoundary = start,
                        EndBoundary = exp,
                        Repetition = new RepetitionPattern(TimeSpan.FromHours(1), TimeSpan.Zero)
                    });
                    td.Settings.DeleteExpiredTaskAfter = TimeSpan.FromMinutes(5);
                    td.Actions.Add(new ExecAction("wscript.exe", $"\"{vbs}\"", null));
                    ts.RootFolder.RegisterTaskDefinition(name, td);
                    MessageBox.Show($"Task created.\nStarts: {start}\nExpires: {exp}");
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void button_Start_Click(object sender, EventArgs e)
        {
            if (TaskNeedsUpdate()) { CreateHourlyTask(); button_start.Enabled = false; }
            else MessageBox.Show("Task already exists and is valid.");
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            _currentHour = int.Parse(listView1.SelectedItems[0].Text);
            UpdateCaptureButtonState();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button_start.Enabled = TaskNeedsUpdate();
            // Defer header setup until after all layout is complete
            BeginInvoke(new System.Action(EnsureHeaderHeight));
        }

        private void checkBoxYarema_CheckedChanged(object sender, EventArgs e) => UpdateColumnVisibility();
        private void checkBoxQayara_CheckedChanged(object sender, EventArgs e) => UpdateColumnVisibility();
        private void checkBoxT1_CheckedChanged(object sender, EventArgs e) => UpdateColumnVisibility();
        private void checkBoxT2_CheckedChanged(object sender, EventArgs e) => UpdateColumnVisibility();
        private void checkBoxT3_CheckedChanged(object sender, EventArgs e) => UpdateColumnVisibility();
        private void checkBoxFdd_CheckedChanged(object sender, EventArgs e) => UpdateColumnVisibility();

        private void button_DeleteCurrentImage_Click(object sender, EventArgs e)
        {
            string ip = GetCurrentImagePath();
            if (MessageBox.Show(
                $"Delete image for Hour {_currentHour:00}?\n\nFile: {ip}\n\nCannot be undone.",
                "Delete Image — Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                if (File.Exists(ip))
                {
                    pictureBox1.Image?.Dispose(); pictureBox1.Image = null;
                    File.Delete(ip); panel_fixedError.Show();
                    SetStatus($"Image for hour {_currentHour:00} deleted.", 3000);
                }
                else SetStatus($"No image found for hour {_currentHour:00}.", 3000);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not delete:\n{ex.Message}", "Delete Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}