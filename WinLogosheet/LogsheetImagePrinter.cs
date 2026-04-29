using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;

namespace WinLogosheet
{
    /// <summary>
    /// Prints text onto the pre-printed logsheet paper.
    ///
    /// Paper size   : 450 mm W × 246.5 mm H  (landscape custom)
    /// Hours        : Hr 1 … Hr 24  (24 rows; workday spans 8 AM today → 7 AM next day)
    ///
    /// Coordinate system : GraphicsUnit.Millimeter (mm from paper top-left)
    /// All positions derived from pixel analysis of LogsheetTemplate.jpg (1046×568 px):
    ///     x_mm = pixel_x × 450  / 1046
    ///     y_mm = pixel_y × 246.5 / 568
    ///
    /// ShowImage = false  →  text only on real pre-printed paper  (production)
    /// ShowImage = true   →  scanned JPG background + red text overlay
    ///                       Use to verify coordinate alignment before real paper.
    ///
    /// Template file: LogsheetTemplate.jpg  (place next to EXE)
    /// OffsetXmm / OffsetYmm  nudge all text together if printer has a small offset.
    /// </summary>
    public class LogsheetTextPrinter : IDisposable
    {
        // ══════════════════════════════════════════════════════════════════
        //  COLUMN COORDINATES — editable via CoordinateCalibrationDialog
        // ══════════════════════════════════════════════════════════════════

        /// <summary>All X-axis column positions in mm. Edit via the calibration dialog.</summary>
        public ColumnCoords Coords { get; } = new ColumnCoords();

        // ══════════════════════════════════════════════════════════════════
        //  CALIBRATION  — global offset nudges ALL fields together
        // ══════════════════════════════════════════════════════════════════
        public float OffsetXmm { get; set; } = 0f;   // + = right,  - = left
        public float OffsetYmm { get; set; } = 0f;   // + = down,   - = up

        // ══════════════════════════════════════════════════════════════════
        //  MODE
        // ══════════════════════════════════════════════════════════════════
        /// <summary>
        /// false = text only (production — real pre-printed paper).
        /// true  = scanned form image as background + red text overlay (alignment check).
        /// </summary>
        public bool ShowImage { get; set; } = false;

        /// <summary>Full path to LogsheetTemplate.jpg (defaults to folder next to EXE).</summary>
        public string TemplatePath { get; set; } =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogsheetTemplate.jpg");

        // ══════════════════════════════════════════════════════════════════
        //  FONT SETTINGS
        // ══════════════════════════════════════════════════════════════════
        public float  DataFontMm   { get; set; } = 3.2f;   // ≈ 6.2 pt
        public float  HeaderFontMm { get; set; } = 3.5f;   // ≈ 7 pt
        public string FontName     { get; set; } = "Arial";

        // ══════════════════════════════════════════════════════════════════
        //  PAPER GEOMETRY
        // ══════════════════════════════════════════════════════════════════
        private const float PAPER_W = 450.0f;    // mm — custom landscape width
        private const float PAPER_H = 246.5f;    // mm — custom landscape height

        // ══════════════════════════════════════════════════════════════════
        //  COORDINATE MAP  (mm from paper top-left)
        //  Source image: 1046 × 568 px  →  x_mm = px × 450/1046
        //                                   y_mm = px × 246.5/568
        //
        //  Grid: GRID_TOP=100px, GRID_BOT=560px, 17 rows (Hr 8–24)
        //  Row height = (560-100)/17 = 27.059 px = 11.74 mm
        //  Row Y centre = (100 + (hr-8)*27.059 + 13.529) × 246.5/568
        // ══════════════════════════════════════════════════════════════════

        private const float GRID_TOP_PX = 99f;   // top border of Hr 1 (verified from template image)
        private const float GRID_BOT_PX  = 537f;  // bottom border of Hr 24
        private const float ROW_H_PX    = (GRID_BOT_PX - GRID_TOP_PX) / 24f;  // 18.208 px per row
        private const int   FIRST_HOUR  = 1;
        private const int   LAST_HOUR   = 24;

        private static float RowY(int hour)
        {
            // Hr1→index 0, Hr8→index 7, Hr24→index 23 — each centred in its row
            float cy_px = GRID_TOP_PX + (hour - 1) * ROW_H_PX + ROW_H_PX / 2f;
            return cy_px * PAPER_H / 568f;
        }

