using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinLogosheet
{
    public class FormFiller
    {
        private PrintDocument printDoc;
        private List<FormField> fields;
        private string paperSizeName;
        private bool isLandscape;

        public FormFiller()
        {
            printDoc = new PrintDocument();
            printDoc.PrintPage += PrintDocument_PrintPage;
            fields = new List<FormField>();
        }

        // Field definition class
        public class FormField
        {
            public string Text { get; set; }
            public float X { get; set; } // cm from left edge
            public float Y { get; set; } // cm from top edge
            public string FontName { get; set; } = "Arial";
            public float FontSize { get; set; } = 0.35f; // cm
            public bool IsBold { get; set; }
            public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;
            public float MaxWidth { get; set; } = 0; // 0 = no limit
            public bool Underline { get; set; }
            public Color TextColor { get; set; } = Color.Black;
        }

        // Set standard paper size
        public void SetPaperSize(string sizeName = "A4", bool landscape = false)
        {
            paperSizeName = sizeName;
            isLandscape = landscape;
            printDoc.DefaultPageSettings.Landscape = landscape;

            // Set standard paper size
            foreach (PaperSize size in printDoc.PrinterSettings.PaperSizes)
            {
                if (size.PaperName.ToUpper().Contains(sizeName.ToUpper()))
                {
                    printDoc.DefaultPageSettings.PaperSize = size;
                    break;
                }
            }
        }

        // Add a field to be printed
        public void AddField(string text, float xCm, float yCm,
                            float fontSizeCm = 0.35f, string fontName = "Arial",
                            bool bold = false)
        {
            fields.Add(new FormField
            {
                Text = text,
                X = xCm,
                Y = yCm,
                FontName = fontName,
                FontSize = fontSizeCm,
                IsBold = bold
            });
        }

        // Add multiple fields
        public void AddFields(List<FormField> fieldList)
        {
            fields.AddRange(fieldList);
        }

        // Clear all fields
        public void ClearFields()
        {
            fields.Clear();
        }

        // Convert cm to printer units (1/100 inch)
        private float CmToPrinterUnits(float cm)
        {
            return cm * (100f / 2.54f);
        }

        // Get font in appropriate units
        private Font GetFont(FormField field)
        {
            // Convert cm font size to points (1 point = 1/72 inch)
            float fontSizePoints = field.FontSize * (72f / 2.54f);
            FontStyle style = FontStyle.Regular;

            if (field.IsBold) style |= FontStyle.Bold;
            if (field.Underline) style |= FontStyle.Underline;

            return new Font(field.FontName, fontSizePoints, style);
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.PageUnit = GraphicsUnit.Display; // 1/100 inch
            float scale = 100f / 2.54f; // cm to printer units

            // Apply scaling for cm coordinates
            g.ScaleTransform(scale, scale);

            // Handle hard margins
            float hardMarginX = e.PageSettings.HardMarginX / scale;
            float hardMarginY = e.PageSettings.HardMarginY / scale;
            g.TranslateTransform(-hardMarginX, -hardMarginY);

            // Handle landscape orientation
            if (isLandscape)
            {
                GraphicsState state = g.Save();
                g.RotateTransform(90);

                // Get page dimensions in cm for landscape
                float pageWidthCm = CmToPrinterUnits(e.PageBounds.Height) / scale;
                float pageHeightCm = CmToPrinterUnits(e.PageBounds.Width) / scale;
                g.TranslateTransform(0, -pageWidthCm);

                DrawAllFields(g);
                g.Restore(state);
            }
            else
            {
                DrawAllFields(g);
            }

            e.HasMorePages = false;
        }

        private void DrawAllFields(Graphics g)
        {
            foreach (FormField field in fields)
            {
                DrawField(g, field);
            }
        }

        private void DrawField(Graphics g, FormField field)
        {
            using (Font font = GetFont(field))
            using (Brush brush = new SolidBrush(field.TextColor))
            {
                // Measure text
                SizeF textSize = g.MeasureString(field.Text, font);

                // Calculate position
                float x = field.X;
                float y = field.Y;

                // Handle alignment
                if (field.Alignment == HorizontalAlignment.Right)
                {
                    x -= textSize.Width;
                }
                else if (field.Alignment == HorizontalAlignment.Center)
                {
                    x -= textSize.Width / 2;
                }

                // Draw text
                g.DrawString(field.Text, font, brush, x, y);

                // Draw underline if requested
                if (field.Underline)
                {
                    float underlineY = y + textSize.Height;
                    Pen underlinePen = new Pen(field.TextColor, 0.02f);
                    g.DrawLine(underlinePen, x, underlineY, x + textSize.Width, underlineY);
                    underlinePen.Dispose();
                }
            }
        }

        // Show print preview
        public void ShowPrintPreview()
        {
            PrintPreviewDialog preview = new PrintPreviewDialog
            {
                Document = printDoc,
                WindowState = FormWindowState.Maximized
            };
            preview.ShowDialog();
        }

        // Print directly
        public void Print()
        {
            printDoc.Print();
        }

        // Calibration method - prints a grid for aligning your form
        public void PrintCalibrationGrid()
        {
            // Save current fields
            List<FormField> savedFields = new List<FormField>(fields);
            fields.Clear();

            // Add grid lines
            AddCalibrationGrid();

            // Print grid
            ShowPrintPreview();

            // Restore fields
            fields = savedFields;
        }

        private void AddCalibrationGrid()
        {
            // Add coordinate markers every cm
            for (float x = 0; x <= 20; x += 1)
            {
                for (float y = 0; y <= 28; y += 1)
                {
                    if (x % 5 == 0 && y % 5 == 0)
                    {
                        AddField($"({x},{y})", x, y, 0.2f);
                    }
                }
            }
        }
    }
}