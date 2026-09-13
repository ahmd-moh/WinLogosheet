using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WinLogosheet
{
    /// <summary>
    /// Coordinate calibration dialog.
    ///
    /// THREE levels of adjustment:
    ///   1. Global Offset X/Y  — shifts EVERY field together
    ///   2. Group Offset       — shifts one whole section (Yarmja / Qayra / TR.1 / …)
    ///   3. Column X           — fine-tunes a single column
    ///
    /// Effective X = GlobalOffsetX + GroupOffset + ColumnX
    ///
    /// Usage from Form1:
    ///     using (var dlg = new CoordinateCalibrationDialog(printer))
    ///         dlg.ShowDialog(this);
    /// </summary>
    public class CoordinateCalibrationDialog : Form
    {
        private readonly LogsheetTextPrinter _printer;

        private Panel            _scrollPanel;
        private NumericUpDown    _nudOffsetX, _nudOffsetY;
        private Label            _lblStatus;
        private Button           _btnPreview, _btnSave, _btnLoad, _btnReset, _btnClose;

        // ── Group offsets (one per section) ──────────────────────────────
        private readonly string[] _groupNames =
            { "Hours", "Yarmja", "Qayra", "TR.1 SCR", "TR.2 SCR", "TR.3 SCR", "BC" };

        private readonly Dictionary<string, float>        _groupOffset  = new Dictionary<string, float>();
        private readonly Dictionary<string, NumericUpDown> _groupNud    = new Dictionary<string, NumericUpDown>();

        // ── Individual column rows ────────────────────────────────────────
        private struct ColRow
        {
            public string          Section;
            public string          ColName;
            public string          Desc;
            public Func<float>     GetBase;   // raw value stored in ColumnCoords
            public Action<float>   SetBase;
            public NumericUpDown   Nud;
            public Label           LblEff;
        }
        private ColRow[] _colRows;

        // ══════════════════════════════════════════════════════════════════
        public CoordinateCalibrationDialog(LogsheetTextPrinter printer)
        {
            _printer = printer ?? throw new ArgumentNullException(nameof(printer));

            foreach (var g in _groupNames) _groupOffset[g] = 0f;

            BuildForm();
            BuildColRows();
            BuildScrollContent();

            // Auto-load saved coords when dialog opens (silent if file missing)
            AutoLoad();
        }

        // ── Default save/load path (next to EXE) ─────────────────────────
        public static string DefaultCoordsPath =>
            System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "logsheet_coords.coords");

        /// <summary>
        /// Load coords from a file directly into a printer — no dialog needed.
        /// Called by Form1.GetPrinter() on first use.
        /// </summary>
        public static void LoadCoordsInto(LogsheetTextPrinter printer, string path)
        {
            if (printer == null || !File.Exists(path)) return;
            var c  = printer.Coords;
            var ic = System.Globalization.NumberStyles.Float;
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                var parts = line.Split('=');
                if (parts.Length != 2) continue;
                if (!float.TryParse(parts[1].Trim(), ic, ci, out float val)) continue;
                switch (parts[0].Trim())
                {
                    case "OffsetX":    printer.OffsetXmm = val; break;
                    case "OffsetY":    printer.OffsetYmm = val; break;
                    case "Hrs_X":      c.Hrs_X       = val; break;
                    case "Yarmja_ST":  c.Yarmja_ST   = val; break;
                    case "Yarmja_A":   c.Yarmja_A    = val; break;
                    case "Yarmja_MW":  c.Yarmja_MW   = val; break;
                    case "Yarmja_MVAR":c.Yarmja_MVAR = val; break;
                    case "Qayra_ST":   c.Qayra_ST    = val; break;
                    case "Qayra_A":    c.Qayra_A     = val; break;
                    case "Qayra_MW":   c.Qayra_MW    = val; break;
                    case "Qayra_MVAR": c.Qayra_MVAR  = val; break;
                    case "TR1_KV":     c.TR1_KV      = val; break;
                    case "TR1_A":      c.TR1_A       = val; break;
                    case "TR1_MW":     c.TR1_MW      = val; break;
                    case "TR1_MVAR":   c.TR1_MVAR    = val; break;
                    case "TR2_KV":     c.TR2_KV      = val; break;
                    case "TR2_A":      c.TR2_A       = val; break;
                    case "TR2_MW":     c.TR2_MW      = val; break;
                    case "TR2_MVAR":   c.TR2_MVAR    = val; break;
                    case "TR3_KV":     c.TR3_KV      = val; break;
                    case "TR3_A":      c.TR3_A       = val; break;
                    case "TR3_MW":     c.TR3_MW      = val; break;
                    case "TR3_MVAR":   c.TR3_MVAR    = val; break;
                    case "BC_A":       c.BC_A        = val; break;
                    case "BC_V":       c.BC_V        = val; break;
                    case "BC_Fdd1":    c.BC_Fdd1     = val; break;
                    case "BC_Fdd2":    c.BC_Fdd2     = val; break;
                }
            }
        }

        private void AutoLoad()
        {
            if (!File.Exists(DefaultCoordsPath)) return;
            try { LoadFromFile(DefaultCoordsPath); }
            catch { /* silent */ }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            //try { SaveToFile(DefaultCoordsPath); } catch { }
        }




        // ── Shared Save helper ────────────────────────────────────────────
        private void SaveToFile(string path)
        {
            var c = _printer.Coords;
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            var lines = new System.Collections.Generic.List<string>
            {
                "OffsetX=" + _printer.OffsetXmm.ToString(ic),
                "OffsetY=" + _printer.OffsetYmm.ToString(ic),
                "Hrs_X="   + c.Hrs_X.ToString(ic),
                "Yarmja_ST="  + c.Yarmja_ST.ToString(ic),
                "Yarmja_A="   + c.Yarmja_A.ToString(ic),
                "Yarmja_MW="  + c.Yarmja_MW.ToString(ic),
                "Yarmja_MVAR="+ c.Yarmja_MVAR.ToString(ic),
                "Qayra_ST="   + c.Qayra_ST.ToString(ic),
                "Qayra_A="    + c.Qayra_A.ToString(ic),
                "Qayra_MW="   + c.Qayra_MW.ToString(ic),
                "Qayra_MVAR=" + c.Qayra_MVAR.ToString(ic),
                "TR1_KV="  + c.TR1_KV.ToString(ic),
                "TR1_A="   + c.TR1_A.ToString(ic),
                "TR1_MW="  + c.TR1_MW.ToString(ic),
                "TR1_MVAR="+ c.TR1_MVAR.ToString(ic),
                "TR2_KV="  + c.TR2_KV.ToString(ic),
                "TR2_A="   + c.TR2_A.ToString(ic),
                "TR2_MW="  + c.TR2_MW.ToString(ic),
                "TR2_MVAR="+ c.TR2_MVAR.ToString(ic),
                "TR3_KV="  + c.TR3_KV.ToString(ic),
                "TR3_A="   + c.TR3_A.ToString(ic),
                "TR3_MW="  + c.TR3_MW.ToString(ic),
                "TR3_MVAR="+ c.TR3_MVAR.ToString(ic),
                "BC_A="    + c.BC_A.ToString(ic),
                "BC_V="    + c.BC_V.ToString(ic),
                "BC_Fdd1=" + c.BC_Fdd1.ToString(ic),
                "BC_Fdd2=" + c.BC_Fdd2.ToString(ic),
            };
            File.WriteAllLines(path, lines);
        }

        // ── Shared Load helper ────────────────────────────────────────────
        private void LoadFromFile(string path)
        {
            var c  = _printer.Coords;
            var ic = System.Globalization.NumberStyles.Float;
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                var parts = line.Split('=');
                if (parts.Length != 2) continue;
                if (!float.TryParse(parts[1].Trim(), ic, ci, out float val)) continue;
                switch (parts[0].Trim())
                {
                    case "OffsetX":    _printer.OffsetXmm = val;
                                       if (_nudOffsetX != null) _nudOffsetX.Value = (decimal)val; break;
                    case "OffsetY":    _printer.OffsetYmm = val;
                                       if (_nudOffsetY != null) _nudOffsetY.Value = (decimal)val; break;
                    case "Hrs_X":      c.Hrs_X       = val; break;
                    case "Yarmja_ST":  c.Yarmja_ST   = val; break;
                    case "Yarmja_A":   c.Yarmja_A    = val; break;
                    case "Yarmja_MW":  c.Yarmja_MW   = val; break;
                    case "Yarmja_MVAR":c.Yarmja_MVAR = val; break;
                    case "Qayra_ST":   c.Qayra_ST    = val; break;
                    case "Qayra_A":    c.Qayra_A     = val; break;
                    case "Qayra_MW":   c.Qayra_MW    = val; break;
                    case "Qayra_MVAR": c.Qayra_MVAR  = val; break;
                    case "TR1_KV":     c.TR1_KV      = val; break;
                    case "TR1_A":      c.TR1_A       = val; break;
                    case "TR1_MW":     c.TR1_MW      = val; break;
                    case "TR1_MVAR":   c.TR1_MVAR    = val; break;
                    case "TR2_KV":     c.TR2_KV      = val; break;
                    case "TR2_A":      c.TR2_A       = val; break;
                    case "TR2_MW":     c.TR2_MW      = val; break;
                    case "TR2_MVAR":   c.TR2_MVAR    = val; break;
                    case "TR3_KV":     c.TR3_KV      = val; break;
                    case "TR3_A":      c.TR3_A       = val; break;
                    case "TR3_MW":     c.TR3_MW      = val; break;
                    case "TR3_MVAR":   c.TR3_MVAR    = val; break;
                    case "BC_A":       c.BC_A        = val; break;
                    case "BC_V":       c.BC_V        = val; break;
                    case "BC_Fdd1":    c.BC_Fdd1     = val; break;
                    case "BC_Fdd2":    c.BC_Fdd2     = val; break;
                }
            }
            // Refresh NUD controls to show loaded values
            if (_colRows != null)
            {
                for (int i = 0; i < _colRows.Length; i++)
                    if (_colRows[i].Nud != null)
                        _colRows[i].Nud.Value = (decimal)Math.Round(_colRows[i].GetBase(), 3);
                foreach (var g in _groupNames)
                {
                    _groupOffset[g] = 0f;
                    if (_lastGroupDelta.ContainsKey(g)) _lastGroupDelta[g] = 0f;
                    if (_groupNud.TryGetValue(g, out var gn)) gn.Value = 0;
                }
                RefreshAllEff();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  FORM LAYOUT
        // ══════════════════════════════════════════════════════════════════
        private void BuildForm()
        {
            Text            = "Column Coordinate Calibration";
            Size            = new Size(820, 720);
            MinimumSize     = new Size(700, 500);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            Font            = new Font("Segoe UI", 9f);

            // ── TOP: global offsets ───────────────────────────────────────
            var topPanel = new Panel
            {
                Dock = DockStyle.Top, Height = 64,
                BackColor = Color.FromArgb(30, 60, 110), Padding = new Padding(10, 8, 10, 6)
            };

            var lblTitle = new Label
            {
                Text = "GLOBAL OFFSET  (shifts ALL fields together — X = left/right,  Y = up/down)",
                ForeColor = Color.White, AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(10, 6)
            };
            topPanel.Controls.Add(lblTitle);

            topPanel.Controls.Add(WhiteLabel("Offset X (mm):", new Point(10, 34)));
            _nudOffsetX = Nud(new Point(130, 31), _printer.OffsetXmm, -50, 50, 0.5m);
            _nudOffsetX.BackColor = Color.FromArgb(255, 255, 200);
            topPanel.Controls.Add(_nudOffsetX);

            topPanel.Controls.Add(WhiteLabel("Offset Y (mm):", new Point(250, 34)));
            _nudOffsetY = Nud(new Point(370, 31), _printer.OffsetYmm, -50, 50, 0.5m);
            _nudOffsetY.BackColor = Color.FromArgb(255, 255, 200);
            topPanel.Controls.Add(_nudOffsetY);

            var hint = new Label
            {
                Text = "Tip: Effective X = Global Offset X + Group Offset + Column X",
                ForeColor = Color.FromArgb(180, 210, 255), AutoSize = true,
                Location = new Point(490, 36), Font = new Font("Segoe UI", 7.5f)
            };
            topPanel.Controls.Add(hint);

            // ── SCROLL area ───────────────────────────────────────────────
            _scrollPanel = new Panel
            {
                Dock = DockStyle.Fill, AutoScroll = true,
                BackColor = SystemColors.Window, Padding = new Padding(0)
            };

            // ── BOTTOM: buttons ───────────────────────────────────────────
            var btmPanel = new Panel
            {
                Dock = DockStyle.Bottom, Height = 46,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(8, 7, 8, 7)
            };

            _btnPreview = Btn("🔍 Preview",      Color.FromArgb(0, 120, 215));
            _btnSave    = Btn("💾 Save",          Color.FromArgb(16, 137, 62));
            _btnLoad    = Btn("📂 Load",          Color.FromArgb(96, 96, 96));
            _btnReset   = Btn("↺ Reset All",      Color.FromArgb(180, 90, 0));
            _btnClose   = Btn("✖ Close",          Color.FromArgb(180, 30, 30));

            _lblStatus = new Label
            {
                AutoSize = false, Width = 300, Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.DarkGreen, Location = new Point(10, 12)
            };

            int bx = 10;
            foreach (var b in new[]{ _btnPreview,_btnSave,_btnLoad,_btnReset,_btnClose })
            {
                b.Location = new Point(bx, 8); btmPanel.Controls.Add(b); bx += b.Width + 6;
            }
            _lblStatus.Location = new Point(bx + 6, 12);
            btmPanel.Controls.Add(_lblStatus);

            Controls.Add(_scrollPanel);
            Controls.Add(topPanel);
            Controls.Add(btmPanel);

            _nudOffsetX.ValueChanged += (s,e) => { _printer.OffsetXmm=(float)_nudOffsetX.Value; RefreshAllEff(); };
            _nudOffsetY.ValueChanged += (s,e) => { _printer.OffsetYmm=(float)_nudOffsetY.Value; };
            _btnPreview.Click += OnPreview;
            _btnSave.Click    += OnSave;
            _btnLoad.Click    += OnLoad;
            _btnReset.Click   += OnReset;
            _btnClose.Click   += (s,e) => Close();
        }

        // ── Define every column row ───────────────────────────────────────
        private void BuildColRows()
        {
            var c = _printer.Coords;
            _colRows = new ColRow[]
            {
                // ── Hours ───────────────────────────────────────────────────
                // Hrs_X is INDEPENDENT — its own cell to the left of Yarmja RS.
                // Default 9.0mm = centre of the pre-printed "Hrs" column.
                // Do NOT set this equal to Yarmja_ST or it will overlap KV.
                CR("Hours",    "Hrs X",    "Hour number (own cell, before RS)",
                                           () => c.Hrs_X,       v => c.Hrs_X       = v),

                // ── Yarmja ─────────────────────────────────────────────────
                CR("Yarmja",   "ST (KV)",  "KV voltage",    () => c.Yarmja_ST,   v => c.Yarmja_ST   = v),
                CR("Yarmja",   "A",        "Ampere",         () => c.Yarmja_A,    v => c.Yarmja_A    = v),
                CR("Yarmja",   "MW",       "Active power",   () => c.Yarmja_MW,   v => c.Yarmja_MW   = v),
                CR("Yarmja",   "MVAR",     "Reactive power", () => c.Yarmja_MVAR, v => c.Yarmja_MVAR = v),

                CR("Qayra",    "ST (KV)",  "KV voltage",    () => c.Qayra_ST,    v => c.Qayra_ST    = v),
                CR("Qayra",    "A",        "Ampere",         () => c.Qayra_A,     v => c.Qayra_A     = v),
                CR("Qayra",    "MW",       "Active power",   () => c.Qayra_MW,    v => c.Qayra_MW    = v),
                CR("Qayra",    "MVAR",     "Reactive power", () => c.Qayra_MVAR,  v => c.Qayra_MVAR  = v),

                CR("TR.1 SCR","KV",        "KV",            () => c.TR1_KV,      v => c.TR1_KV      = v),
                CR("TR.1 SCR","A",         "Ampere",         () => c.TR1_A,       v => c.TR1_A       = v),
                CR("TR.1 SCR","MW",        "Active power",   () => c.TR1_MW,      v => c.TR1_MW      = v),
                CR("TR.1 SCR","MVAR",      "Reactive power", () => c.TR1_MVAR,    v => c.TR1_MVAR    = v),

                CR("TR.2 SCR","KV",        "KV",            () => c.TR2_KV,      v => c.TR2_KV      = v),
                CR("TR.2 SCR","A",         "Ampere",         () => c.TR2_A,       v => c.TR2_A       = v),
                CR("TR.2 SCR","MW",        "Active power",   () => c.TR2_MW,      v => c.TR2_MW      = v),
                CR("TR.2 SCR","MVAR",      "Reactive power", () => c.TR2_MVAR,    v => c.TR2_MVAR    = v),

                CR("TR.3 SCR","KV",        "KV",            () => c.TR3_KV,      v => c.TR3_KV      = v),
                CR("TR.3 SCR","A",         "Ampere",         () => c.TR3_A,       v => c.TR3_A       = v),
                CR("TR.3 SCR","MW",        "Active power",   () => c.TR3_MW,      v => c.TR3_MW      = v),
                CR("TR.3 SCR","MVAR",      "Reactive power", () => c.TR3_MVAR,    v => c.TR3_MVAR    = v),

                CR("BC",       "A",        "Bus coupler A",  () => c.BC_A,        v => c.BC_A        = v),
                CR("BC",       "V",        "Bus coupler V",  () => c.BC_V,        v => c.BC_V        = v),
                CR("BC",       "Fdd1",     "Feeder 1",       () => c.BC_Fdd1,     v => c.BC_Fdd1     = v),
                CR("BC",       "Fdd2",     "Feeder 2",       () => c.BC_Fdd2,     v => c.BC_Fdd2     = v),
            };
        }

        // ══════════════════════════════════════════════════════════════════
        //  BUILD SCROLLABLE CONTENT
        //  Uses a simple vertical Panel stack — no FlowLayoutPanel nesting,
        //  no null-forgiving operators (compatible with .NET Framework 4.7)
        // ══════════════════════════════════════════════════════════════════
        private void BuildScrollContent()
        {
            // One master panel that grows downward; placed inside _scrollPanel
            var master = new Panel
            {
                AutoSize     = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Location     = new Point(0, 0)
            };

            const int ROW_H   = 28;   // data row height px
            const int HDR_H   = 34;   // group header bar height px
            const int SUBHDR_H= 22;   // column sub-header height px
            const int COL_W   = 448;  // total table width (4 cols × widths)
            const int GROUP_GAP = 8;  // vertical gap between groups

            int col0 = 90, col1 = 130, col2 = 110, col3 = COL_W - col0 - col1 - col2;

            int yPos = 4;   // running Y position in master panel

            string lastSec = "";
            int groupDataTop = 0;   // Y where current group's data rows start
            Panel currentGroupPanel = null;

            for (int ri = 0; ri < _colRows.Length; ri++)
            {
                var row = _colRows[ri];

                // ── Start a new group ──────────────────────────────────────
                if (row.Section != lastSec)
                {
                    lastSec   = row.Section;
                    Color sc  = SectionColor(row.Section);
                    Color dark = DarkenColor(sc, 45);

                    // Outer group panel (will be sized after rows are added)
                    currentGroupPanel = new Panel
                    {
                        Left        = 4,
                        Top         = yPos,
                        Width       = COL_W + 12,
                        BackColor   = sc,
                        BorderStyle = BorderStyle.FixedSingle
                    };
                    master.Controls.Add(currentGroupPanel);
                    int localY = 0;

                    // ── Group header bar ──────────────────────────────────
                    var hdrPanel = new Panel
                    {
                        Left      = 0, Top = 0,
                        Width     = COL_W + 12, Height = HDR_H,
                        BackColor = dark
                    };

                    var lblSec = new Label
                    {
                        Text      = "  " + row.Section,
                        ForeColor = Color.White,
                        Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                        Left = 2, Top = 4, Width = 200, Height = 24
                    };
                    hdrPanel.Controls.Add(lblSec);

                    var lblGO = new Label
                    {
                        Text      = "Group offset X (mm):",
                        ForeColor = Color.FromArgb(220, 235, 255),
                        Left = 210, Top = 8, AutoSize = true
                    };
                    hdrPanel.Controls.Add(lblGO);

                    var nudGrp = new NumericUpDown
                    {
                        Minimum = -100m, Maximum = 100m,
                        DecimalPlaces = 3, Increment = 0.5m, Value = 0,
                        Width = 90, Left = 380, Top = 6,
                        BackColor = Color.FromArgb(255, 255, 200)
                    };
                    hdrPanel.Controls.Add(nudGrp);
                    _groupNud[row.Section] = nudGrp;

                    string secCapture = row.Section;
                    nudGrp.ValueChanged += (s, e) =>
                    {
                        _groupOffset[secCapture] = (float)nudGrp.Value;
                        ApplyGroupOffset(secCapture);
                        RefreshAllEff();
                    };

                    currentGroupPanel.Controls.Add(hdrPanel);
                    localY += HDR_H;

                    // ── Column sub-header ─────────────────────────────────
                    var subHdr = new Panel
                    {
                        Left = 0, Top = localY, Width = COL_W + 12, Height = SUBHDR_H,
                        BackColor = Color.FromArgb(230, 230, 230)
                    };
                    int sx = 0;
                    foreach (var (txt, w) in new[]{
                        ("Column", col0), ("Description", col1),
                        ("X (mm)", col2), ("Effective X", col3)})
                    {
                        subHdr.Controls.Add(new Label
                        {
                            Text      = txt, Left = sx, Top = 0, Width = w, Height = SUBHDR_H,
                            TextAlign = ContentAlignment.MiddleCenter,
                            Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                            ForeColor = Color.FromArgb(50, 50, 80),
                            BorderStyle = BorderStyle.FixedSingle
                        });
                        sx += w;
                    }
                    currentGroupPanel.Controls.Add(subHdr);
                    localY += SUBHDR_H;

                    groupDataTop = localY;   // remember where data rows begin

                    // ── Close this group's height once we know rows count ──
                    // We'll do it after the loop; store localY reference via Tag
                    currentGroupPanel.Tag = (object)groupDataTop;

                    yPos += HDR_H + SUBHDR_H;   // advance master Y for this section
                }

                // ── One data row ───────────────────────────────────────────
                int gdt  = (int)currentGroupPanel.Tag;
                int rowY = gdt + (ri - FirstRowOfSection(ri)) * ROW_H;

                int lx = 0;
                Color bg = SectionColor(row.Section);

                // Column name
                currentGroupPanel.Controls.Add(new Label
                {
                    Text = row.ColName, Left = lx, Top = rowY, Width = col0, Height = ROW_H,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    BackColor = bg, BorderStyle = BorderStyle.FixedSingle
                }); lx += col0;

                // Description
                currentGroupPanel.Controls.Add(new Label
                {
                    Text = row.Desc, Left = lx, Top = rowY, Width = col1, Height = ROW_H,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding   = new Padding(4, 0, 0, 0),
                    BackColor = bg, BorderStyle = BorderStyle.FixedSingle
                }); lx += col1;

                // X NUD
                var nud = new NumericUpDown
                {
                    Minimum = -20m, Maximum = 500m,
                    DecimalPlaces = 3, Increment = 0.5m,
                    Value     = (decimal)Math.Round(row.GetBase(), 3),
                    Left = lx, Top = rowY, Width = col2, Height = ROW_H,
                    BorderStyle = BorderStyle.None, Tag = ri
                };
                nud.ValueChanged += OnColNudChanged;
                _colRows[ri].Nud = nud;
                currentGroupPanel.Controls.Add(nud);
                lx += col2;

                // Effective X label
                var lblEff = new Label
                {
                    Text      = EffText(ri),
                    Left = lx, Top = rowY, Width = col3, Height = ROW_H,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.DarkBlue,
                    Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    BackColor = bg, BorderStyle = BorderStyle.FixedSingle
                };
                _colRows[ri].LblEff = lblEff;
                currentGroupPanel.Controls.Add(lblEff);

                // Grow group panel height and master Y on each data row
                int newGroupH = (int)currentGroupPanel.Tag + (ri - FirstRowOfSection(ri) + 1) * ROW_H + 2;
                currentGroupPanel.Height = newGroupH;
                yPos = currentGroupPanel.Bottom + GROUP_GAP;
            }

            master.Height = yPos + 4;

            _scrollPanel.Controls.Clear();
            _scrollPanel.Controls.Add(master);
        }

        // Returns the index of the first row that belongs to the same section as row[ri]
        private int FirstRowOfSection(int ri)
        {
            string sec = _colRows[ri].Section;
            for (int i = ri - 1; i >= 0; i--)
                if (_colRows[i].Section != sec) return i + 1;
            return 0;
        }

        // ══════════════════════════════════════════════════════════════════
        //  GROUP OFFSET LOGIC
        // ══════════════════════════════════════════════════════════════════

        // Tracks the last applied group delta so we can un-apply before re-applying
        private readonly Dictionary<string, float> _lastGroupDelta = new Dictionary<string, float>();

        private void ApplyGroupOffset(string section)
        {
            float prev  = _lastGroupDelta.ContainsKey(section) ? _lastGroupDelta[section] : 0f;
            float now   = _groupOffset[section];
            float delta = now - prev;
            _lastGroupDelta[section] = now;

            // Add delta to every base coord in this group
            for (int i = 0; i < _colRows.Length; i++)
            {
                if (_colRows[i].Section != section) continue;
                float newBase = _colRows[i].GetBase() + delta;
                _colRows[i].SetBase(newBase);
                _colRows[i].Nud.Value = (decimal)Math.Round(newBase, 3);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  REFRESH
        // ══════════════════════════════════════════════════════════════════
        private void RefreshAllEff()
        {
            for (int i = 0; i < _colRows.Length; i++)
                if (_colRows[i].LblEff != null)
                    _colRows[i].LblEff.Text = EffText(i);
        }

        private string EffText(int ri)
        {
            float eff = _colRows[ri].GetBase() + (float)_nudOffsetX.Value;
            return $"{eff:F2} mm";
        }

        private void OnColNudChanged(object sender, EventArgs e)
        {
            var nud = (NumericUpDown)sender;
            int  ri  = (int)nud.Tag;
            _colRows[ri].SetBase((float)nud.Value);
            if (_colRows[ri].LblEff != null)
                _colRows[ri].LblEff.Text = EffText(ri);
        }

        // ══════════════════════════════════════════════════════════════════
        //  BUTTONS
        // ══════════════════════════════════════════════════════════════════
        private void OnPreview(object sender, EventArgs e)
        {
            _printer.OffsetXmm = (float)_nudOffsetX.Value;
            _printer.OffsetYmm = (float)_nudOffsetY.Value;

            bool was = _printer.ShowImage;
            _printer.ShowImage = true;
            try
            {
                if (Owner is Form1 f1)
                    f1.OpenCalibrationPreview(_printer, this);
                else
                    MessageBox.Show("Open this dialog from Form1 to use Preview.",
                        "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Preview failed:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { _printer.ShowImage = was; }

            _lblStatus.Text = "Preview closed — adjust and preview again, or Close when done.";
        }


        private bool VerifySavePassword()
        {
            const string CORRECT_PASSWORD = "1254"; // 🔴 change this

            using (var dlg = new PasswordDialog())
            {
                var result = dlg.ShowDialog();

                // ❌ Closed or Cancel → block save
                if (result != DialogResult.OK)
                    return false;

                // ❌ Wrong password → block save
                if (dlg.EnteredPassword != CORRECT_PASSWORD)
                {
                    MessageBox.Show("Incorrect password. Save blocked.",
                        "Access Denied",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return false;
                }

                return true; // ✅ allowed
            }
        }

        private void OnSave(object sender, EventArgs e)
        {

            // 🔐 MUST PASS PASSWORD FIRST
            if (!VerifySavePassword())
            {
                if (this.Owner is Form1 f1)
                    f1.SetStatus("Save cancelled — invalid or missing password.", 4000);
                return; // ⛔ HARD STOP
            }


            using (var sfd = new SaveFileDialog
            {
                Title    = "Save coordinates",
                Filter   = "Coordinate file (*.coords)|*.coords|All files (*.*)|*.*",
                FileName = "logsheet_coords.coords"
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;
                try
                {
                    SaveToFile(sfd.FileName);
                    _lblStatus.Text = "✔ Saved to " + System.IO.Path.GetFileName(sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Save failed:\n" + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog
            {
                Title  = "Load coordinates",
                Filter = "Coordinate file (*.coords)|*.coords|All files (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog() != DialogResult.OK) return;
                try
                {
                    LoadFromFile(ofd.FileName);
                    _lblStatus.Text = "✔ Loaded from " + System.IO.Path.GetFileName(ofd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Load failed:\n" + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnReset(object sender, EventArgs e)
        {
            if (MessageBox.Show("Reset ALL coordinates to factory defaults?",
                    "Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                != DialogResult.Yes) return;

            _printer.Coords.ResetToDefaults();
            _nudOffsetX.Value = 0;
            _nudOffsetY.Value = 0;
            _printer.OffsetXmm = 0f;
            _printer.OffsetYmm = 0f;

            foreach (var g in _groupNames)
            {
                _groupOffset[g] = 0f; _lastGroupDelta[g] = 0f;
                if (_groupNud.TryGetValue(g, out var gn)) gn.Value = 0;
            }
            for (int i = 0; i < _colRows.Length; i++)
                _colRows[i].Nud.Value = (decimal)Math.Round(_colRows[i].GetBase(), 3);
            RefreshAllEff();
            _lblStatus.Text = "✔ Reset to factory defaults";
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════

        private static Color SectionColor(string s)
        {
            switch (s)
            {
                case "Hours":    return Color.FromArgb(235, 235, 235);
                case "Yarmja":   return Color.FromArgb(255, 250, 220);
                case "Qayra":    return Color.FromArgb(220, 238, 255);
                case "TR.1 SCR": return Color.FromArgb(220, 255, 225);
                case "TR.2 SCR": return Color.FromArgb(242, 228, 255);
                case "TR.3 SCR": return Color.FromArgb(255, 228, 228);
                case "BC":       return Color.FromArgb(255, 242, 210);
                default:         return SystemColors.Window;
            }
        }

        private static Color DarkenColor(Color c, int amount) =>
            Color.FromArgb(
                Math.Max(0, c.R - amount),
                Math.Max(0, c.G - amount),
                Math.Max(0, c.B - amount));

        private static Label WhiteLabel(string text, Point loc) => new Label
        {
            Text = text, Location = loc, AutoSize = true,
            ForeColor = Color.White, TextAlign = ContentAlignment.MiddleLeft
        };

        private static NumericUpDown Nud(Point loc, float val, float min, float max, decimal inc)
            => new NumericUpDown
            {
                Location      = loc, Width = 90,
                Minimum       = (decimal)min, Maximum = (decimal)max,
                DecimalPlaces = 3, Increment = inc,
                Value         = (decimal)Math.Round(Math.Max(min, Math.Min(max, val)), 3)
            };

        private static Button Btn(string text, Color back) => new Button
        {
            Text = text, Width = 110, Height = 30,
            BackColor = back, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f)
        };

        private static ColRow CR(string sec, string col, string desc,
                                  Func<float> get, Action<float> set)
            => new ColRow { Section=sec, ColName=col, Desc=desc, GetBase=get, SetBase=set };
    }
}
