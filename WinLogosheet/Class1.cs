using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace WinLogosheet
{
    public class LabelPrinter
    {
        private readonly List<LabelInfo> _frontLabels = new List<LabelInfo>();
        private readonly List<LabelInfo> _backLabels = new List<LabelInfo>();
        private Ruler _ruler;
        private int _currentPageIndex = 0;
        private bool _landscape;

        public class LabelInfo
        {
            public string Text { get; }
            public float XMm { get; }   // X coordinate in mm (0-250 from left)
            public float YMm { get; }   // Y coordinate in mm (0-680 from top)
            public Font Font { get; }
            public Color Color { get; }
            public bool OwnsFont { get; }

            public LabelInfo(string text, float xMm, float yMm, Font font, Color color, bool ownsFont)
            {
                Text = text;
                XMm = xMm;
                YMm = yMm;
                Font = font;
                Color = color;
                OwnsFont = ownsFont;
            }
        }

        // Add this method to enable ruler printing
        public void EnableRuler(SizeF pageSizeMm, int topMm, int rightMm, int bottomMm, int leftMm)
        {
            _ruler = new Ruler(pageSizeMm, topMm, rightMm, bottomMm, leftMm);
        }

        // Overload for default margins
        public void EnableRuler(SizeF pageSizeMm)
        {
            _ruler = new Ruler(pageSizeMm, 5, 5, 5, 5);
        }

        // Method to disable ruler
        public void DisableRuler()
        {
            _ruler = null;
        }

        public void AddFrontLabel(string text, float xMm, float yMm, Font font = null, Color? color = null)
        {
            _frontLabels.Add(CreateLabel(text, xMm, yMm, font, color));
        }

        public void AddBackLabel(string text, float xMm, float yMm, Font font = null, Color? color = null)
        {
            _backLabels.Add(CreateLabel(text, xMm, yMm, font, color));
        }

        private LabelInfo CreateLabel(string text, float xMm, float yMm, Font font, Color? color)
        {
            bool ownsFont;
            Font actualFont;

            if (font == null)
            {
                actualFont = new Font("Arial", 12);
                ownsFont = true;
            }
            else
            {
                actualFont = font;
                ownsFont = false;
            }

            Color actualColor = color ?? Color.Black;
            return new LabelInfo(text, xMm, yMm, actualFont, actualColor, ownsFont);
        }

        public void Print(bool duplex = false, bool landscape = false)
        {
            if (_frontLabels.Count == 0 && _backLabels.Count == 0)
            {
                MessageBox.Show("No labels to print.", "LabelPrinter",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _currentPageIndex = 0;
            _landscape = landscape;   // <== remember it

            using (PrintDocument pd = new PrintDocument())
            {
                pd.DocumentName = "Banner Labels";

                // Create custom paper size 250mm x 680mm
                PaperSize customPaper = CreateCustomPaperSize(250, 680, "Banner_250x680");

                if (customPaper != null)
                {
                    pd.DefaultPageSettings.PaperSize = customPaper;
                    pd.DefaultPageSettings.Landscape = landscape;
                }
                else
                {
                    MessageBox.Show("Failed to create custom paper size. Using default A4.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                if (duplex && _backLabels.Count > 0)
                    pd.PrinterSettings.Duplex = Duplex.Vertical;
                else
                    pd.PrinterSettings.Duplex = Duplex.Simplex;

                pd.PrintPage += PrintPage;

                try
                {
                    pd.Print();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Print failed: {ex.Message}", "Print Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                pd.PrintPage -= PrintPage;
            }

            // Cleanup
            foreach (var label in _frontLabels)
                if (label.OwnsFont) label.Font?.Dispose();
            foreach (var label in _backLabels)
                if (label.OwnsFont) label.Font?.Dispose();

            _frontLabels.Clear();
            _backLabels.Clear();
        }

        private PaperSize CreateCustomPaperSize(int widthMm, int heightMm, string paperName)
        {
            try
            {
                int widthInHundredthsInch = (int)(widthMm / 25.4 * 100);
                int heightInHundredthsInch = (int)(heightMm / 25.4 * 100);
                return new PaperSize(paperName, widthInHundredthsInch, heightInHundredthsInch);
            }
            catch
            {
                return null;
            }
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            e.Graphics.PageUnit = GraphicsUnit.Display;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // --- handle landscape: rotate the whole drawing surface ---
            int pageWidthPx = e.PageBounds.Width;
            int pageHeightPx = e.PageBounds.Height;

            if (!_landscape)
            {
                // Rotate 90° clockwise so our portrait math (250x680mm) still works
                e.Graphics.TranslateTransform(pageWidthPx, 0);
                e.Graphics.RotateTransform(90);

                // After rotation, logical width/height are swapped
                int tmp = pageWidthPx;
                pageWidthPx = pageHeightPx;
                pageHeightPx = tmp;
            }

            // Now print ruler (after rotation) if enabled
            if (_ruler != null)
            {
                _ruler.Print(e.Graphics, false); // orientation already handled by transform
            }

            // Page dimensions in pixels now correspond to 250mm x 680mm in our math
            float pxPerMmX = pageWidthPx / 250f; // 0–250 mm horizontally
            float pxPerMmY = pageHeightPx / 680f; // 0–680 mm vertically

            // Select front or back labels as you already do...
            List<LabelInfo> labelsToPrint = (_currentPageIndex == 0)
                ? _frontLabels
                : _backLabels;

            foreach (var label in labelsToPrint)
            {
                // Convert mm coordinates to pixels
                // XMm: 0-250mm from left → 0 to pageWidthPx
                // YMm: 0-680mm from top → 0 to pageHeightPx
                float xPx = label.XMm * pxPerMmX;
                float yPx = label.YMm * pxPerMmY;

                using (SolidBrush brush = new SolidBrush(label.Color))
                {
                    e.Graphics.DrawString(label.Text, label.Font, brush, xPx, yPx);
                }
            }

            // Handle duplex printing
            if (_currentPageIndex == 0 && _backLabels.Count > 0)
            {
                _currentPageIndex++;
                e.HasMorePages = true;
            }
            else
            {
                e.HasMorePages = false;
            }
        }
    }
}