        // ── Header positions (mm) ──────────────────────────────────────────
        // "SUBSTATION" dotted area is left of DATE; "/20" is right of DATE
        private const float HDR_SUBSTATION_X = 60.0f;    // after "STATION ..."
        private const float HDR_DATE_X       = 295.0f;   // under "DATE"
        private const float HDR_YEAR_X       = 410.0f;   // under "/20"
        private const float HDR_Y            = 10.0f;    // header row

        // ── LINE: Yarmja  [RS, ST, TR, A, MW, MVAR] (mm) ─────────────────
        // User note: KV reading → ST column
        // YARMJA column centres (mm).  Only ST, A, MW, MVAR are written.
        // col1=6.0mm=RS(unused), col2=13.3mm=ST, col3=20.2mm=TR(unused),
        // col4=27.5mm=A, col5=34.4mm=A2?, col6=42.2mm=MW, col7=47.3mm=MVAR
        private static readonly float[] YARMJA = {
            6.023f,   // RS   — unused
            13.337f,  // ST   ← KV value here  (was 20.22 — shifted one col left)
            20.220f,  // TR   — unused
            34.417f,  // A
            42.161f,  // MW
            47.323f,  // MVAR
        };
        private const int Y_RS=0, Y_ST=1, Y_TR=2, Y_A=3, Y_MW=4, Y_MVAR=5;

        // ── LINE: Qayra  [RS, ST, TR, A, MW] (mm) ────────────────────────
        // User note: KV reading → ST column
        // QAYRA column centres (mm).  Only ST, A, MW, MVAR are written.
        private static readonly float[] QAYRA = {
            45.170f,  // RS   — unused
            52.055f,  // ST   ← KV value here  (was 59.37 — shifted one col left)
            59.369f,  // TR   — unused
            66.252f,  // A
            73.136f,  // MW
            80.449f,  // MVAR
        };
        private const int Q_RS=0, Q_ST=1, Q_TR=2, Q_A=3, Q_MW=4, Q_MVAR=5;

        // ── TR.1  SCR section  [KV, A, MW, MVAR] (mm) ────────────────────
        // TR1 SCR — shifted one column left vs previous
        private static readonly float[] TR1_SCR = {
            119.168f,  // KV
            126.052f,  // A
            133.365f,  // MW
            140.679f,  // MVAR
        };

        // ── TR.2  SCR section  [KV, A, MW, MVAR] (mm) ────────────────────
        // TR2 SCR — shifted one column left vs previous
        private static readonly float[] TR2_SCR = {
            208.652f,  // KV
            215.535f,  // A
            222.419f,  // MW
            229.732f,  // MVAR
        };

        // ── TR.3  SCR section  [KV, A, MW, MVAR] (mm) ────────────────────
        // TR3 SCR — shifted one column left vs previous
        private static readonly float[] TR3_SCR = {
            306.310f,  // KV
            313.193f,  // A
            320.507f,  // MW
            327.820f,  // MVAR
        };
        private const int SCR_KV=0, SCR_A=1, SCR_MW=2, SCR_MVAR=3;

        // ── BC section  [A, V, Fdd1, Fdd2] (mm) ──────────────────────────
        private const float BC_A    = 373.42f;  // col 53
        private const float BC_V    = 380.74f;  // col 54
        private const float BC_FDD1 = 390.20f;  // col 56
        private const float BC_FDD2 = 397.94f;  // col 57

        // ══════════════════════════════════════════════════════════════════
        //  INTERNAL STATE
        // ══════════════════════════════════════════════════════════════════
        private List<TextField> _fields;
        private PrintDocument   _pd;
        private Bitmap          _template;

        private class TextField {
            public string Text;
            public float  Xmm, Ymm, FontMm;
            public bool   Bold;
            public HorizontalAlignment Align = HorizontalAlignment.Center;
        }

        // ══════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════════════

        public void PrintPreview(ListView lv,
            string substationName = "", string date = "", string[] lineNames = null)
        {
            Build(lv, substationName, date, lineNames);
            using (var dlg = new PrintPreviewDialog
                { Document = _pd, WindowState = FormWindowState.Maximized })
                dlg.ShowDialog();
        }

