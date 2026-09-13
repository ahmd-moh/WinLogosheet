using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace WinLogosheet
{
    public class Ruler
    {
        private readonly SizeF _pageSizeMm;
        private readonly int _topMm;
        private readonly int _rightMm;
        private readonly int _bottomMm;
        private readonly int _leftMm;

        // Tick lengths in mm
        private const float SHORT_TICK_LENGTH = 2f;
        private const float LONG_TICK_LENGTH = 5f;
        private const float LABEL_OFFSET = 1f;

        public Ruler(SizeF pageSizeMm, int topMm, int rightMm, int bottomMm, int leftMm)
        {
            _pageSizeMm = pageSizeMm;
            _topMm = topMm;
            _rightMm = rightMm;
            _bottomMm = bottomMm;
            _leftMm = leftMm;
        }

        public void Print(Graphics g, bool landscape = true)
        {
            // Get DPI for mm to pixel conversion
            float dpiX = g.DpiX;
            float dpiY = g.DpiY;
            
            // Convert mm to pixels: mm * dpi / 25.4f
            float pixelsPerMmX = dpiX / 25.4f;
            float pixelsPerMmY = dpiY / 25.4f;

            // Convert page size and margins to pixels
            float pageWidthPx = _pageSizeMm.Width * pixelsPerMmX;
            float pageHeightPx = _pageSizeMm.Height * pixelsPerMmY;
            
            float leftPx = _leftMm * pixelsPerMmX;
            float rightPx = _rightMm * pixelsPerMmX;
            float topPx = _topMm * pixelsPerMmY;
            float bottomPx = _bottomMm * pixelsPerMmY;

            using (Pen pen = new Pen(Color.Black, 1f))
            using (Font labelFont = new Font("Arial", 6f))
            using (Brush brush = new SolidBrush(Color.Black))
            {
                // Draw horizontal rulers (top and bottom)
                if (_topMm > 0)
                    DrawHorizontalRuler(g, pen, brush, labelFont, topPx, pageWidthPx, leftPx, rightPx, 
                                       pixelsPerMmX, pixelsPerMmY, true);
                
                if (_bottomMm > 0)
                    DrawHorizontalRuler(g, pen, brush, labelFont, pageHeightPx - bottomPx, pageWidthPx, 
                                       leftPx, rightPx, pixelsPerMmX, pixelsPerMmY, false);

                // Draw vertical rulers (left and right)
                if (_leftMm > 0)
                    DrawVerticalRuler(g, pen, brush, labelFont, leftPx, pageHeightPx, topPx, bottomPx, 
                                     pixelsPerMmX, pixelsPerMmY, true);
                
                if (_rightMm > 0)
                    DrawVerticalRuler(g, pen, brush, labelFont, pageWidthPx - rightPx, pageHeightPx, 
                                     topPx, bottomPx, pixelsPerMmX, pixelsPerMmY, false);
            }
        }

        private void DrawHorizontalRuler(Graphics g, Pen pen, Brush brush, Font font, 
                                       float yPos, float pageWidthPx, float leftPx, float rightPx,
                                       float pixelsPerMmX, float pixelsPerMmY, bool isTop)
        {
            float rulerLengthMm = _pageSizeMm.Width - _leftMm - _rightMm;
            float startXPx = leftPx;
            float endXPx = pageWidthPx - rightPx;

            // Draw baseline
            g.DrawLine(pen, startXPx, yPos, endXPx, yPos);

            // Draw ticks
            for (int mm = 0; mm <= rulerLengthMm; mm++)
            {
                float xPos = startXPx + (mm * pixelsPerMmX);
                
                if (mm % 10 == 0) // Long tick every 10mm
                {
                    float tickLength = LONG_TICK_LENGTH * pixelsPerMmY;
                    float tickY1 = isTop ? yPos : yPos - tickLength;
                    float tickY2 = isTop ? yPos + tickLength : yPos;
                    
                    g.DrawLine(pen, xPos, tickY1, xPos, tickY2);
                    
                    // Draw label
                    string label = mm.ToString();
                    SizeF labelSize = g.MeasureString(label, font);
                    float labelX = xPos - (labelSize.Width / 2);
                    float labelY = isTop ? 
                        yPos + tickLength + (LABEL_OFFSET * pixelsPerMmY) :
                        yPos - tickLength - labelSize.Height - (LABEL_OFFSET * pixelsPerMmY);
                    
                    g.DrawString(label, font, brush, labelX, labelY);
                }
                else // Short tick every 1mm
                {
                    float tickLength = SHORT_TICK_LENGTH * pixelsPerMmY;
                    float tickY1 = isTop ? yPos : yPos - tickLength;
                    float tickY2 = isTop ? yPos + tickLength : yPos;
                    
                    g.DrawLine(pen, xPos, tickY1, xPos, tickY2);
                }
            }
        }

        private void DrawVerticalRuler(Graphics g, Pen pen, Brush brush, Font font,
                                     float xPos, float pageHeightPx, float topPx, float bottomPx,
                                     float pixelsPerMmX, float pixelsPerMmY, bool isLeft)
        {
            float rulerLengthMm = _pageSizeMm.Height - _topMm - _bottomMm;
            float startYPx = topPx;
            float endYPx = pageHeightPx - bottomPx;

            // Draw baseline
            g.DrawLine(pen, xPos, startYPx, xPos, endYPx);

            // Draw ticks
            for (int mm = 0; mm <= rulerLengthMm; mm++)
            {
                float yPos = startYPx + (mm * pixelsPerMmY);
                
                if (mm % 10 == 0) // Long tick every 10mm
                {
                    float tickLength = LONG_TICK_LENGTH * pixelsPerMmX;
                    float tickX1 = isLeft ? xPos : xPos - tickLength;
                    float tickX2 = isLeft ? xPos + tickLength : xPos;
                    
                    g.DrawLine(pen, tickX1, yPos, tickX2, yPos);
                    
                    // Draw label
                    string label = mm.ToString();
                    SizeF labelSize = g.MeasureString(label, font);
                    float labelX = isLeft ? 
                        xPos + tickLength + (LABEL_OFFSET * pixelsPerMmX) :
                        xPos - tickLength - labelSize.Width - (LABEL_OFFSET * pixelsPerMmX);
                    float labelY = yPos - (labelSize.Height / 2);
                    
                    g.DrawString(label, font, brush, labelX, labelY);
                }
                else // Short tick every 1mm
                {
                    float tickLength = SHORT_TICK_LENGTH * pixelsPerMmX;
                    float tickX1 = isLeft ? xPos : xPos - tickLength;
                    float tickX2 = isLeft ? xPos + tickLength : xPos;
                    
                    g.DrawLine(pen, tickX1, yPos, tickX2, yPos);
                }
            }
        }
    }
}