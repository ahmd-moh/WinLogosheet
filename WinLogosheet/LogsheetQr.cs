using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using QRCoder;

namespace WinLogosheet
{
    // ═══════════════════════════════════════════════════════════════════════
    //  OFFLINE QR TRANSFER
    //  Serialises the on-screen logsheet grid into a compact, alphanumeric-safe
    //  string and renders it as an OFFLINE QR code for transfer to a companion
    //  mobile app. Nothing here touches the network — the QR is generated
    //  locally and simply displayed for the phone camera to scan.
    //
    //  Payload format (every character is inside the QR "alphanumeric" table
    //  0-9 A-Z space $ % * + - . / :  so the code stays in the high-capacity
    //  alphanumeric encoding mode instead of falling back to byte mode):
    //
    //      LS1 <YYYYMMDD>*<row>*<row>*...
    //
    //      row = [-]HH:c1.c2.c3. ... .c24
    //            HH = two-digit hour (08..24, 01..07)
    //            -  = optional leading marker: the hour is flagged "skip" on PC
    //            cX = integer reading, or empty when the cell has no value
    //                 (so "12..7" means: 12, empty, 7)
    //
    //  Only columns 1..24 of each row are emitted (column 0 is the hour label,
    //  carried by the HH token). All 24 cells are always written — including
    //  trailing empties — so column positions line up exactly on the phone,
    //  reconstructing the grid pixel-for-pixel with the PC.
    // ═══════════════════════════════════════════════════════════════════════
    public static class LogsheetQr
    {
        public const string FormatTag = "LS1";
        public const int ColumnCount = 24;

        private const char RowSep = '*';
        private const char CellSep = '.';
        private const char HourSep = ':';
        private const char SkipMark = '-';

        // Build the QR text payload from exactly what is currently on screen
        // (the ListView already reflects every manual textbox edit).
        public static string BuildPayload(ListView list, DateTime date)
        {
            var sb = new StringBuilder(2048);
            sb.Append(FormatTag).Append(' ').Append(date.ToString("yyyyMMdd"));

            foreach (ListViewItem item in list.Items)
            {
                if (!int.TryParse(item.Text, out int hour)) continue;

                sb.Append(RowSep);
                if ((item.Tag as string) == "skip") sb.Append(SkipMark);
                sb.Append(hour.ToString("00")).Append(HourSep);

                for (int col = 1; col <= ColumnCount; col++)
                {
                    if (col > 1) sb.Append(CellSep);
                    if (col < item.SubItems.Count)
                        AppendDigits(sb, item.SubItems[col].Text);
                }
            }
            return sb.ToString();
        }

        // Readings are integers; keep only digits so a stray character from a
        // manual edit can never push the payload out of alphanumeric mode.
        private static void AppendDigits(StringBuilder sb, string raw)
        {
            if (string.IsNullOrEmpty(raw)) return;
            foreach (char c in raw)
                if (c >= '0' && c <= '9') sb.Append(c);
        }

        // Render the payload as a QR bitmap. Prefers error-correction level M
        // (robust for phone-camera scanning); steps down to L (highest data
        // capacity) only if the sheet is unusually large. eccLevel reports which
        // level was actually used.
        public static Bitmap Generate(string payload, int pixelsPerModule, out string eccLevel)
        {
            payload = payload ?? string.Empty;

            QRCodeData data = null;
            eccLevel = null;
            foreach (var lvl in new[] { QRCodeGenerator.ECCLevel.M, QRCodeGenerator.ECCLevel.L })
            {
                try
                {
                    using (var gen = new QRCodeGenerator())
                        data = gen.CreateQrCode(payload, lvl);
                    eccLevel = lvl.ToString();
                    break;
                }
                catch (QRCoder.Exceptions.DataTooLongException)
                {
                    data = null;   // retry at the next, higher-capacity level
                }
            }

            if (data == null)
                throw new InvalidOperationException(
                    "The logsheet is too large to fit in a single QR code.");

            using (data)
            {
                byte[] png = new PngByteQRCode(data).GetGraphic(pixelsPerModule);
                using (var ms = new MemoryStream(png))
                using (var tmp = Image.FromStream(ms))
                    return new Bitmap(tmp);   // independent copy; stream can close
            }
        }
    }
}