        /// <summary>
        /// Open a PrintPreviewDialog parented to <paramref name="owner"/> so
        /// it appears on top of the calibration dialog, which stays open behind it.
        /// </summary>
        public void PrintPreview(ListView lv, string substationName, string date,
                                 string[] lineNames, IWin32Window owner)
        {
            Build(lv, substationName, date, lineNames);
            using (var dlg = new PrintPreviewDialog
                { Document = _pd, WindowState = FormWindowState.Maximized })
                dlg.ShowDialog(owner);   // owner = calibration dialog
        }

        public void Print(ListView lv,
            string substationName = "", string date = "", string[] lineNames = null)
        {
            Build(lv, substationName, date, lineNames);
            _pd.Print();
        }

        public void Dispose()
        {
            _template?.Dispose();
            _template = null;
            _pd?.Dispose();
            _pd = null;
        }

      

        // ══════════════════════════════════════════════════════════════════
        //  BUILD FIELD LIST
        // ══════════════════════════════════════════════════════════════════
        private void Build(ListView lv,
            string substationName, string date, string[] lineNames)
        {
            _fields = new List<TextField>();

            // ── Load template image (ShowImage mode) ──────────────────────
            _template?.Dispose();
            _template = null;
            if (ShowImage)
            {
                if (!File.Exists(TemplatePath))
                {
                    MessageBox.Show(
                        $"Template image not found:\n{TemplatePath}\n\n" +
                        "Place LogsheetTemplate.jpg next to the EXE.",
                        "ShowImage Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ShowImage = false;
                }
                else
                {
                    _template = new Bitmap(TemplatePath);  // owned, not in a using
                }
            }

            // ── Header ────────────────────────────────────────────────────
            Add(substationName, HDR_SUBSTATION_X, HDR_Y,
                HeaderFontMm, bold:true, HorizontalAlignment.Left);
            Add(date, HDR_DATE_X, HDR_Y,
                HeaderFontMm, bold:true, HorizontalAlignment.Center);

            if (lineNames != null)
                for (int i = 0; i < Math.Min(lineNames.Length, 6); i++)
                    Add(lineNames[i], YARMJA[0] + i * 10f, 22f,    // approx label row
                        HeaderFontMm, bold:false, HorizontalAlignment.Center);

            // ── Data rows  Hr 8 … Hr 24 ───────────────────────────────────
            // ListView sub-item layout (existing app, unchanged):
            //  [0]      = Hour
            //  [1-4]    = Yarema  : KV, A, MW, MVAR
            //  [5-8]    = Qayara  : KV, A, MW, MVAR
            //  [9-12]   = T1      : KV(SCR), A, MW, MVAR  → TR.1 SCR
            //  [13-16]  = T2      : KV(SCR), A, MW, MVAR  → TR.2 SCR
            //  [17-20]  = T3      : KV(SCR), A, MW, MVAR  → TR.3 SCR
            //  [21-24]  = Fdd     : Fdd1-A, Fdd1-V, Fdd2-A, Fdd2-V → BC section

            foreach (ListViewItem item in lv.Items)
            {
                if (!int.TryParse(item.Text, out int hour)) continue;
                if (hour == 0) hour = 24;
                // Only Hr 8–24
                if (hour < FIRST_HOUR || hour > LAST_HOUR) continue;
                // Hours the user marked as blank-on-print: leave the row empty
                if ((item.Tag as string) == "skip") continue;

                float cy = RowY(hour);

                // ── Hour number (printed at RS column position of Yarmja)
                AddData(hour.ToString(), Coords.Hrs_X, cy);

                // ── Yarmja: ST=KV, A, MW, MVAR  (coords from Coords object)
                AddSub(item,  1, Coords.Yarmja_ST,   cy);   // KV → ST
                AddSub(item,  2, Coords.Yarmja_A,    cy);   // A
                AddSub(item,  3, Coords.Yarmja_MW,   cy);   // MW
                AddSub(item,  4, Coords.Yarmja_MVAR, cy);   // MVAR

                // ── Qayra: ST=KV, A, MW, MVAR
                AddSub(item,  5, Coords.Qayra_ST,   cy);    // KV → ST
                AddSub(item,  6, Coords.Qayra_A,    cy);    // A
                AddSub(item,  7, Coords.Qayra_MW,   cy);    // MW
                AddSub(item,  8, Coords.Qayra_MVAR, cy);    // MVAR

                // ── T1 → TR.1 SCR
                AddSub(item,  9, Coords.TR1_KV,   cy);
                AddSub(item, 10, Coords.TR1_A,    cy);
                AddSub(item, 11, Coords.TR1_MW,   cy);
                AddSub(item, 12, Coords.TR1_MVAR, cy);

                // ── T2 → TR.2 SCR
                AddSub(item, 13, Coords.TR2_KV,   cy);
                AddSub(item, 14, Coords.TR2_A,    cy);
                AddSub(item, 15, Coords.TR2_MW,   cy);
                AddSub(item, 16, Coords.TR2_MVAR, cy);

                // ── T3 → TR.3 SCR
                AddSub(item, 17, Coords.TR3_KV,   cy);
                AddSub(item, 18, Coords.TR3_A,    cy);
                AddSub(item, 19, Coords.TR3_MW,   cy);
                AddSub(item, 20, Coords.TR3_MVAR, cy);

                // ── Fdd → BC section
                AddSub(item, 21, Coords.BC_A,    cy);
                AddSub(item, 22, Coords.BC_V,    cy);
                AddSub(item, 23, Coords.BC_Fdd1, cy);
                AddSub(item, 24, Coords.BC_Fdd2, cy);
            }

            // ── Configure PrintDocument ────────────────────────────────────
            _pd = new PrintDocument();
            _pd.DocumentName = ShowImage ? "Logsheet [IMAGE CHECK]" : "Nineveh Logsheet";

            // Custom paper: 450 mm W × 246.5 mm H  (PaperSize unit = 1/100 inch)
            // Feed as landscape: set PaperSize to portrait values then Landscape=true
            // Portrait short side = 246.5 mm, long side = 450 mm
            int shortIn = (int)(PAPER_H / 25.4 * 100);  // 246.5 mm
            int longIn  = (int)(PAPER_W  / 25.4 * 100); // 450 mm
            _pd.DefaultPageSettings.PaperSize =
                new PaperSize("Custom_450x246", shortIn, longIn);
            _pd.DefaultPageSettings.Landscape = true;
            _pd.DefaultPageSettings.Margins   = new Margins(0, 0, 0, 0);

            _pd.PrintPage += OnPrintPage;
        }

        // ══════════════════════════════════════════════════════════════════
        //  PRINT PAGE  — GraphicsUnit.Millimeter, no ScaleTransform
        // ══════════════════════════════════════════════════════════════════
        private void OnPrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            g.PageUnit = GraphicsUnit.Millimeter;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // Printer hard-margin compensation (1/100 inch → mm)
            float hmx = e.PageSettings.HardMarginX * 25.4f / 100f;
            float hmy = e.PageSettings.HardMarginY * 25.4f / 100f;

            GraphicsState state = g.Save();

            // Apply hard-margin offset + user calibration offsets.
            // OffsetXmm and OffsetYmm shift EVERY field together.
            g.TranslateTransform(-hmx + OffsetXmm, -hmy + OffsetYmm);

            // ── 1. Background image (ShowImage = true) ─────────────────────
            if (ShowImage && _template != null)
            {
                // Stretch the full template image to fill the entire paper area
                var destRect = new RectangleF(0f, 0f, PAPER_W, PAPER_H);
                var srcRect  = new RectangleF(0f, 0f, _template.Width, _template.Height);
                g.DrawImage(_template, destRect, srcRect, GraphicsUnit.Pixel);
            }

            // ── 2. Text fields ─────────────────────────────────────────────
            foreach (var f in _fields)
                DrawField(g, f);

            g.Restore(state);
            e.HasMorePages = false;
        }

        private void DrawField(Graphics g, TextField f)
        {
            // Font in Millimeter units — size directly in mm, no scaling needed
            using (var font  = new Font(FontName, f.FontMm,
                                   f.Bold ? FontStyle.Bold : FontStyle.Regular,
                                   GraphicsUnit.Millimeter))
            using (var brush = new SolidBrush(ShowImage ? Color.Red : Color.Black))
            {
                SizeF sz = g.MeasureString(f.Text, font);
                float x  = f.Xmm;
                float y  = f.Ymm - sz.Height / 2f;   // vertically centred on Ymm

                switch (f.Align)
                {
                    case HorizontalAlignment.Center: x -= sz.Width / 2f; break;
                    case HorizontalAlignment.Right:  x -= sz.Width;      break;
                }
                g.DrawString(f.Text, font, brush, x, y);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════
        private void Add(string text, float xmm, float ymm,
            float fontMm, bool bold, HorizontalAlignment align)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            _fields.Add(new TextField
                { Text=text, Xmm=xmm, Ymm=ymm, FontMm=fontMm, Bold=bold, Align=align });
        }

        // Writes a plain string value (e.g. the hour number) at (xmm, ymm)
        private void AddData(string text, float xmm, float ymm)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            _fields.Add(new TextField
                { Text=text, Xmm=xmm, Ymm=ymm, FontMm=DataFontMm,
                  Bold=false, Align=HorizontalAlignment.Center });
        }

        private void AddSub(ListViewItem item, int si, float xmm, float ymm)
        {
            if (si >= item.SubItems.Count) return;
            string t = item.SubItems[si].Text;
            if (string.IsNullOrWhiteSpace(t)) return;
            _fields.Add(new TextField
                { Text=t, Xmm=xmm, Ymm=ymm, FontMm=DataFontMm,
                  Bold=false, Align=HorizontalAlignment.Center });
        }
    }
    /// <summary>
    /// All column X-coordinates in mm. Editable at runtime via CoordinateCalibrationDialog.
    /// Call ResetToDefaults() to restore factory values.
    /// </summary>
    public class ColumnCoords
    {
        // ── Hours column ────────────────────────────────────────────────
        public float Hrs_X       { get; set; } = 9.0f;    // Hour number — centre of the Hrs cell (between left border & RS)

        // ── Yarmja ──────────────────────────────────────────────────────
        public float Yarmja_ST   { get; set; } = 13.337f;   // KV
        public float Yarmja_A    { get; set; } = 34.417f;
        public float Yarmja_MW   { get; set; } = 42.161f;
        public float Yarmja_MVAR { get; set; } = 47.323f;

        // ── Qayra ───────────────────────────────────────────────────────
        public float Qayra_ST    { get; set; } = 52.055f;   // KV
        public float Qayra_A     { get; set; } = 66.252f;
        public float Qayra_MW    { get; set; } = 73.136f;
        public float Qayra_MVAR  { get; set; } = 80.449f;

        // ── TR.1 SCR ────────────────────────────────────────────────────
        public float TR1_KV      { get; set; } = 119.168f;
        public float TR1_A       { get; set; } = 126.052f;
        public float TR1_MW      { get; set; } = 133.365f;
        public float TR1_MVAR    { get; set; } = 140.679f;

        // ── TR.2 SCR ────────────────────────────────────────────────────
        public float TR2_KV      { get; set; } = 208.652f;
        public float TR2_A       { get; set; } = 215.535f;
        public float TR2_MW      { get; set; } = 222.419f;
        public float TR2_MVAR    { get; set; } = 229.732f;

        // ── TR.3 SCR ────────────────────────────────────────────────────
        public float TR3_KV      { get; set; } = 306.310f;
        public float TR3_A       { get; set; } = 313.193f;
        public float TR3_MW      { get; set; } = 320.507f;
        public float TR3_MVAR    { get; set; } = 327.820f;

        // ── BC ──────────────────────────────────────────────────────────
        public float BC_A        { get; set; } = 373.423f;
        public float BC_V        { get; set; } = 380.736f;
        public float BC_Fdd1     { get; set; } = 390.201f;
        public float BC_Fdd2     { get; set; } = 397.945f;

        /// <summary>Restore all coordinates to the factory-calibrated values.</summary>
        public void ResetToDefaults()
        {
            Hrs_X       = 9.0f;
            Yarmja_ST   = 13.337f;  Yarmja_A  = 34.417f;
            Yarmja_MW   = 42.161f;  Yarmja_MVAR = 47.323f;
            Qayra_ST    = 52.055f;  Qayra_A   = 66.252f;
            Qayra_MW    = 73.136f;  Qayra_MVAR  = 80.449f;
            TR1_KV      = 119.168f; TR1_A     = 126.052f;
            TR1_MW      = 133.365f; TR1_MVAR  = 140.679f;
            TR2_KV      = 208.652f; TR2_A     = 215.535f;
            TR2_MW      = 222.419f; TR2_MVAR  = 229.732f;
            TR3_KV      = 306.310f; TR3_A     = 313.193f;
            TR3_MW      = 320.507f; TR3_MVAR  = 327.820f;
            BC_A        = 373.423f; BC_V      = 380.736f;
            BC_Fdd1     = 390.201f; BC_Fdd2   = 397.945f;
        }
    }

}
